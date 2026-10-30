using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    public class ProgressionLabelsTests
    {
        [Test(Description = "D-010-AC-01: any gem cost spends directly or picks the smallest covering pack")]
        public void PlanForAnyGemCost()
        {
            Assert.IsTrue(GemCatalog.PlanFor(30, 30).CanSpendNow);
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.PlanFor(60, 10).PackToBuy);
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.PlanFor(300, 0).PackToBuy);
        }

        [Test(Description = "D-026: the result screen names coins, milestone gems and tier unlocks")]
        public void RewardLineNamesEverythingEarned()
        {
            Assert.AreEqual("+100 COINS", ProgressionLabels.Reward(new MissionReward(100, 0, null)));
            Assert.AreEqual("+100 COINS   +20 GEMS MILESTONE REWARD   VETERAN UNLOCKED",
                ProgressionLabels.Reward(new MissionReward(100, 20, DifficultyTier.Veteran)));
        }

        [Test(Description = "D-017: upgrade rows show the current and next effect")]
        public void EffectLabelsShowUpgradeValues()
        {
            Assert.AreEqual("3 HITS", ProgressionLabels.Effect(UpgradeTrack.Armor, 0));
            Assert.AreEqual("X1.5 DAMAGE", ProgressionLabels.Effect(UpgradeTrack.Firepower, 5));
            Assert.AreEqual("X1.50 SPEED", ProgressionLabels.Effect(UpgradeTrack.Speed, 10));
        }

        [Test(Description = "D-018: the upgrade button names coins, plus gems when coins are short")]
        public void UpgradeButtonNamesCurrencies()
        {
            Assert.AreEqual("UPGRADE FOR 200 COINS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 0), false));
            Assert.AreEqual("UPGRADE FOR 100 COINS + 5 GEMS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false));
            Assert.AreEqual("UPGRADE FOR 10 GEMS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 200), false));
            Assert.AreEqual("MAXED", ProgressionLabels.UpgradeButton(new UpgradeQuote(0, 0), true));
        }

        [Test(Description = "D-017: the upgrade preview names the level where the next armor hit arrives")]
        public void PreviewShowsNextGain()
        {
            Assert.AreEqual("3 HITS  >  4 HITS AT LEVEL 2", ProgressionLabels.Preview(UpgradeTrack.Armor, 0));
            Assert.AreEqual("3 HITS  >  4 HITS", ProgressionLabels.Preview(UpgradeTrack.Armor, 1));
            Assert.AreEqual("X1.0 DAMAGE  >  X1.1 DAMAGE", ProgressionLabels.Preview(UpgradeTrack.Firepower, 0));
            Assert.AreEqual("8 HITS", ProgressionLabels.Preview(UpgradeTrack.Armor, 10));
        }
    }
}
