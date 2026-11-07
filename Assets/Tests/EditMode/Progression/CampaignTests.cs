using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class CampaignTests
    {
        [Test(Description = "D-019-AC-01: missions 1-120 are Recruit 1-40, Veteran 41-80 and Elite 81-120, each playing map ((n - 1) mod 40) + 1")]
        public void NumbersMapToTierAndMap()
        {
            Assert.AreEqual(120, Campaign.MissionTotal);
            Assert.AreEqual((DifficultyTier.Recruit, 1), (Campaign.TierOf(1), Campaign.MapOf(1)));
            Assert.AreEqual((DifficultyTier.Recruit, 40), (Campaign.TierOf(40), Campaign.MapOf(40)));
            Assert.AreEqual((DifficultyTier.Veteran, 1), (Campaign.TierOf(41), Campaign.MapOf(41)));
            Assert.AreEqual((DifficultyTier.Veteran, 16), (Campaign.TierOf(56), Campaign.MapOf(56)), "the arc-shot map in Veteran");
            Assert.AreEqual((DifficultyTier.Elite, 40), (Campaign.TierOf(120), Campaign.MapOf(120)));
            for (var n = 1; n <= Campaign.MissionTotal; n++)
                Assert.AreEqual(n, Campaign.Number(Campaign.TierOf(n), Campaign.MapOf(n)));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Campaign.TierOf(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Campaign.TierOf(121));
        }

        [Test(Description = "D-019-AC-01: missions unlock strictly in order, across tiers too")]
        public void MissionsUnlockInOrder()
        {
            var cleared = new HashSet<int>();
            var open = new HashSet<DifficultyTier> { DifficultyTier.Recruit };
            bool Unlocked(int n) => Campaign.IsUnlocked(n, cleared.Contains, open.Contains);
            Assert.IsTrue(Unlocked(1));
            Assert.IsFalse(Unlocked(2));
            for (var n = 1; n <= 39; n++)
                cleared.Add(n);
            Assert.IsTrue(Unlocked(40));
            Assert.IsFalse(Unlocked(41), "Veteran is not open until mission 40 is cleared");
            cleared.Add(40);
            open.Add(DifficultyTier.Veteran);
            Assert.IsTrue(Unlocked(41));
            Assert.IsFalse(Unlocked(42));
        }

        [Test(Description = "D-019-AC-04: NEXT MISSION goes 40 to 41 and 80 to 81, clearing 40 and 80 promotes, and nothing follows 120")]
        public void NextMissionRunsThroughTheTiers()
        {
            Assert.AreEqual(2, Campaign.Next(1));
            Assert.AreEqual(41, Campaign.Next(40));
            Assert.AreEqual(81, Campaign.Next(80));
            Assert.IsNull(Campaign.Next(120));
            Assert.IsTrue(Campaign.Promotes(40));
            Assert.IsTrue(Campaign.Promotes(80));
            Assert.IsFalse(Campaign.Promotes(39));
            Assert.IsFalse(Campaign.Promotes(120), "the last mission ends the campaign rather than promoting");
            Assert.AreEqual("ALL 120 MISSIONS CLEARED", Campaign.Cleared);
        }

        [Test(Description = "D-019-AC-03: the tier is named in the mission list headers and beside the mission number in the HUD")]
        public void TierLabels()
        {
            Assert.AreEqual("RECRUIT · 1–40", Campaign.Header(DifficultyTier.Recruit));
            Assert.AreEqual("VETERAN · 41–80", Campaign.Header(DifficultyTier.Veteran));
            Assert.AreEqual("ELITE · 81–120", Campaign.Header(DifficultyTier.Elite));
            Assert.AreEqual("MISSION 41 · VETERAN", Campaign.HudLabel(41));
            Assert.AreEqual("MISSION 7 · RECRUIT", Campaign.HudLabel(7));
            Assert.AreEqual("PROMOTED TO VETERAN", Campaign.PromotionTitle(DifficultyTier.Veteran));
            Assert.AreEqual("ENEMIES ARE TOUGHER, FASTER AND FIRE MORE OFTEN", Campaign.PromotionLine);
        }
    }
}
