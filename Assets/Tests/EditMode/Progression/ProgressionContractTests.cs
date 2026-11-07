using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ProgressionContractTests
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

        private sealed class FakeGateway : IPurchaseGateway
        {
            public readonly List<string> Started = new List<string>();
            public bool TryStartPurchase(string productId)
            {
                Started.Add(productId);
                return true;
            }
        }

        private static PlayerProgression NewProgression(GemWallet wallet = null) =>
            new PlayerProgression(new MemoryProgressionStore(), wallet ?? new GemWallet(new MemoryWalletStore()));

        private static void ClearTier(PlayerProgression progression, DifficultyTier tier)
        {
            for (var mission = 1; mission <= ProgressionRules.MissionCount; mission++)
                progression.CompleteMission(tier, mission, true);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-017-AC-02: the upgrade screen shows a track's current and next level before buying")]
        public void PreviewShowsCurrentAndNext()
        {
            Assert.AreEqual("X1.0 DAMAGE  >  X1.1 DAMAGE", ProgressionLabels.Preview(UpgradeTrack.Firepower, 0));
            Assert.AreEqual("X1.00 SPEED  >  X1.05 SPEED", ProgressionLabels.Preview(UpgradeTrack.Speed, 0));
            Assert.AreEqual("3 HITS  >  4 HITS AT LEVEL 2", ProgressionLabels.Preview(UpgradeTrack.Armor, 0));
            StringAssert.Contains(": ProgressionLabels.Preview(track, Progression.Level(track));", Script("Game/TreadShredUpgradePanel.cs"),
                "the coin ladder's preview; armor and a track at level 10 preview supply (D-045)");
        }

        [Test(Description = "D-017-AC-02: every mission reads the upgrade effects when it starts")]
        public void EveryMissionAppliesUpgradeEffects()
        {
            var player = Script("Player/PlayerMoverment.cs");
            StringAssert.Contains("ConsumeForMission(progress.ArmorHits)", player);
            StringAssert.Contains("Speed *= progress.SpeedMultiplier", player);
            StringAssert.Contains("TreadShredProgress.Ensure().DamageMultiplier", Script("Emnemy/EmnemyRed.cs"));
        }

        [Test(Description = "D-019-AC-02: each higher tier makes enemies tougher and faster and pays more coins, by R-DIFFICULTY-TIERS")]
        public void HigherTiersAreHarderAndPayMore()
        {
            var recruit = ProgressionRules.Modifiers(DifficultyTier.Recruit);
            var veteran = ProgressionRules.Modifiers(DifficultyTier.Veteran);
            var elite = ProgressionRules.Modifiers(DifficultyTier.Elite);
            Assert.AreEqual((1f, 1f, 1f, 1), (recruit.EnemyHealth, recruit.EnemySpeed, recruit.EnemyFireRate, recruit.Coins));
            Assert.AreEqual((2f, 1.3f, 1.75f, 2), (veteran.EnemyHealth, veteran.EnemySpeed, veteran.EnemyFireRate, veteran.Coins));
            Assert.AreEqual((3f, 1.5f, 2.6f, 3), (elite.EnemyHealth, elite.EnemySpeed, elite.EnemyFireRate, elite.Coins));

            var progression = NewProgression();
            ClearTier(progression, DifficultyTier.Recruit);
            Assert.AreEqual(ProgressionRules.CoinsPerWin * 2, progression.CompleteMission(DifficultyTier.Veteran, 1, true).Coins);

            StringAssert.Contains("TierModifiers.EnemyHealth", Script("Emnemy/EmnemyRed.cs"));
            StringAssert.Contains("TierModifiers.EnemySpeed", Script("Emnemy/MoveEmnemy.cs"));
            StringAssert.Contains("TierModifiers.EnemyFireRate", Script("Emnemy/BulletEmnemy.cs"));
        }

        [Test(Description = "D-019-AC-03: mission progress is tracked separately per tier, and mission select sets the tier")]
        public void ProgressIsTrackedPerTier()
        {
            var progression = NewProgression();
            progression.CompleteMission(DifficultyTier.Recruit, 3, true);
            ClearTier(progression, DifficultyTier.Recruit);
            progression.CompleteMission(DifficultyTier.Veteran, 5, true);

            Assert.IsTrue(progression.IsCleared(DifficultyTier.Veteran, 5));
            Assert.IsFalse(progression.IsCleared(DifficultyTier.Veteran, 3), "a Recruit clear does not count on Veteran");
            Assert.AreEqual(1, progression.ClearedCount(DifficultyTier.Veteran));
            Assert.AreEqual(ProgressionRules.MissionCount, progression.ClearedCount(DifficultyTier.Recruit));
            Assert.AreEqual(0, progression.ClearedCount(DifficultyTier.Elite));
            // D-019: the mission number sets the tier; there is no tier picker.
            StringAssert.Contains("progress.SelectTier(tier)", Script("Game/TreadShredCampaign.cs"));
            Assert.IsFalse(File.Exists(Path.Combine(Application.dataPath, "Script/Game/TreadShredTierSelector.cs")));
        }

        [Test(Description = "D-026-AC-02: the result screen says when a milestone reward was earned and how many gems it paid")]
        public void ResultScreenNamesMilestoneGems()
        {
            var progression = NewProgression();
            MissionReward reward = default;
            for (var mission = 1; mission <= 10; mission++)
                reward = progression.CompleteMission(DifficultyTier.Recruit, mission, true);

            Assert.AreEqual("+50 COINS   +20 GEMS MILESTONE REWARD", ProgressionLabels.Reward(reward));
            Assert.AreEqual("+50 COINS", ProgressionLabels.Reward(progression.CompleteMission(DifficultyTier.Recruit, 10, true)),
                "a repeat clear pays no milestone and says nothing about one");
            StringAssert.Contains("ProgressionLabels.Reward(reward.Value)", Script("ManagerScore/ManagerScore.cs"));
        }

        [Test(Description = "D-020-AC-01: the revive offer stays up for offer_seconds, pausing while the purchase sheet is open")]
        public void OfferStaysUpForOfferSeconds()
        {
            var offer = new ReviveOffer(0);
            Assert.AreEqual(12, offer.SecondsShown);

            Assert.IsFalse(offer.Tick(11.5f, purchaseOpen: false));
            Assert.IsFalse(offer.Tick(30f, purchaseOpen: true), "the clock waits for the purchase sheet");
            Assert.AreEqual(ReviveOutcome.Pending, offer.Outcome);
            Assert.IsTrue(offer.Tick(0.6f, purchaseOpen: false));
            Assert.AreEqual(ReviveOutcome.Declined, offer.Outcome);
        }

        [Test(Description = "D-020-AC-01: accepting revives the tank in place with full mission armor and no reload")]
        public void AcceptingRevivesInPlace()
        {
            var offer = new ReviveOffer(0);
            Assert.IsTrue(offer.Accept());
            Assert.AreEqual(ReviveOutcome.Revived, offer.Outcome);
            Assert.IsFalse(offer.Decline(), "a closed offer cannot change");

            var player = Script("Player/PlayerMoverment.cs");
            var revive = player.Substring(player.IndexOf("public void Revive()"), 400);
            StringAssert.Contains("countdie = missionArmorHits;", revive);
            StringAssert.DoesNotContain("LoadScene", revive);
        }

        [Test(Description = "D-020-AC-02: each revive costs the next amount in costs_gems, and a short balance buys the covering pack then revives in one tap")]
        public void ReviveCostsAndOneTapPurchase()
        {
            Assert.AreEqual(30, new ReviveOffer(0).Cost);
            Assert.AreEqual(60, new ReviveOffer(1).Cost);
            Assert.Throws<System.InvalidOperationException>(() => new ReviveOffer(2), "only max_per_mission revives");

            var wallet = new GemWallet(new MemoryWalletStore());
            wallet.CreditReward(10);
            var gateway = new FakeGateway();
            var flow = new PurchaseFlow(wallet, gateway);
            var offer = new ReviveOffer(0);
            flow.SpendOrBuy(offer.Cost, () => offer.Accept(), _ => Assert.Fail("no status expected"));
            flow.OrderDelivered(gateway.Started[0], "txn-revive", out _);

            Assert.AreEqual(ReviveOutcome.Revived, offer.Outcome);
            Assert.AreEqual(10 + GemCatalog.Packs[0].Gems - 30, wallet.Balance);
        }

        [Test(Description = "D-020-AC-03: declining or timing out leads to the normal failure screen with Armored Redeploy")]
        public void DecliningShowsFailureScreen()
        {
            var declined = new ReviveOffer(0);
            Assert.IsTrue(declined.Decline());
            Assert.AreEqual(ReviveOutcome.Declined, declined.Outcome);

            var panel = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("player.Die()", panel);
            var player = Script("Player/PlayerMoverment.cs");
            StringAssert.Contains("ManagerScore.instance.Failure()", player.Substring(player.IndexOf("public void Die()")));
            var score = Script("ManagerScore/ManagerScore.cs");
            var failure = score.Substring(score.IndexOf("public void Failure()"), 1200);
            StringAssert.Contains("GemOffer.ArmoredRedeploy", failure);
        }
    }
}
