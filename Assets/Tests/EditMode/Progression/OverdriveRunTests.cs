using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Overdrive lasts from the next mission until the player's tank is lost (owner decision 2026-10-31).
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

        [Test(Description = "D-009-AC-03: Overdrive gives two rounds per tap in every mission until the player's tank is lost")]
        public void OverdriveLastsUntilTheTankIsLost()
        {
            var boosts = new CombatBoosts(new MemoryFlags());
            boosts.ArmOverdrive();
            for (var mission = 0; mission < 3; mission++)
                Assert.AreEqual(2, boosts.ConsumeForMission(3).RoundsPerTap, "mission " + (mission + 1));

            boosts.TankLost();

            Assert.IsFalse(boosts.OverdriveActive);
            Assert.AreEqual(1, boosts.ConsumeForMission(3).RoundsPerTap);
        }

        [Test(Description = "D-009-AC-03: an Armored Redeploy bought after the tank is lost keeps Overdrive running")]
        public void RedeployKeepsOverdrive()
        {
            var boosts = new CombatBoosts(new MemoryFlags());
            boosts.ArmOverdrive();
            boosts.ConsumeForMission(3);
            boosts.TankLost();

            boosts.ArmRedeploy();

            Assert.IsTrue(boosts.OverdriveActive);
            var replay = boosts.ConsumeForMission(3);
            Assert.AreEqual((4, 2), (replay.ArmorHits, replay.RoundsPerTap));
        }

        [Test(Description = "D-009-AC-03: once a mission starts without a redeploy after the loss, that Overdrive is gone for good")]
        public void OverdriveEndsWithoutARedeploy()
        {
            var boosts = new CombatBoosts(new MemoryFlags());
            boosts.ArmOverdrive();
            boosts.TankLost();
            Assert.AreEqual(1, boosts.ConsumeForMission(3).RoundsPerTap, "the free retry starts without Overdrive");

            boosts.ArmRedeploy();

            Assert.IsFalse(boosts.OverdriveActive, "a later redeploy does not bring it back");
        }

        [Test(Description = "D-009-AC-03: the offer, the active state and the sales screen all say Overdrive lasts until the tank goes down")]
        public void CopySaysHowLongOverdriveLasts()
        {
            Assert.AreEqual("2 SHOTS PER TAP UNTIL YOUR TANK GOES DOWN", OfferLabels.OverdriveEffect);
            Assert.AreEqual("2 SHOTS PER TAP UNTIL YOUR TANK GOES DOWN. 60 GEMS SPENT", OfferLabels.OverdriveActiveDetail(60));
            Assert.AreEqual(OfferLabels.OverdriveEffect, OfferLabels.OverdriveActiveDetail(0), "carried from an earlier mission");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.Overdrive).Points, "UNTIL YOUR TANK GOES DOWN");
            StringAssert.Contains("OfferLabels.OverdriveEffect,", Script("ManagerScore/ManagerScore.cs"));
        }

        [Test(Description = "D-009-AC-03: losing the tank ends Overdrive, the victory screen shows it active, and the mission HUD shows it")]
        public void ScreensFollowTheOverdriveRun()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("TreadShredBoosts.Current.TankLost();", results);
            StringAssert.Contains("if (offer == GemOffer.Overdrive && TreadShredBoosts.Current.OverdriveActive)", results);
            StringAssert.Contains("TreadShredOverdriveBadge.Show(", Script("Player/PlayerMoverment.cs"));
            StringAssert.Contains("TreadShredOverdriveBadge.SetVisible(false);", Script("ManagerScore/ManagerButton.cs"));
        }
    }
}
