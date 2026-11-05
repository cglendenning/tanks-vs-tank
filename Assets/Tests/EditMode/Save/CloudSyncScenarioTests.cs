using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;

namespace TreadShred.Save.Tests
{
    /// <summary>
    /// D-025 end to end: the real wallet, progression and CloudSync against a fake iCloud
    /// that behaves as Apple documents. Reinstalls, late first syncs, second devices and
    /// offline play are all simulated here instead of on a phone.
    /// </summary>
    public sealed class CloudSyncScenarioTests
    {
        private long now = 1000;
        private long Clock() => now += 10;

        private static string Pack => GemCatalog.Packs[0].ProductId;

        // A player's history: two missions, a gem pack, an upgrade, a boost bought with gems.
        private static void PlayHistory(Install install)
        {
            install.Progression.CompleteMission(DifficultyTier.Recruit, 1, true);
            install.Progression.CompleteMission(DifficultyTier.Recruit, 2, true);
            install.Wallet.CreditPurchase(Pack, "txn-history");
            install.Progression.TryUpgrade(UpgradeTrack.Armor);
            install.Wallet.TrySpend(60);
        }

        private static void AssertSameProgress(Install expected, Install actual)
        {
            Assert.AreEqual(expected.Wallet.Balance, actual.Wallet.Balance, "gems");
            Assert.AreEqual(expected.Progression.Coins, actual.Progression.Coins, "coins");
            Assert.AreEqual(expected.Progression.Level(UpgradeTrack.Armor), actual.Progression.Level(UpgradeTrack.Armor), "armor");
            Assert.AreEqual(expected.Progression.ClearedCount(DifficultyTier.Recruit), actual.Progression.ClearedCount(DifficultyTier.Recruit), "missions");
        }

        [Test(Description = "D-025-AC-01: every change to gems, purchases, coins, upgrades and missions reaches the cloud")]
        public void EveryChangeReachesTheCloud()
        {
            var cloud = new FakeICloud();
            var phone = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();

            phone.Progression.CompleteMission(DifficultyTier.Recruit, 1, true);
            Assert.AreEqual(phone.Progression.Coins, cloud.Server.progression.coins);
            phone.Wallet.CreditPurchase(Pack, "txn-1");
            Assert.AreEqual(phone.Wallet.Balance, cloud.Server.wallet.balance);
            CollectionAssert.Contains(cloud.Server.wallet.creditedTransactions, "txn-1");
            phone.Progression.CompleteMission(DifficultyTier.Recruit, 2, true);
            phone.Progression.TryUpgrade(UpgradeTrack.Firepower);
            Assert.AreEqual(1, cloud.Server.progression.upgradeLevels[(int)UpgradeTrack.Firepower]);
            phone.Wallet.TrySpend(40);
            Assert.AreEqual(phone.Wallet.Balance, cloud.Server.wallet.balance);
            Assert.AreNotEqual(0L, cloud.Server.progression.clearedMissions[0]);
            Assert.IsTrue(cloud.Server.wallet.starterGranted);
        }

        [Test(Description = "D-025-AC-02: a reinstall restores everything at launch, before the starter grant, so it is never given twice")]
        public void ReinstallRestoresBeforeTheStarterGrant()
        {
            var cloud = new FakeICloud();
            var first = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            PlayHistory(first);

            var reinstall = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();

            AssertSameProgress(first, reinstall);
            Assert.AreEqual(CreditResult.Duplicate, reinstall.Wallet.CreditPurchase(Pack, "txn-history"),
                "a redelivered purchase is still recognised after the reinstall");
        }

        [Test(Description = "D-025-AC-02: when the first iCloud sync arrives after launch, the restored save replaces the starter state whole")]
        public void LateFirstSyncReplacesTheStarterState()
        {
            var cloud = new FakeICloud();
            var first = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            PlayHistory(first);

            var device = cloud.NewInstall(firstSyncDone: false);
            var reinstall = new Install(device, Clock).Launch();
            Assert.AreEqual(GemCatalog.StarterGrant, reinstall.Wallet.Balance, "before the sync: a fresh start");

            device.CompleteFirstSync();

            AssertSameProgress(first, reinstall);
            Assert.AreNotEqual(first.Wallet.Balance + GemCatalog.StarterGrant, reinstall.Wallet.Balance, "the starter gems did not stack");
        }

        [Test(Description = "D-025-AC-02: playing before the first iCloud sync arrives cannot overwrite the player's history")]
        public void PlayBeforeTheFirstSyncCannotEraseHistory()
        {
            var cloud = new FakeICloud();
            var first = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            PlayHistory(first);
            var history = cloud.ServerJson;

            var device = cloud.NewInstall(firstSyncDone: false);
            var reinstall = new Install(device, Clock).Launch();
            reinstall.Progression.CompleteMission(DifficultyTier.Recruit, 1, false);
            Assert.Greater(device.DiscardedWrites, 0, "iCloud discarded the writes made before its first sync");
            Assert.AreEqual(history, cloud.ServerJson);

            device.CompleteFirstSync();

            AssertSameProgress(first, reinstall);
            Assert.IsTrue(reinstall.Synced);
            reinstall.Progression.CompleteMission(DifficultyTier.Recruit, 3, true);
            Assert.AreEqual(reinstall.Progression.Coins, cloud.Server.progression.coins, "once synced, play saves to the cloud again");
        }

        [Test(Description = "D-025-AC-03: across two devices the most recent save wins as a whole and balances are never added")]
        public void NewestSaveWinsAcrossDevices()
        {
            var cloud = new FakeICloud();
            var phone = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            PlayHistory(phone);
            var tablet = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            AssertSameProgress(phone, tablet);

            tablet.Wallet.TrySpend(100);
            tablet.Progression.CompleteMission(DifficultyTier.Recruit, 3, true);

            AssertSameProgress(tablet, phone);
            Assert.AreEqual(tablet.Wallet.Balance, phone.Wallet.Balance, "the phone took the tablet's balance, not the sum");
        }

        [Test(Description = "D-025-AC-04: offline, the game plays on local data and the latest save uploads once the device is back online")]
        public void OfflinePlayUploadsWhenBackOnline()
        {
            var cloud = new FakeICloud();
            var device = cloud.NewInstall(firstSyncDone: true);
            var phone = new Install(device, Clock).Launch();
            phone.Progression.CompleteMission(DifficultyTier.Recruit, 1, true);
            var beforeOffline = cloud.ServerJson;

            device.GoOffline();
            phone.Progression.CompleteMission(DifficultyTier.Recruit, 2, true);
            phone.Wallet.CreditPurchase(Pack, "txn-offline");
            Assert.AreEqual(beforeOffline, cloud.ServerJson, "nothing reaches the server while offline");
            Assert.AreEqual(100, phone.Progression.Coins, "play carries on with local data");

            device.GoOnline();

            Assert.AreEqual(phone.Progression.Coins, cloud.Server.progression.coins);
            Assert.AreEqual(phone.Wallet.Balance, cloud.Server.wallet.balance);
        }

        [Test(Description = "D-025-AC-01: the very first install, with an empty cloud, starts saving to it as soon as there is progress")]
        public void FirstEverInstallStartsTheCloudSave()
        {
            var cloud = new FakeICloud();
            var phone = new Install(cloud.NewInstall(firstSyncDone: true), Clock).Launch();
            Assert.IsNull(cloud.Server, "starter gems alone are not uploaded");

            phone.Progression.CompleteMission(DifficultyTier.Recruit, 1, false);

            Assert.IsNotNull(cloud.Server);
            Assert.IsTrue(phone.Synced);
        }
    }
}
