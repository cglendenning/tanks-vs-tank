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
            var boosts = new CombatBoosts(new MemoryFlags(), () => false);
            boosts.ArmRedeploy();

            var reloaded = boosts.ConsumeForMission(3);
            var after = boosts.ConsumeForMission(3);

            Assert.AreEqual(4, reloaded.ArmorHits);
            Assert.AreEqual(1, reloaded.RoundsPerTap);
            Assert.AreEqual(3, after.ArmorHits, "the extra hit is used up by the reloaded mission");
        }

        [Test(Description = "D-045-AC-04: Overdrive gives two rounds per tap in every mission while Overdrive supply is held, and none once it is used up")]
        public void OverdriveFollowsSupply()
        {
            var held = true;
            var boosts = new CombatBoosts(new MemoryFlags(), () => held);

            Assert.AreEqual(2, boosts.ConsumeForMission(3).RoundsPerTap);
            Assert.AreEqual(2, boosts.ConsumeForMission(3).RoundsPerTap, "every mission while it is held");
            held = false;
            Assert.AreEqual(1, boosts.ConsumeForMission(3).RoundsPerTap, "used up");
        }

        [Test(Description = "D-009-AC-03: with no boost bought a mission starts with base armor and one round per tap")]
        public void NoBoostMeansBaseLoadout()
        {
            var loadout = new CombatBoosts(new MemoryFlags(), () => false).ConsumeForMission(5);

            Assert.AreEqual(5, loadout.ArmorHits);
            Assert.AreEqual(1, loadout.RoundsPerTap);
        }
    }
}
