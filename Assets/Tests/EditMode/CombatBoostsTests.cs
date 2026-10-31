using System.Collections.Generic;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Economy.Tests
{
    public sealed class CombatBoostsTests
    {
        private sealed class MemoryFlags : IBoostFlags
        {
            private readonly HashSet<string> set = new HashSet<string>();
            public bool IsSet(string key) => set.Contains(key);
            public void Set(string key) => set.Add(key);
            public void Clear(string key) => set.Remove(key);
        }

        [Test(Description = "D-009-AC-03: Armored Redeploy gives the reloaded mission exactly one extra hit, once")]
        public void RedeployAddsOneHitToTheNextMissionOnly()
        {
            var boosts = new CombatBoosts(new MemoryFlags());
            boosts.ArmRedeploy();

            var reloaded = boosts.ConsumeForMission(3);
            var after = boosts.ConsumeForMission(3);

            Assert.AreEqual(4, reloaded.ArmorHits);
            Assert.AreEqual(1, reloaded.RoundsPerTap);
            Assert.AreEqual(3, after.ArmorHits, "the extra hit is used up by the reloaded mission");
        }

        [Test(Description = "D-009-AC-03: Overdrive gives the next mission two rounds per tap and stays on for the missions after it")]
        public void OverdriveDoublesRoundsFromTheNextMission()
        {
            var boosts = new CombatBoosts(new MemoryFlags());
            boosts.ArmOverdrive();

            var next = boosts.ConsumeForMission(3);
            var later = boosts.ConsumeForMission(3);

            Assert.AreEqual(2, next.RoundsPerTap);
            Assert.AreEqual(3, next.ArmorHits);
            Assert.AreEqual(2, later.RoundsPerTap, "Overdrive lasts until the tank is lost (owner decision 2026-10-31)");
        }

        [Test(Description = "D-009-AC-03: with no boost bought a mission starts with base armor and one round per tap")]
        public void NoBoostMeansBaseLoadout()
        {
            var loadout = new CombatBoosts(new MemoryFlags()).ConsumeForMission(5);

            Assert.AreEqual(5, loadout.ArmorHits);
            Assert.AreEqual(1, loadout.RoundsPerTap);
        }
    }
}
