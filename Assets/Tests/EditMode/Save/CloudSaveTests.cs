using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

namespace TreadShred.Save.Tests
{
    public sealed class CloudSaveTests
    {
        private sealed class MemoryWalletStore : IWalletStore
        {
            public WalletState Saved;
            public WalletState Load() => Saved;
            public void Save(WalletState state) => Saved = state;
        }

        private sealed class MemoryProgressionStore : IProgressionStore
        {
            public ProgressionState Saved;
            public ProgressionState Load() => Saved;
            public void Save(ProgressionState state) => Saved = state;
        }

        private static SaveSnapshot Snapshot(long writtenAt, int gems, int coins, bool starterGranted = true) => new SaveSnapshot
        {
            writtenAtTicks = writtenAt,
            wallet = new WalletState { balance = gems, starterGranted = starterGranted },
            progression = new ProgressionState { coins = coins },
        };

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-025-AC-03: when local and cloud differ, the more recently written save wins")]
        public void NewerSaveWins()
        {
            var older = Snapshot(100, 340, 900);
            var newer = Snapshot(200, 60, 1500);
            Assert.AreEqual(SaveSource.Cloud, SaveRules.Choose(older, newer));
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(newer, older));
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(newer, Snapshot(200, 999, 0)), "a tie keeps the local save");
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(newer, null), "no cloud save: play on local data");
        }

        [Test(Description = "D-025-AC-03: a restored save replaces the wallet and progression as a whole, never adding balances")]
        public void RestoreReplacesWholeNeverAdds()
        {
            var wallet = new GemWallet(new MemoryWalletStore());
            wallet.GrantStarterIfNeeded();
            wallet.CreditPurchase(GemCatalog.Packs[0].ProductId, "local-txn");
            var progression = new PlayerProgression(new MemoryProgressionStore(), wallet);
            progression.CompleteMission(DifficultyTier.Recruit, 1, true);

            wallet.Replace(new WalletState { balance = 550, starterGranted = true, creditedTransactions = new List<string> { "cloud-txn" } });
            progression.Replace(new ProgressionState { coins = 4200, upgradeLevels = new[] { 3, 1, 0 }, clearedMissions = new long[] { 7, 0, 0 } });

            Assert.AreEqual(550, wallet.Balance, "the cloud balance, not local + cloud");
            Assert.AreEqual(CreditResult.Duplicate, wallet.CreditPurchase(GemCatalog.Packs[0].ProductId, "cloud-txn"),
                "the restored transaction history still blocks double credits");
            Assert.AreEqual(4200, progression.Coins);
            Assert.AreEqual(3, progression.Level(UpgradeTrack.Armor));
            Assert.IsTrue(progression.IsCleared(DifficultyTier.Recruit, 1));
        }

        [Test(Description = "D-025-AC-02: a fresh install takes the cloud save, so the starter grant is never given twice")]
        public void FreshInstallTakesTheCloudSave()
        {
            var freshInstall = Snapshot(writtenAt: 999999, gems: GemCatalog.StarterGrant, coins: 0);
            var cloud = Snapshot(writtenAt: 1000, gems: 40, coins: 2300);
            Assert.IsFalse(SaveRules.HasProgress(freshInstall), "starter gems alone are not progress");
            Assert.AreEqual(SaveSource.Cloud, SaveRules.Choose(freshInstall, cloud),
                "even though the fresh install's save is newer");

            var restored = new GemWallet(new MemoryWalletStore { Saved = cloud.wallet });
            Assert.IsFalse(restored.GrantStarterIfNeeded(), "the restored wallet already had its starter grant");
            Assert.AreEqual(40, restored.Balance);

            var emptyCloud = Snapshot(writtenAt: 5, gems: GemCatalog.StarterGrant, coins: 0);
            Assert.AreEqual(SaveSource.Local, SaveRules.Choose(freshInstall, emptyCloud), "a cloud save with no progress never wins");
        }

        [Test(Description = "D-025-AC-01: the wallet and progression are pushed to the cloud after every save")]
        public void EverySaveIsPushedToTheCloud()
        {
            StringAssert.Contains("TreadShredCloudSave.LocalSaved();", Read("Script/Store/PlayerPrefsWalletStore.cs"));
            StringAssert.Contains("TreadShredCloudSave.LocalSaved();", Read("Script/Game/PlayerPrefsProgressionStore.cs"));
            var bridge = Read("Script/Game/TreadShredCloudSave.cs");
            StringAssert.Contains("RuntimeInitializeLoadType.BeforeSceneLoad", bridge, "restore runs before the store grants starter gems");
            StringAssert.Contains("TreadShredCloudStore(JsonUtility.ToJson(snapshot))", bridge);
            Assert.IsTrue(SaveRules.HasProgress(Snapshot(0, GemCatalog.StarterGrant, 20)), "a mission's coins are progress worth saving");
        }

        [Test(Description = "D-025-AC-04: cloud save never shows a login screen and play continues without it")]
        public void NoLoginScreen()
        {
            var plugin = Read("Plugins/iOS/TreadShredCloud.m");
            StringAssert.Contains("NSUbiquitousKeyValueStore", plugin);
            foreach (var ui in new[] { "presentViewController", "UIAlertController", "authenticateHandler" })
                StringAssert.DoesNotContain(ui, plugin);
            StringAssert.Contains("return null;", Read("Script/Game/TreadShredCloudSave.cs"), "a missing or unreadable cloud save leaves local data in play");
        }
    }
}
