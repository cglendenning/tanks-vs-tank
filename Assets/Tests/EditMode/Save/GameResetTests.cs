using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

namespace TreadShred.Save.Tests
{
    public sealed class GameResetTests
    {
        private long now = 1000;
        private long Clock() => now += 10;

        // Saved values that are not the player's game: attribution bookkeeping, the store-capture switch and the
        // save's own sync stamps (CloudSync.Reset rewrites those).
        private static readonly string[] Kept =
        {
            "attribution_purchases_usd_cents", "attribution_skan_registered", "TreadShredStoreCaptureMode",
            "TreadShredSaveWrittenAt", "TreadShredSaveSynced", "TreadShredSaveResetAt"
        };

        private static void PlayHistory(Install install)
        {
            install.Progression.CompleteMission(DifficultyTier.Recruit, 1, true);
            install.Progression.CompleteMission(DifficultyTier.Recruit, 2, true);
            install.Wallet.CreditPurchase(GemCatalog.Packs[0].ProductId, "txn-before-reset");
            install.Progression.TryUpgrade(UpgradeTrack.Firepower);
        }

        private static void AssertFresh(Install install, int gems, string who)
        {
            Assert.AreEqual(0, install.Progression.ClearedCount(DifficultyTier.Recruit), who + ": missions");
            Assert.AreEqual(0, install.Progression.Coins, who + ": coins");
            Assert.AreEqual(0, install.Progression.Level(UpgradeTrack.Firepower), who + ": upgrades");
            Assert.AreEqual(gems, install.Wallet.Balance, who + ": gems");
        }

        private static void Reset(Install install, FakeICloud.Device device, Func<long> clock) =>
            new CloudSync(install, device, clock).Reset();

        [Test(Description = "D-044-AC-02: only the typed word RESET unlocks the erase, in any letter case and ignoring surrounding spaces")]
        public void OnlyTheTypedWordUnlocksTheErase()
        {
            foreach (var typed in new[] { "RESET", "reset", " Reset " })
                Assert.IsTrue(GameReset.Confirms(typed), typed);
            foreach (var typed in new[] { null, "", "RESE", "RESETS", "RESET GAME", "RE SET", "ERASE" })
                Assert.IsFalse(GameReset.Confirms(typed), typed ?? "null");
            Assert.AreEqual("TYPE RESET TO CONFIRM", GameReset.Prompt);
            Assert.AreEqual(("RESET GAME", "ERASE EVERYTHING", "KEEP MY GAME"), (GameReset.Title, GameReset.Erase, GameReset.Keep));
        }

        [Test(Description = "D-044-AC-01: the confirmation names what is erased, bought gems included, that the platform cloud save goes too and that it cannot be undone")]
        public void TheConfirmationSaysWhatIsErased()
        {
            foreach (var part in new[] { "MISSIONS", "MEDALS", "COINS", "GEMS (BOUGHT GEMS TOO)", "UPGRADES", "WEAPONS", "SETTINGS" })
                StringAssert.Contains(part, GameReset.Erases);
            StringAssert.Contains("ICLOUD ACCOUNT", GameReset.Reach(android: false));
            StringAssert.Contains("GOOGLE PLAY GAMES ACCOUNT", GameReset.Reach(android: true));
            StringAssert.Contains("CANNOT BE UNDONE", GameReset.Reach(android: false));
        }

