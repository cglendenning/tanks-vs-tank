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
    // D-020-AC-06: after a paid revive the game waits on READY?; GO and the pause menu's resume both play the 3-2-1.
    public sealed class ReviveReadyTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression =>
            (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static List<string> Texts() => UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy
                && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static Button ButtonLabeled(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static bool CountingDown => UnityEngine.Object.FindFirstObjectByType(GameType("Count")) != null;

        private static Component Player => GameObject.FindGameObjectWithTag("player").GetComponent(GameType("PlayerMoverment"));

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator WaitForPlay()
        {
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // The arc shot's mission, so the weapon button is on the HUD; three one-hit shells, then REVIVE for gems.
        private static IEnumerator ReviveOnTheArcShotMission()
        {
            Wallet.Replace(new WalletState { balance = 200, starterGranted = true, starterPackBought = true });
            Progression.Replace(new ProgressionState { clearedMissions = new[] { (1L << 40) - 1, 0L, 0L } });
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv16");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            yield return WaitForPlay();
            var hit = Player.GetType().GetMethod("DeadPlayer");
            for (var shell = 0; shell < 3; shell++)
            {
                hit.Invoke(Player, new object[] { 1 });
                yield return new WaitForSecondsRealtime(0.8f);
            }
            ButtonStarting("REVIVE").onClick.Invoke();
            yield return null;
        }

        private static void CleanUp(string savedWallet)
        {
            Time.timeScale = 1f;
            foreach (var name in new[] { "TreadShredReady", "TreadShredRevive" })
            {
                var open = GameObject.Find(name);
                if (open != null)
                    UnityEngine.Object.Destroy(open);
            }
            Progression.Replace(new ProgressionState());
            Wallet.Replace(JsonUtility.FromJson<WalletState>(savedWallet));
        }

        [UnityTest(Description = "D-020-AC-06: a paid revive restores the tank in place and waits on READY? with GO and NOT YET; GO plays the 3-2-1 with the weapon button hidden, then play resumes where it stopped, and READY? passes the screen audit on every screen shape")]
        public IEnumerator GoPlaysTheCountdown()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                yield return ReviveOnTheArcShotMission();
                var position = Player.transform.position;
                Assert.IsNull(GameObject.Find("TreadShredRevive"), "the revive panel is gone");
                Assert.IsNotNull(GameObject.Find("TreadShredReady"), "READY? waits for the player");
                CollectionAssert.IsSupersetOf(Texts(), new[] { OfferLabels.Ready, OfferLabels.Go, OfferLabels.NotYet });
                Assert.AreEqual(0f, Time.timeScale, "the game stays paused");
                var shields = ((List<GameObject>)Player.GetType().GetField("Heart").GetValue(Player)).Where(h => h != null).ToList();
                Assert.IsTrue(shields.All(h => h.GetComponent<Image>().color.a > 0.9f), "full armor again");
                yield return Audit("READY AFTER A REVIVE", findings);

                ButtonLabeled(OfferLabels.Go).onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsNull(GameObject.Find("TreadShredReady"));
                Assert.IsTrue(CountingDown, "the 3-2-1 plays");
                Assert.AreEqual(0f, Time.timeScale, "still paused during the countdown");
                Assert.IsNull(GameObject.Find("TreadShredWeaponToggle"), "the weapon button is hidden during the countdown");

                yield return WaitForPlay();
                Assert.AreEqual(1f, Time.timeScale, "play resumes after the countdown");
                Assert.IsFalse(CountingDown);
                Assert.IsNotNull(GameObject.Find("TreadShredWeaponToggle"), "the weapon button is back");
                Assert.Less(Vector3.Distance(position, Player.transform.position), 0.5f, "the tank resumes where it stopped");
            }
            finally
            {
                CleanUp(saved);
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-020-AC-06: NOT YET opens the pause menu, and resuming from it plays the 3-2-1 before play goes on")]
        public IEnumerator NotYetOpensThePauseMenu()
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                yield return ReviveOnTheArcShotMission();
                ButtonLabeled(OfferLabels.NotYet).onClick.Invoke();
                yield return null;
                var buttons = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
                var menu = (GameObject)buttons.GetType().GetField("PauseGameObj").GetValue(buttons);
                Assert.IsTrue(menu.activeInHierarchy, "the pause menu is open");
                Assert.IsNull(GameObject.Find("TreadShredReady"));
                Assert.AreEqual(0f, Time.timeScale);

                buttons.GetType().GetMethod("PauseGame").Invoke(buttons, null);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsFalse(menu.activeInHierarchy, "the pause menu closes");
                Assert.IsTrue(CountingDown, "resuming plays the 3-2-1");
                Assert.AreEqual(0f, Time.timeScale, "still paused during the countdown");

                yield return WaitForPlay();
                Assert.AreEqual(1f, Time.timeScale, "play resumes after the countdown");
            }
            finally
            {
                CleanUp(saved);
            }
        }
    }
}
