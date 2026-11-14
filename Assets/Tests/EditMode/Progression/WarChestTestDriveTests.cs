using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class WarChestTestDriveTests
    {
        // Round-trips through JsonUtility, as PlayerPrefs and the cloud snapshot do, so a reload sees only saved fields.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private sealed class JsonProgressionStore : IProgressionStore
        {
            private string saved;
            public ProgressionState Load() => saved == null ? null : JsonUtility.FromJson<ProgressionState>(saved);
            public void Save(ProgressionState state) => saved = JsonUtility.ToJson(state);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-047-AC-05: while the War Chest is test driven its supply and weapons count in the mission, nothing is saved or used up, and ending the drive takes them away")]
        public void TestDriveGearCountsButIsNeverKept()
        {
            var store = new JsonProgressionStore();
            var progression = new PlayerProgression(store, new GemWallet(new JsonWalletStore()));
            progression.AddSupply(SupplyTrack.Armor);
            progression.StartTestDrive();
            Assert.IsTrue(progression.TestDriving);
            Assert.AreEqual(4, progression.Supply(SupplyTrack.Armor), "the chest's 3 units on top of the 1 held");
            foreach (var item in WarChest.Items.Where(item => item.Track != SupplyTrack.Armor))
                Assert.AreEqual(item.Units, progression.Supply(item.Track), item.Track.ToString());
            foreach (var weapon in new[] { Weapon.ArcShot, Weapon.TripleShot, Weapon.Seeker })
            {
                Assert.IsTrue(progression.HasUsed(weapon));
                Assert.IsTrue(progression.Drawer(Weapon.Cannon).Any(entry => entry.Weapon == weapon && entry.State == DrawerState.Rented),
                    weapon + " is in the drawer to switch to");
            }
            Assert.AreEqual(Weapon.Seeker, progression.StartingWeapon(Weapon.Cannon), "a cannon mission starts on the latest weapon");

            for (var won = 0; won < 2 * SupplyRules.MissionsPerUse; won++)
                progression.SupplyMissionWon();
            Assert.AreEqual(4, progression.Supply(SupplyTrack.Armor), "a drive uses nothing up");
            var reloaded = new PlayerProgression(store, new GemWallet(new JsonWalletStore()));
            Assert.AreEqual(1, reloaded.Supply(SupplyTrack.Armor), "the drive's units are never saved");
            Assert.IsFalse(reloaded.HasUsed(Weapon.Seeker));

            progression.EndTestDrive();
            Assert.IsFalse(progression.TestDriving);
            Assert.AreEqual(1, progression.Supply(SupplyTrack.Armor));
            Assert.AreEqual(0, progression.Supply(SupplyTrack.Seeker));
            Assert.IsFalse(progression.HasUsed(Weapon.Seeker));
            Assert.AreEqual(1, progression.Drawer(Weapon.Cannon).Count, "the cannon alone again");
        }

        [Test(Description = "D-047-AC-05: the test drive lasts 30 seconds of play, counted down in whole seconds, and says the War Chest gear is on")]
        public void TestDriveClockCountsDown()
        {
            Assert.AreEqual(30f, WarChest.TestDriveSeconds);
            Assert.AreEqual("TEST DRIVE 0:30", WarChest.TestDriveClock(30f));
            Assert.AreEqual("TEST DRIVE 0:30", WarChest.TestDriveClock(29.2f));
            Assert.AreEqual("TEST DRIVE 0:01", WarChest.TestDriveClock(0.4f));
            Assert.AreEqual("TEST DRIVE 0:00", WarChest.TestDriveClock(-1f));
            Assert.AreEqual("ALL WAR CHEST GEAR ON", WarChest.TestDriveNote);
            Assert.AreEqual("TEST DRIVE IT FREE", WarChest.TestDriveButton);
            Assert.AreEqual(WarChestTestDriveBits(), WarChest.WeaponBits);
            StringAssert.Contains("left -= Time.deltaTime;", Script("Game/TreadShredTestDrive.cs"), "game time: a paused mission pauses it");
        }

        [Test(Description = "D-047-AC-05: a test drive earns nothing: clearing the map or losing the tank ends it before any result is counted, no revive is offered, and leaving the mission takes the gear away")]
        public void TestDriveEarnsNothing()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            var victory = results.IndexOf("public void Victory()", StringComparison.Ordinal);
            var failure = results.IndexOf("public void Failure()", StringComparison.Ordinal);
            Assert.Less(results.IndexOf("TreadShredTestDrive.Finish();", victory, StringComparison.Ordinal),
                results.IndexOf("TreadShredReviewPrompt.MissionEnded(true);", victory, StringComparison.Ordinal), "before a victory is counted");
            Assert.Less(results.IndexOf("TreadShredTestDrive.Finish();", failure, StringComparison.Ordinal),
                results.IndexOf("CompleteCurrentMission(false)", failure, StringComparison.Ordinal), "before a loss is counted");
            StringAssert.Contains("if (TreadShredTestDrive.Active)\n            return false;", Script("Game/TreadShredRevivePanel.cs"));
            var drive = Script("Game/TreadShredTestDrive.cs");
            StringAssert.Contains("if (loaded.name != scene || hud != null)\n        {\n            End(false);", drive);
            StringAssert.Contains("TreadShredProgress.Ensure().Progression.EndTestDrive();", drive);
        }

        private static int WarChestTestDriveBits() =>
            WeaponRental.Bit(Weapon.ArcShot) | WeaponRental.Bit(Weapon.TripleShot) | WeaponRental.Bit(Weapon.Seeker);
    }
}
