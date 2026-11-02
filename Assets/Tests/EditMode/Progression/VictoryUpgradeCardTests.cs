using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class VictoryUpgradeCardTests
    {
        private sealed class MemoryProgressionStore : IProgressionStore
        {
            private ProgressionState saved;
            public ProgressionState Load() => saved;
            public void Save(ProgressionState state) => saved = state;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            private WalletState saved;
            public WalletState Load() => saved;
            public void Save(WalletState state) => saved = state;
        }

        private static PlayerProgression WithLevels(params int[] levels)
        {
            var progression = new PlayerProgression(new MemoryProgressionStore(), new GemWallet(new MemoryWalletStore()));
            progression.Replace(new ProgressionState { upgradeLevels = levels });
            return progression;
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-009-AC-03: with Overdrive carried over, the victory card offers the next level that costs fewest coins, armor first on a tie, and nothing once every track is maxed")]
        public void CardOffersTheCheapestNextUpgrade()
        {
            var max = ProgressionRules.LevelsPerTrack;

            Assert.AreEqual(UpgradeTrack.Armor, WithLevels(0, 0, 0).CheapestNextUpgrade(), "a tie goes to armor");
            Assert.AreEqual(UpgradeTrack.Firepower, WithLevels(2, 0, 1).CheapestNextUpgrade());
            Assert.AreEqual(UpgradeTrack.Armor, WithLevels(1, 2, 1).CheapestNextUpgrade(), "armor before speed at the same price");
            Assert.AreEqual(UpgradeTrack.Speed, WithLevels(max, max, 3).CheapestNextUpgrade(), "maxed tracks are skipped");
            Assert.IsNull(WithLevels(max, max, max).CheapestNextUpgrade());
        }

        [Test(Description = "D-009-AC-03: the Overdrive status says it is on and why, without repeating the effect the card already states")]
        public void StatusLineDoesNotRepeatTheEffect()
        {
            Assert.AreEqual("OVERDRIVE ACTIVE  |  ON FROM AN EARLIER MISSION", OfferLabels.OverdriveStatus(0));
            Assert.AreEqual("OVERDRIVE ACTIVE  |  60 GEMS SPENT", OfferLabels.OverdriveStatus(60));
            StringAssert.DoesNotContain(OfferLabels.OverdriveEffect, OfferLabels.OverdriveStatus(0));
        }

        [Test(Description = "D-009-AC-03: while Overdrive runs the card never shows a button that sells nothing, and with Overdrive carried over it offers an upgrade")]
        public void NoButtonSellsNothing()
        {
            var results = Script("ManagerScore/ManagerScore.cs");

            StringAssert.Contains("rewardButton.gameObject.SetActive(false);", results);
            StringAssert.Contains("? TreadShredProgress.Ensure().Progression.CheapestNextUpgrade()", results);
            StringAssert.Contains("BuildUpgradeOfferPanel(track.Value);", results);
        }
    }
}
