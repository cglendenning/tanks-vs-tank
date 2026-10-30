using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    public class PlayerProgressionTests
    {
        private sealed class MemoryProgressionStore : IProgressionStore
        {
            public ProgressionState Saved;
            public ProgressionState Load() => Saved;
            public void Save(ProgressionState state) => Saved = state;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            public WalletState Saved;
            public WalletState Load() => Saved;
            public void Save(WalletState state) => Saved = state;
        }

        private MemoryProgressionStore store;
        private MemoryWalletStore walletStore;
        private GemWallet wallet;
        private PlayerProgression progression;

        [SetUp]
        public void SetUp()
        {
            store = new MemoryProgressionStore();
            walletStore = new MemoryWalletStore();
            wallet = new GemWallet(walletStore);
            progression = new PlayerProgression(store, wallet);
        }

        private void EarnCoins(int wins)
        {
            for (var i = 0; i < wins; i++)
                progression.CompleteMission(DifficultyTier.Recruit, 1, true);
        }

        private void ClearAll(DifficultyTier tier)
        {
            for (var mission = 1; mission <= ProgressionRules.MissionCount; mission++)
                progression.CompleteMission(tier, mission, true);
        }

        [Test(Description = "D-017-AC-01: three tracks of ten levels that persist across sessions")]
        public void TracksHaveTenLevelsAndPersist()
        {
            // All 30 upgrades cost 33,000 coins.
            EarnCoins(330);
            foreach (UpgradeTrack track in System.Enum.GetValues(typeof(UpgradeTrack)))
            {
                for (var level = 0; level < ProgressionRules.LevelsPerTrack; level++)
                    Assert.AreEqual(UpgradeResult.Upgraded, progression.TryUpgrade(track));
                Assert.AreEqual(UpgradeResult.AlreadyMaxed, progression.TryUpgrade(track));
            }

            var reloaded = new PlayerProgression(store, wallet);

            Assert.AreEqual(3, System.Enum.GetValues(typeof(UpgradeTrack)).Length);
            Assert.AreEqual(10, reloaded.Level(UpgradeTrack.Armor));
            Assert.AreEqual(10, reloaded.Level(UpgradeTrack.Speed));
        }

        [Test(Description = "D-017-AC-03: level n costs n times 200 coins")]
        public void LevelCostScalesWithLevel()
        {
            EarnCoins(200);
            for (var level = 1; level <= ProgressionRules.LevelsPerTrack; level++)
            {
                Assert.AreEqual(level * 200, progression.Quote(UpgradeTrack.Firepower).CostCoins);
                var before = progression.Coins;
                progression.TryUpgrade(UpgradeTrack.Firepower);
                Assert.AreEqual(before - level * 200, progression.Coins);
            }
        }

        [Test(Description = "D-017-AC-04: armor, firepower and speed effects per level")]
        public void UpgradeEffectsMatchRules()
        {
            Assert.AreEqual(3, UpgradeEffects.ArmorHits(0));
            Assert.AreEqual(3, UpgradeEffects.ArmorHits(1));
            Assert.AreEqual(4, UpgradeEffects.ArmorHits(2));
            Assert.AreEqual(8, UpgradeEffects.ArmorHits(10));
            Assert.AreEqual(1.0f, UpgradeEffects.DamageMultiplier(0), 1e-5f);
            Assert.AreEqual(2.0f, UpgradeEffects.DamageMultiplier(10), 1e-5f);
            Assert.AreEqual(1.25f, UpgradeEffects.SpeedMultiplier(5), 1e-5f);
            Assert.AreEqual(1.5f, UpgradeEffects.SpeedMultiplier(10), 1e-5f);
        }

        [Test(Description = "D-018-AC-01: missions pay tier-scaled coins that persist")]
        public void MissionsPayCoinsScaledByTier()
        {
            Assert.AreEqual(100, progression.CompleteMission(DifficultyTier.Recruit, 1, true).Coins);
            Assert.AreEqual(20, progression.CompleteMission(DifficultyTier.Recruit, 2, false).Coins);
            ClearAll(DifficultyTier.Recruit);
            Assert.AreEqual(200, progression.CompleteMission(DifficultyTier.Veteran, 1, true).Coins);
            Assert.AreEqual(40, progression.CompleteMission(DifficultyTier.Veteran, 2, false).Coins);
            ClearAll(DifficultyTier.Veteran);
            Assert.AreEqual(300, progression.CompleteMission(DifficultyTier.Elite, 1, true).Coins);

            var coins = progression.Coins;
            Assert.AreEqual(coins, new PlayerProgression(store, wallet).Coins);
        }

        [Test(Description = "D-018-AC-02: gems cover a coin shortfall at 20 coins per gem in one action")]
        public void GemsCoverCoinShortfall()
        {
            wallet.GrantStarterIfNeeded();
            EarnCoins(1);
            // Level 1 costs 200; 100 coins short means 5 gems.
            Assert.AreEqual(5, progression.Quote(UpgradeTrack.Armor).GemsNeeded);

            Assert.AreEqual(UpgradeResult.Upgraded, progression.TryUpgrade(UpgradeTrack.Armor));

            Assert.AreEqual(1, progression.Level(UpgradeTrack.Armor));
            Assert.AreEqual(GemCatalog.StarterGrant - 5, wallet.Balance);
            Assert.AreEqual(0, progression.Coins);
        }

        [Test(Description = "D-018: an upgrade fails without changes when gems cannot cover the shortfall")]
        public void UpgradeFailsWhenGemsCannotCover()
        {
            Assert.AreEqual(UpgradeResult.NotEnoughGems, progression.TryUpgrade(UpgradeTrack.Speed));
            Assert.AreEqual(0, progression.Level(UpgradeTrack.Speed));
            Assert.AreEqual(0, progression.Coins);
        }

        [Test(Description = "D-019-AC-01: tiers unlock in order after clearing every mission")]
        public void TiersUnlockInOrder()
        {
            Assert.IsTrue(progression.IsUnlocked(DifficultyTier.Recruit));
            Assert.IsFalse(progression.IsUnlocked(DifficultyTier.Veteran));

            for (var mission = 1; mission < ProgressionRules.MissionCount; mission++)
                progression.CompleteMission(DifficultyTier.Recruit, mission, true);
            Assert.IsFalse(progression.IsUnlocked(DifficultyTier.Veteran));

            var reward = progression.CompleteMission(DifficultyTier.Recruit, ProgressionRules.MissionCount, true);
            Assert.AreEqual(DifficultyTier.Veteran, reward.UnlockedTier);
            Assert.IsTrue(progression.IsUnlocked(DifficultyTier.Veteran));
            Assert.IsFalse(progression.IsUnlocked(DifficultyTier.Elite));

            ClearAll(DifficultyTier.Veteran);
            Assert.IsTrue(progression.IsUnlocked(DifficultyTier.Elite));
            Assert.Throws<System.InvalidOperationException>(() =>
                new PlayerProgression(new MemoryProgressionStore(), wallet).CompleteMission(DifficultyTier.Elite, 1, true));
        }

        [Test(Description = "D-019: tougher enemies and bigger payouts on higher tiers")]
        public void TierModifiersMatchRules()
        {
            Assert.AreEqual(1.5f, ProgressionRules.Modifiers(DifficultyTier.Veteran).EnemyHealth, 1e-5f);
            Assert.AreEqual(1.2f, ProgressionRules.Modifiers(DifficultyTier.Veteran).EnemySpeed, 1e-5f);
            Assert.AreEqual(2.25f, ProgressionRules.Modifiers(DifficultyTier.Elite).EnemyHealth, 1e-5f);
            Assert.AreEqual(1.4f, ProgressionRules.Modifiers(DifficultyTier.Elite).EnemySpeed, 1e-5f);
            Assert.AreEqual(3, ProgressionRules.Modifiers(DifficultyTier.Elite).Coins);
        }

        [Test(Description = "D-019: mission progress is tracked separately per tier")]
        public void ProgressIsTrackedPerTier()
        {
            ClearAll(DifficultyTier.Recruit);
            progression.CompleteMission(DifficultyTier.Veteran, 3, true);

            Assert.AreEqual(40, progression.ClearedCount(DifficultyTier.Recruit));
            Assert.AreEqual(1, progression.ClearedCount(DifficultyTier.Veteran));
            Assert.IsTrue(progression.IsCleared(DifficultyTier.Veteran, 3));
            Assert.IsFalse(progression.IsCleared(DifficultyTier.Veteran, 4));
        }

        [Test(Description = "D-026-AC-01: milestone gems are paid exactly once, including after reload")]
        public void MilestoneGemsPaidOnce()
        {
            Assert.AreEqual(0, progression.CompleteMission(DifficultyTier.Recruit, 9, true).Gems);
            Assert.AreEqual(20, progression.CompleteMission(DifficultyTier.Recruit, 10, true).Gems);
            Assert.AreEqual(0, progression.CompleteMission(DifficultyTier.Recruit, 10, true).Gems);

            var reloaded = new PlayerProgression(store, wallet);
            Assert.AreEqual(0, reloaded.CompleteMission(DifficultyTier.Recruit, 10, true).Gems);
            Assert.AreEqual(20, wallet.Balance);
        }

        [Test(Description = "D-026: all milestones total 150 gems")]
        public void AllMilestonesTotal150Gems()
        {
            ClearAll(DifficultyTier.Recruit);
            ClearAll(DifficultyTier.Veteran);
            Assert.AreEqual(150, wallet.Balance);
        }

        [Test(Description = "D-026: a mission loss pays no milestone")]
        public void LossPaysNoMilestone()
        {
            Assert.AreEqual(0, progression.CompleteMission(DifficultyTier.Recruit, 10, false).Gems);
            Assert.IsFalse(progression.IsCleared(DifficultyTier.Recruit, 10));
        }

        [Test(Description = "D-020-AC-04: at most two revives per mission")]
        public void RevivesAreLimitedPerMission()
        {
            Assert.IsTrue(ReviveRules.CanOffer(0));
            Assert.IsTrue(ReviveRules.CanOffer(1));
            Assert.IsFalse(ReviveRules.CanOffer(2));
        }

        [Test(Description = "D-020: revives cost 30 then 60 gems")]
        public void ReviveCostsEscalate()
        {
            Assert.AreEqual(30, ReviveRules.CostGems(0));
            Assert.AreEqual(60, ReviveRules.CostGems(1));
        }

        [Test(Description = "D-026: wallet rewards add gems and reject non-positive amounts")]
        public void WalletRewardCredits()
        {
            wallet.CreditReward(20);
            Assert.AreEqual(20, wallet.Balance);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => wallet.CreditReward(0));
        }
    }
}
