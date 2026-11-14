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
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-047-AC-06 and AC-07 on the real upgrades and result screens. WarChestSessionSetup and WarChestArrivalSetup keep
    // the War Chest's offers off every other screen test; this turns them on and puts the wallet back after.
    public sealed class WarChestFreeMissionTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Store().GetType().GetProperty("Wallet").GetValue(Store());

        private static PlayerProgression Progression
        {
            get
            {
                var progress = GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);
                return (PlayerProgression)progress.GetType().GetProperty("Progression").GetValue(progress);
            }
        }

        private static bool FreeMission() => (bool)GameType("TreadShredFreeWarChest").GetProperty("Active").GetValue(null);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

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
            for (var waited = 0f; scene != "Start" && Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static IEnumerator Win()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        private static readonly float Reveal = WarChest.RevealSeconds * (WarChest.Items.Count + 6);

        private static void OnSale() => Wallet.Replace(new WalletState
        {
            balance = 0,
            starterGranted = true,
            starterPackBought = true,
            starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks
        });

        private static void CloseOverlays()
        {
            foreach (var name in new[] { "TreadShredWarChestOffer", "TreadShredUpgrades", "TreadShredFreeWarChestHud" })
            {
                var open = GameObject.Find(name);
                if (open != null)
                    UnityEngine.Object.Destroy(open);
            }
        }

        [UnityTest(Description = "D-047-AC-06: while the War Chest is on sale the upgrades screen shows WAR CHEST: <price> beside DONE; it opens the reveal with the purchase alone and closing it returns to the upgrades")]
        public IEnumerator UpgradesScreenSellsTheWarChest()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                OnSale();
                yield return Load("Start");
                var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
                var panel = (Component)GameType("TreadShredUpgradePanel").GetMethod("Show").Invoke(null, new object[] { font });
                yield return null;
                Assert.IsNotNull(ButtonStarting("WAR CHEST: "), "the War Chest beside DONE");
                yield return Audit("upgrades with the War Chest", findings);

                ButtonStarting("WAR CHEST: ").onClick.Invoke();
                yield return new WaitForSecondsRealtime(Reveal);
                Assert.IsNotNull(GameObject.Find("TreadShredWarChestOffer"));
                Assert.IsTrue(Texts().Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)), "the purchase");
                Assert.IsNull(ButtonStarting(WarChest.TestDriveButton), "no free drive from the upgrades screen");
                yield return Audit("War Chest from the upgrades screen", findings);

                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"));
                Assert.IsTrue(panel != null, "back on the upgrades screen");
            }
            finally
            {
                CloseOverlays();
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-047-AC-07: the free War Chest arrives on a result screen; taking it puts every item on for the next mission, which counts as usual, and its result screen opens the reveal")]
        public IEnumerator FreeWarChestForTheNextMission()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var free = GameType("TreadShredFreeWarChest");
            try
            {
                OnSale();
                WarChestArrivalSetup.Arrivals(true);
                // Due on the next result screen.
                var schedule = free.GetField("arrivals", Statics).GetValue(null);
                schedule.GetType().GetField("target", Members).SetValue(schedule, 1);
                schedule.GetType().GetField("seen", Members).SetValue(schedule, 0);

                yield return Load("Lv1");
                yield return Win();
                Assert.IsNotNull(ButtonStarting(WarChest.FreeButton), string.Join(" | ", Texts()));
                yield return Audit("free War Chest on a victory", findings);
                ButtonStarting(WarChest.FreeButton).onClick.Invoke();
                yield return null;
                Assert.IsNotNull(ButtonStarting(WarChest.ArmedLabel), "taken, for the next mission");

                var armorHeld = Progression.Supply(SupplyTrack.Armor);
                var coins = Progression.Coins;
                yield return Load("Lv1");
                Assert.IsTrue(FreeMission(), "the next mission carries it");
                Assert.AreEqual(armorHeld + 3, Progression.Supply(SupplyTrack.Armor));
                Assert.GreaterOrEqual(Progression.Drawer(Weapon.Cannon).Count, 4);
                Assert.Contains(WarChest.FreeNote, Texts());
                yield return Audit("free War Chest mission", findings);

                yield return Win();
                Assert.IsFalse(FreeMission(), "the free mission is over");
                Assert.AreEqual(armorHeld, Progression.Supply(SupplyTrack.Armor), "and its gear is gone");
                Assert.Greater(Progression.Coins, coins, "it counted as usual");
                yield return new WaitForSecondsRealtime(Reveal);
                var texts = Texts();
                Assert.Contains(WarChest.AfterDriveSubtitle, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)));
                Assert.IsNull(ButtonStarting(WarChest.FreeButton), "no second free chest on the same screen");
                yield return Audit("War Chest after a free mission", findings);
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"), "back on the result screen");
            }
            finally
            {
                CloseOverlays();
                if (FreeMission() || (bool)free.GetProperty("Armed").GetValue(null))
                    free.GetMethod("End", Statics).Invoke(null, null);
                WarChestArrivalSetup.Arrivals(false);
                Time.timeScale = 1f;
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
