using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-044 in the game: RESET GAME under Settings, the typed confirmation, and the fresh start it leads to.
    public sealed class GameResetScreenTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const string AttributionKey = "attribution_skan_registered";

        private static PlayerProgression Progression =>
            (PlayerProgression)TreadShredProgress.Ensure().Progression;

        private static GemWallet Wallet => (GemWallet)TreadShredStore.Ensure().Wallet;

        private static List<string> Texts() => UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonLabeled(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static InputField Typed => GameObject.Find("TreadShredResetTyped").GetComponent<InputField>();

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
        }

        private static IEnumerator OpenSettings()
        {
            var font = (Font)TreadShredUi.FindFont(GameObject.Find("Canvas"));
            var menu = UnityEngine.Object.FindFirstObjectByType<LoadLv>();
            TreadShredSettingsPanel.Show(menu, font);
            yield return null;
        }

        [UnityTest(Description = "D-044-AC-04: RESET GAME under Settings asks for the typed word (a near miss and KEEP MY GAME change nothing); erasing starts the game over at the start screen with no missions, coins, upgrades or medals, the best score kept, settings at their defaults and only the starter gems, keeping attribution bookkeeping, and Settings and the confirmation pass the screen audit on every screen shape")]
        public IEnumerator ResetStartsTheGameOver()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var hadAttribution = PlayerPrefs.HasKey(AttributionKey);
            var bestRun = PlayerPrefs.GetInt("TreadShredBestRun", 0);
            var attribution = PlayerPrefs.GetInt(AttributionKey, 0);
            try
            {
                Wallet.Replace(new WalletState { balance = 750, starterGranted = true, starterPackBought = true });
                Progression.Replace(new ProgressionState
                {
                    coins = 400, highestUnlockedTier = 1, upgradeLevels = new[] { 2, 1, 3 },
                    clearedMissions = new[] { (1L << 40) - 1, 3L, 0L }
                });
                PlayerPrefs.SetFloat("Lv1", 3f);
                PlayerPrefs.SetFloat("Sound", 1f);
                PlayerPrefs.SetInt("TreadShredBestRun", 98765);
                PlayerPrefs.SetInt(AttributionKey, 7);
                yield return Load("Start");
                yield return OpenSettings();
                Assert.IsNotNull(ButtonLabeled(TreadShred.Save.GameReset.Title), "Settings offers RESET GAME");
                yield return Audit("settings with reset game", findings);

                ButtonLabeled(TreadShred.Save.GameReset.Title).onClick.Invoke();
                yield return null;
                CollectionAssert.IsSupersetOf(Texts(), new[]
                {
                    TreadShred.Save.GameReset.Title, TreadShred.Save.GameReset.Erases, TreadShred.Save.GameReset.Prompt, TreadShred.Save.GameReset.ScoreStays, "IT ALSO ERASES YOUR GAME'S BACKUP IN ICLOUD. IT CANNOT BE UNDONE."
                });
                var erase = ButtonLabeled(TreadShred.Save.GameReset.Erase);
                Assert.IsFalse(erase.interactable, "off until RESET is typed");
                yield return Audit("reset game", findings);

                // D-044-AC-02: a near miss and KEEP MY GAME change nothing.
                Typed.text = "RESE";
                yield return null;
                Assert.IsFalse(erase.interactable);
                erase.onClick.Invoke();
                yield return null;
                Assert.AreEqual((750, 400), (Wallet.Balance, Progression.Coins), "a near miss erases nothing");
                ButtonLabeled(TreadShred.Save.GameReset.Keep).onClick.Invoke();
                yield return null;
                Assert.IsNull(GameObject.Find("TreadShredReset"), "KEEP MY GAME closes it");
                Assert.AreEqual((750, 400), (Wallet.Balance, Progression.Coins), "with nothing changed");

                ButtonLabeled(TreadShred.Save.GameReset.Title).onClick.Invoke();
                yield return null;
                Typed.text = "reset";
                yield return null;
                erase = ButtonLabeled(TreadShred.Save.GameReset.Erase);
                Assert.IsTrue(erase.interactable, "typing RESET unlocks it");
                yield return Audit("reset game, confirmed", findings);
                erase.onClick.Invoke();
                yield return null;
                yield return new WaitForSecondsRealtime(2f);

                Assert.AreEqual("Start", SceneManager.GetActiveScene().name, "the game starts over at the start screen");
                Assert.IsNull(GameObject.Find("TreadShredSettings"));
                Assert.AreEqual((0, 0, 0), (Progression.ClearedCount(DifficultyTier.Recruit), Progression.ClearedCount(DifficultyTier.Veteran), Progression.Coins));
                foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
                    Assert.AreEqual(0, Progression.Level(track), track + " upgrades");
                Assert.IsFalse(Progression.IsUnlocked(DifficultyTier.Veteran), "Veteran is locked again");
                Assert.AreEqual(DifficultyTier.Recruit, (DifficultyTier)TreadShredProgress.Ensure().SelectedTier);
                Assert.AreEqual(GemCatalog.StarterGrant, Wallet.Balance, "only the starter gems, as on a first launch");
                Assert.IsFalse(Wallet.StarterPackBought, "the Starter Pack is on offer again");
                Assert.AreEqual(0f, PlayerPrefs.GetFloat("Lv1", 0f), "medals erased");
                Assert.AreEqual(0f, PlayerPrefs.GetFloat("Sound", 0f), "sound back on, its default");
                Assert.AreEqual(98765, PlayerPrefs.GetInt("TreadShredBestRun", 0), "the best score stays");
                Assert.AreEqual(7, PlayerPrefs.GetInt(AttributionKey, 0), "attribution bookkeeping stays");
            }
            finally
            {
                Time.timeScale = 1f;
                foreach (var name in new[] { "TreadShredReset", "TreadShredSettings", "TreadShredStarterOffer" })
                {
                    var open = GameObject.Find(name);
                    if (open != null)
                        UnityEngine.Object.Destroy(open);
                }
                PlayerPrefs.SetInt("TreadShredBestRun", bestRun);
                if (hadAttribution)
                    PlayerPrefs.SetInt(AttributionKey, attribution);
                else
                    PlayerPrefs.DeleteKey(AttributionKey);
                Progression.Replace(new ProgressionState());
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
