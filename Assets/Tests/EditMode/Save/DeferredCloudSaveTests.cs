using System.Collections.Generic;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Save.Tests
{
    // Android: Play Games saved games load only after sign-in, so the cloud answers later than iCloud does.
    public sealed class DeferredCloudSaveTests
    {
        private sealed class MemoryLocal : ILocalSave
        {
            public SaveSnapshot Saved = new SaveSnapshot();
            public bool Synced { get; private set; }
            public SaveSnapshot Load() => Saved;
            public void Write(SaveSnapshot snapshot) => Saved = snapshot;
            public void Stamp(long writtenAtTicks) => Saved.writtenAtTicks = writtenAtTicks;
            public void MarkSynced() => Synced = true;
        }

        private static SaveSnapshot StarterOnly() => new SaveSnapshot
        {
            wallet = new WalletState { balance = GemCatalog.StarterGrant, starterGranted = true, creditedTransactions = new List<string>() }
        };

        private static SaveSnapshot Paid(int gems, long writtenAt) => new SaveSnapshot
        {
            writtenAtTicks = writtenAt,
            wallet = new WalletState { balance = gems, starterGranted = true, creditedTransactions = new List<string> { "txn-paid" } }
        };

        private MemoryLocal local;
        private DeferredCloudSave cloud;
        private CloudSync sync;
        // The balance of each save sent, recorded when it was sent.
        private List<int> sent;
        private long now = 1000;

        [SetUp]
        public void FreshInstall()
        {
            local = new MemoryLocal { Saved = StarterOnly() };
            sent = new List<int>();
            cloud = new DeferredCloudSave(snapshot => sent.Add(snapshot.wallet.balance));
            sync = new CloudSync(local, cloud, () => ++now);
            Assert.IsFalse(sync.RestoreAtLaunch(), "nothing is known about the cloud at launch");
        }

        // The game's handling of the plugin's OnCloudLoaded message (TreadShredCloudSave).
        private bool Loaded(SaveSnapshot fromCloud)
        {
            cloud.Arrived(fromCloud);
            var applied = sync.CloudChanged(_ => { });
            if (cloud.ReleaseHeld(applied))
                sync.LocalSaved();
            return applied;
        }

        [Test(Description = "D-025-AC-02: on Android a fresh install takes the Play Games saved game whole when it arrives, so the starter grant never stacks")]
        public void FreshInstallTakesTheSavedGameWhole()
        {
            Assert.IsTrue(Loaded(Paid(550, 10)));
            Assert.AreEqual(550, local.Saved.wallet.balance, "the saved balance, not 550 + the starter 100");
            Assert.IsTrue(local.Synced);
            CollectionAssert.IsEmpty(sent);
        }

        [Test(Description = "D-025-AC-02: a fresh install never uploads before the saved game has loaded, even after earning something")]
        public void NothingUploadsBeforeTheSavedGameLoads()
        {
            local.Saved.wallet.balance = 140;
            sync.LocalSaved();
            CollectionAssert.IsEmpty(sent, "held until Play Games has answered");

            Assert.IsTrue(Loaded(Paid(550, 10)), "an install that never synced gives way to the player's history");
            Assert.AreEqual(550, local.Saved.wallet.balance);
            CollectionAssert.IsEmpty(sent, "the held local save is dropped, never uploaded over the saved game");
        }

        [Test(Description = "D-025-AC-04: while Play Games is unavailable the game plays on local data, and the save goes up once it answers")]
        public void LocalProgressGoesUpOnceAvailable()
        {
            local.Saved.wallet.balance = 140;
            sync.LocalSaved();
            CollectionAssert.IsEmpty(sent);

            Assert.IsFalse(Loaded(null), "the player had no saved game yet");

            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual(140, sent[0]);
            Assert.IsTrue(local.Synced);
        }

        [Test(Description = "D-025-AC-01: once the saved game has loaded, every change with progress is saved to Play Games")]
        public void EveryChangeGoesUpAfterLoading()
        {
            Loaded(null);
            local.Saved.wallet.balance = 160;
            sync.LocalSaved();
            local.Saved.wallet.balance = 180;
            sync.LocalSaved();
            Assert.AreEqual(new[] { 160, 180 }, sent);
        }
    }
}
