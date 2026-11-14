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
    // D-047-AC-05 on a real lost mission: the free test drive from the War Chest reveal, the drive itself and the reveal
    // that follows. WarChestSessionSetup keeps the offer off every other screen test; this reopens it.
    public sealed class WarChestTestDriveTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

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

        private static bool Driving() => (bool)GameType("TreadShredTestDrive").GetProperty("Active").GetValue(null);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonLabelled(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        // The mission is playing: loaded and past any start pause.
        private static IEnumerator Playing()
        {
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static readonly float Reveal = WarChest.RevealSeconds * (WarChest.Items.Count + 6);

        [UnityTest(Description = "D-047-AC-05: TEST DRIVE IT FREE replays the lost mission with every War Chest item on and a countdown, earning and keeping nothing; when it ends the reveal sells it, and closing that replays the mission without the gear")]
        public IEnumerator TestDriveThenReveal()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState
                {
                    balance = 0,
                    starterGranted = true,
                    starterPackBought = true,
                    starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks
                });
                WarChestSessionSetup.Offered(false);
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv1");
                yield return null;
                yield return Playing();
                var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
                manager.GetType().GetMethod("Failure", Members).Invoke(manager, null);
                yield return new WaitForSecondsRealtime(2.6f + Reveal);
                var armorHeld = Progression.Supply(SupplyTrack.Armor);
                var coins = Progression.Coins;
                Assert.IsNotNull(ButtonLabelled(WarChest.TestDriveButton), "the free test drive leads the reveal after a loss");
                Assert.IsTrue(Texts().Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)), "beside the purchase");

                ButtonLabelled(WarChest.TestDriveButton).onClick.Invoke();
                yield return null;
                yield return Playing();
                Assert.IsTrue(Driving(), "the drive is running");
                Assert.AreEqual(SceneManager.GetActiveScene().name, "Lv1", "on the mission that was lost");
                Assert.IsTrue(Progression.TestDriving);
                Assert.AreEqual(armorHeld + 3, Progression.Supply(SupplyTrack.Armor), "the chest's armor is on");
                Assert.GreaterOrEqual(Progression.Drawer(Weapon.Cannon).Count, 4, "every weapon is in the drawer");
                var texts = Texts();
                Assert.IsTrue(texts.Any(t => t.StartsWith("TEST DRIVE 0:", StringComparison.Ordinal)), string.Join(" | ", texts));
                Assert.Contains(WarChest.TestDriveNote, texts);
                yield return Audit("War Chest test drive", findings);

                // Run the clock out.
                var drive = UnityEngine.Object.FindFirstObjectByType(GameType("TreadShredTestDrive"));
                drive.GetType().GetField("left", Members).SetValue(drive, 0.05f);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsFalse(Driving(), "the countdown ended the drive");
                Assert.IsFalse(Progression.TestDriving, "and took the gear away");
                Assert.AreEqual(armorHeld, Progression.Supply(SupplyTrack.Armor));
                Assert.AreEqual(coins, Progression.Coins, "the drive earned nothing");
                yield return new WaitForSecondsRealtime(Reveal);
                texts = Texts();
                Assert.Contains(WarChest.AfterDriveSubtitle, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)));
                Assert.IsNull(ButtonLabelled(WarChest.TestDriveButton), "one drive per offer");
                yield return Audit("War Chest after a test drive", findings);

                ButtonLabelled("NOT NOW").onClick.Invoke();
                yield return null;
                yield return Playing();
                Assert.AreEqual("Lv1", SceneManager.GetActiveScene().name, "the mission plays again");
                Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"));
                Assert.IsNull(GameObject.Find("TreadShredTestDriveHud"), "without the drive");
                Assert.IsFalse(Driving());
                Assert.AreEqual(armorHeld, Progression.Supply(SupplyTrack.Armor), "and without the gear");
            }
            finally
            {
                if (Driving())
                    GameType("TreadShredTestDrive").GetMethod("Finish").Invoke(null, null);
                foreach (var name in new[] { "TreadShredWarChestOffer", "TreadShredTestDriveHud" })
                {
                    var open = GameObject.Find(name);
                    if (open != null)
                        UnityEngine.Object.Destroy(open);
                }
                Progression.EndTestDrive();
                WarChestSessionSetup.Offered(true);
                Time.timeScale = 1f;
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
