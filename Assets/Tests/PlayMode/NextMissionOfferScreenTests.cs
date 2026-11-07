using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-040 and D-017-AC-08 on the real screens: the offer after NEXT MISSION, and Overdrive in the upgrade list.
    public sealed class NextMissionOfferScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static CombatBoosts Boosts => (CombatBoosts)GameType("TreadShredBoosts").GetProperty("Current").GetValue(null);

        // D-045: Overdrive is supply; this uses up the Overdrive the save holds (the tests hold no other supply).
        private static void ClearOverdrive()
        {
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            while (progression.Supply(SupplyTrack.Overdrive) > 0)
                progression.SupplyMissionWon();
        }

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static IEnumerator WinAndTapNext(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
            var buttons = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
            buttons.GetType().GetMethod("NextLv").Invoke(buttons, null);
            yield return null;
        }

        [UnityTest(Description = "D-040-AC-04: after a victory NEXT MISSION opens ARM OVERDRIVE, NO THANKS, PLAY loads the next mission, and with Overdrive running it offers the cheapest upgrade; both pass the screen audit on every shape")]
        public IEnumerator NextMissionOpensAnOffer()
        {
            var findings = new List<Finding>();
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            try
            {
                // Armor at level 1 (bought with gems, D-017-AC-03), so the cheapest upgrade is one coins can buy.
                progression.Replace(new ProgressionState { coins = 500, upgradeLevels = new[] { 1, 0, 0 } });
                yield return WinAndTapNext("Lv1");
                Assert.IsNotNull(GameObject.Find("TreadShredNextMissionOffer"), "an offer before the next mission");
                var texts = Texts();
                Assert.Contains(OfferLabels.ArmOverdrive, texts);
                Assert.Contains(OfferLabels.ForNextMission, texts);
                Assert.IsNotNull(ButtonStarting("ACTIVATE"), "the one-price button");
                yield return Audit("NEXT MISSION offer: Overdrive", findings);

                ButtonStarting(OfferLabels.NoThanksPlay).onClick.Invoke();
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual("Lv2", SceneManager.GetActiveScene().name, "NO THANKS, PLAY loads the next mission");

                progression.AddSupply(SupplyTrack.Overdrive);
                yield return WinAndTapNext("Lv2");
                Assert.IsNotNull(GameObject.Find("TreadShredNextMissionOffer"));
                Assert.Contains("UPGRADE FIREPOWER", Texts(), "Overdrive running: the cheapest upgrade instead");
                Assert.IsNotNull(ButtonStarting("UPGRADE FOR "), "priced in coins");
                yield return Audit("NEXT MISSION offer: upgrade", findings);
                UnityEngine.Object.Destroy(GameObject.Find("TreadShredNextMissionOffer"));
                yield return null;
            }
            finally
            {
                ClearOverdrive();
                Time.timeScale = 1f;
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-017-AC-08: the upgrade screen lists OVERDRIVE with FOR YOUR NEXT MISSION and an ACTIVATE button; held gems are confirmed once, then it shows its supply and offers ADD (D-045), and the screen passes the audit on every shape")]
        public IEnumerator UpgradeScreenSellsOverdrive()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString("TreadShredGemWallet", JsonUtility.ToJson(new WalletState()));
            Component panel = null;
            try
            {
                ClearOverdrive();
                Wallet.Replace(new WalletState { balance = 100, starterGranted = true, starterPackBought = true });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Start");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
                panel = (Component)GameType("TreadShredUpgradePanel").GetMethod("Show").Invoke(null, new object[] { font });
                yield return null;
                var texts = Texts();
                Assert.Contains("OVERDRIVE", texts);
                Assert.Contains(OfferLabels.ForNextMission, texts);
                Assert.Contains(OfferLabels.OverdriveShots, texts);
                Assert.IsNotNull(ButtonStarting("ACTIVATE FOR 100 GEMS"), "held gems cover it");
                yield return Audit("upgrade screen with Overdrive", findings);

                ButtonStarting("ACTIVATE FOR 100 GEMS").onClick.Invoke();
                yield return null;
                ButtonStarting("SPEND 100 GEMS").onClick.Invoke();
                yield return null;
                Assert.IsTrue(Boosts.OverdriveActive, "armed for the next mission");
                Assert.AreEqual(0, Wallet.Balance);
                // D-045: the row shows the supply it holds, and more can be added.
                Assert.Contains("SUPPLY 1: 5 MISSIONS LEFT", Texts());
                Assert.IsNotNull(ButtonStarting("ADD"), "more Overdrive can be added");
            }
            finally
            {
                if (panel != null)
                    UnityEngine.Object.Destroy(panel.gameObject);
                ClearOverdrive();
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