        [Test(Description = "D-025-AC-05: a reset save replaces every save written before it on either side, and a save written after it wins as usual")]
        public void AResetBeatsEverySaveBeforeIt()
        {
            var history = new SaveSnapshot { writtenAtTicks = 500, wallet = new WalletState { balance = 900, starterGranted = true } };
            history.progression.coins = 300;
            var reset = new SaveSnapshot { writtenAtTicks = 600, resetAtTicks = 600 };
            Assert.AreEqual(SaveSource.Cloud, SaveRules.Choose(history, reset), "a reset made elsewhere replaces older progress here");
            Assert.AreEqual(SaveSource.Cloud, SaveRules.Choose(null, reset), "and a fresh install takes it");
            var afterGrant = new SaveSnapshot { writtenAtTicks = 610, resetAtTicks = 600 };
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(afterGrant, history), "older progress never undoes a reset made here");
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(afterGrant, history, localSynced: false), "even before a first sync");
            var played = new SaveSnapshot { writtenAtTicks = 700, resetAtTicks = 600, wallet = new WalletState { balance = 140, starterGranted = true } };
            Assert.AreEqual(SaveSource.Cloud, SaveRules.Choose(afterGrant, played), "a save written after the reset wins as usual");
        }

        [Test(Description = "D-025-AC-05: after a reset on this phone, the old cloud save arriving late does not bring the erased game back")]
        public void TheOldSaveArrivingLateIsIgnored()
        {
            var cloud = new FakeICloud();
            var device = cloud.NewInstall(firstSyncDone: true);
            var phone = new Install(device, Clock).Launch();
            PlayHistory(phone);
            var before = device.LocalJson;

            Reset(phone, device, Clock);
            phone.Launch();
            AssertFresh(phone, GemCatalog.StarterGrant, "after the reset");

            device.LocalJson = before;
            device.Changed();
            AssertFresh(phone, GemCatalog.StarterGrant, "after the old save arrives");
            phone.Launch();
            AssertFresh(phone, GemCatalog.StarterGrant, "after a relaunch");
        }

        [Test(Description = "D-044-AC-05: the reset reaches the other devices on the account and a reinstall, and progress made after it syncs as usual")]
        public void TheResetReachesEveryDevice()
        {
            var cloud = new FakeICloud();
            var phoneDevice = cloud.NewInstall(firstSyncDone: true);
            var phone = new Install(phoneDevice, Clock).Launch();
            PlayHistory(phone);
            var tablet = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            Assert.AreEqual(2, tablet.Progression.ClearedCount(DifficultyTier.Recruit), "the tablet has the history");

            Reset(phone, phoneDevice, Clock);
            phone.Launch();
            Assert.AreEqual(0, tablet.Progression.ClearedCount(DifficultyTier.Recruit), "the tablet takes the reset while playing");
            Assert.AreEqual(0, tablet.Progression.Coins);
            tablet.Launch();
            AssertFresh(tablet, GemCatalog.StarterGrant, "the tablet relaunched");
            var reinstall = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            AssertFresh(reinstall, GemCatalog.StarterGrant, "a reinstall");

            phone.Progression.CompleteMission(DifficultyTier.Recruit, 1, true);
            Assert.AreEqual(1, tablet.Progression.ClearedCount(DifficultyTier.Recruit), "new progress after the reset syncs");
        }

        [Test(Description = "D-044-AC-03: the reset erases every value the game saves, medals for all 120 missions included, and keeps only attribution bookkeeping, the capture switch and the save's sync stamps")]
        public void TheResetCoversEverySavedKey()
        {
            var reset = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TreadShredGameReset")).First(t => t != null);
            var erased = new HashSet<string>((IEnumerable<string>)reset.GetMethod("ErasedKeys").Invoke(null, null));
            CollectionAssert.IsSupersetOf(erased, new[] { "Lv1", "Lvs1", "Lv120", "Lvs120", "Sound", "Music", "Vibrate", "Camera" });
            CollectionAssert.IsEmpty(erased.Intersect(Kept), "attribution and sync stamps stay");

            var saved = new HashSet<string>();
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "Script"), "*.cs", SearchOption.AllDirectories))
            {
                var code = File.ReadAllText(file);
                foreach (Match key in Regex.Matches(code, @"const string \w*Key\w* = ""([^""]+)"""))
                    saved.Add(key.Groups[1].Value);
                // A whole literal key; "Lv" + mission is covered by the 120 medal keys above.
                foreach (Match key in Regex.Matches(code, @"PlayerPrefs\.\w+\(""([^""]+)""\s*[,)]"))
                    saved.Add(key.Groups[1].Value);
            }
            Assert.Greater(saved.Count, 15, "the scan finds the game's keys");
            CollectionAssert.IsEmpty(saved.Where(key => !erased.Contains(key) && !Kept.Contains(key)).ToList(),
                "every saved key is erased by the reset or deliberately kept");
        }
    }
}
