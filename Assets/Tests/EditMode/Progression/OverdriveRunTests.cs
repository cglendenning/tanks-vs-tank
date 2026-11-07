using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // D-045: Overdrive is supply; it stays on while held, whatever happens to the tank (owner decision 2026-11-07).
    public sealed class OverdriveRunTests
    {
        private sealed class MemoryFlags : IBoostFlags
        {
            private readonly HashSet<string> set = new HashSet<string>();
            public bool IsSet(string key) => set.Contains(key);
            public void Set(string key) => set.Add(key);
            public void Clear(string key) => set.Remove(key);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-045-AC-04: losing the tank and an Armored Redeploy leave Overdrive on while its supply is held")]
        public void TankLossNoLongerEndsOverdrive()
        {
            var boosts = new CombatBoosts(new MemoryFlags(), () => true);
            boosts.ArmRedeploy();
            var reloaded = boosts.ConsumeForMission(3);
            Assert.AreEqual((4, 2), (reloaded.ArmorHits, reloaded.RoundsPerTap), "the redeploy's hit and Overdrive together");
            Assert.IsTrue(boosts.OverdriveActive);
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.DoesNotContain("TreadShredBoosts.Current.TankLost();", results, "a lost tank leaves Overdrive alone");
            StringAssert.Contains("Progression.Supply(TreadShred.Progression.SupplyTrack.Overdrive) > 0", Script("Game/TreadShredBoosts.cs"));
        }

        [Test(Description = "D-045-AC-05: a 1.9.5 save's Overdrive, on or lost to a redeploy, is taken once and becomes one unit of supply")]
        public void LegacyOverdriveIsTakenOnce()
        {
            var flags = new MemoryFlags();
            flags.Set(CombatBoosts.OverdriveKey);
            var boosts = new CombatBoosts(flags, () => false);
            Assert.IsTrue(boosts.TakeLegacyOverdrive());
            Assert.IsFalse(boosts.TakeLegacyOverdrive(), "only once");
            flags.Set(CombatBoosts.OverdriveLostKey);
            Assert.IsTrue(boosts.TakeLegacyOverdrive(), "a lost Overdrive a redeploy could still bring back counts too");
            StringAssert.Contains("if (TreadShredBoosts.Current.TakeLegacyOverdrive())\n            Progression.AddSupply(SupplyTrack.Overdrive);",
                Script("Game/TreadShredProgress.cs"));
        }

        [Test(Description = "D-045-AC-01: Overdrive is a unit of supply at 100 gems, and the offer, its sales screen and the victory card say it lasts at least 5 missions")]
        public void CopySaysHowLongOverdriveLasts()
        {
            Assert.AreEqual(SupplyRules.UnitGems, GemCatalog.Cost(GemOffer.Overdrive));
            Assert.AreEqual("2 SHOTS PER TAP FOR AT LEAST 5 MISSIONS", OfferLabels.OverdriveEffect);
            CollectionAssert.Contains(SalesPitches.For(SaleItem.Overdrive).Points, "FOR AT LEAST 5 MISSIONS");
            StringAssert.Contains("OfferLabels.OverdriveEffect,", Script("ManagerScore/ManagerScore.cs"));
            StringAssert.Contains("TreadShredOverdriveBadge.Show(", Script("Player/PlayerMoverment.cs"));
            StringAssert.Contains("TreadShredOverdriveBadge.SetVisible(false);", Script("ManagerScore/ManagerButton.cs"));
        }
    }
}
