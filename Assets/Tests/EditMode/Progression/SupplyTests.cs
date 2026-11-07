using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class SupplyTests
    {
        private sealed class MemoryProgression : IProgressionStore
        {
            public ProgressionState Saved;
            public ProgressionState Load() => Saved;
            public void Save(ProgressionState state) => Saved = state;
        }

        private sealed class MemoryWallet : IWalletStore
        {
            public WalletState Saved = new WalletState();
            public WalletState Load() => Saved;
            public void Save(WalletState state) => Saved = state;
        }

        private static (PlayerProgression progression, GemWallet wallet) Player(int gems, ProgressionState state = null)
        {
            var wallet = new GemWallet(new MemoryWallet());
            wallet.Replace(new WalletState { balance = gems, starterGranted = true });
            return (new PlayerProgression(new MemoryProgression { Saved = state }, wallet), wallet);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-045-AC-02: every 5 missions won one unit is used up, from the next track in rotation that holds any, so armor x2, firepower x1 and Overdrive x1 last 20 missions and wind down together")]
        public void SupplyIsUsedUpInRotation()
        {
            var (progression, _) = Player(0, new ProgressionState { supply = new[] { 2, 1, 0, 1, 0, 0, 0 } });
            Assert.AreEqual((20, 10, 15), (progression.SupplyMissionsLeft(SupplyTrack.Armor),
                progression.SupplyMissionsLeft(SupplyTrack.Firepower), progression.SupplyMissionsLeft(SupplyTrack.Overdrive)));
            Assert.AreEqual((SupplyTrack.Armor, 5), (progression.NextSupplyUse.Value, progression.MissionsToNextSupplyUse));

            var used = new System.Collections.Generic.List<string>();
            for (var mission = 1; mission <= 20; mission++)
            {
                var before = new[] { progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Firepower), progression.Supply(SupplyTrack.Overdrive) };
                progression.SupplyMissionWon();
                var after = new[] { progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Firepower), progression.Supply(SupplyTrack.Overdrive) };
                for (var track = 0; track < 3; track++)
                    if (after[track] < before[track])
                        used.Add(mission + ":" + track);
            }
            CollectionAssert.AreEqual(new[] { "5:0", "10:1", "15:2", "20:0" }, used, "armor, firepower, Overdrive, then armor again");
            Assert.IsNull(progression.NextSupplyUse, "all used up");
        }

        [Test(Description = "D-045-AC-02: with no supply held the clock stands still, and only missions won count")]
        public void TheClockRunsOnlyWhileSupplyIsHeld()
        {
            var (progression, _) = Player(0);
            for (var mission = 0; mission < 7; mission++)
                progression.SupplyMissionWon();
            progression.AddSupply(SupplyTrack.Seeker);
            Assert.AreEqual(5, progression.SupplyMissionsLeft(SupplyTrack.Seeker), "a fresh unit lasts five missions won");
            StringAssert.Contains("if (won)\n            Progression.SupplyMissionWon();", Script("Game/TreadShredProgress.cs"), "missions lost use nothing");
        }

        [Test(Description = "D-045-AC-01: armor, and firepower or speed at level 10, sell units of 100 gems with no cap: +2 hits, +20% damage, +10% speed")]
        public void PurchasesBuySupply()
        {
            Assert.AreEqual((100, 2, 0.2f, 0.1f, 5), (SupplyRules.UnitGems, SupplyRules.ArmorHitsPerUnit, SupplyRules.DamagePerUnit,
                SupplyRules.SpeedPerUnit, SupplyRules.MissionsPerUse));
            var (progression, wallet) = Player(250, new ProgressionState { upgradeLevels = new[] { 0, 10, 3 } });
            Assert.IsTrue(progression.BuysSupply(UpgradeTrack.Armor) && progression.BuysSupply(UpgradeTrack.Firepower));
            Assert.IsFalse(progression.BuysSupply(UpgradeTrack.Speed), "speed below level 10 buys a coin level");
            Assert.AreEqual(100, progression.Quote(UpgradeTrack.Armor).GemsNeeded);

            Assert.AreEqual(UpgradeResult.Upgraded, progression.TryUpgrade(UpgradeTrack.Armor, out var armor));
            Assert.AreEqual(UpgradeResult.Upgraded, progression.TryUpgrade(UpgradeTrack.Firepower));
            Assert.AreEqual((1, 1, 0, 10, 50), (progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Firepower),
                progression.Level(UpgradeTrack.Armor), progression.Level(UpgradeTrack.Firepower), wallet.Balance), "units, not levels");
            Assert.AreEqual(UpgradeResult.NotEnoughGems, progression.TryUpgrade(UpgradeTrack.Armor), "no cap, only the price");

            Assert.IsTrue(armor.Supply);
            Assert.IsTrue(progression.Refund(armor), "undo returns the unit");
            Assert.AreEqual((0, 150), (progression.Supply(SupplyTrack.Armor), wallet.Balance), "and its gems");

            var effects = Script("Game/TreadShredProgress.cs");
            StringAssert.Contains("+ SupplyRules.ArmorHitsPerUnit * Progression.Supply(SupplyTrack.Armor);", effects);
            StringAssert.Contains("+ SupplyRules.DamagePerUnit * Progression.Supply(SupplyTrack.Firepower);", effects);
            StringAssert.Contains("+ SupplyRules.SpeedPerUnit * Progression.Supply(SupplyTrack.Speed);", effects);
        }

        [Test(Description = "D-045-AC-03: the screens show each track's supply and the missions it lasts, never MAXED, and the confirmation and undo name the supply")]
        public void ScreensNameTheSupply()
        {
            Assert.AreEqual("SUPPLY 2: 9 MISSIONS LEFT", ProgressionLabels.SupplyLine(2, 9));
            Assert.AreEqual("NO SUPPLY", ProgressionLabels.SupplyLine(0, 0));
            Assert.AreEqual(("+2 HITS", "+20% DAMAGE", "+10% SPEED"), (ProgressionLabels.SupplyUnit(UpgradeTrack.Armor),
                ProgressionLabels.SupplyUnit(UpgradeTrack.Firepower), ProgressionLabels.SupplyUnit(UpgradeTrack.Speed)));
            Assert.AreEqual("7 HITS  >  +2 HITS", ProgressionLabels.SupplyPreview(UpgradeTrack.Armor, 0, 2));
            var unit = new UpgradeQuote(SupplyRules.UnitGems * ProgressionRules.CoinsPerGem, SupplyRules.UnitGems * ProgressionRules.CoinsPerGem);
            Assert.AreEqual("+2 HITS SUPPLY FOR 100 GEMS", ProgressionLabels.UpgradeConfirmation(UpgradeTrack.Armor, 0, unit, 500).Detail);
            Assert.AreEqual("ARMOR SUPPLY UNDONE. 100 GEMS RETURNED",
                ProgressionLabels.Undone(new UpgradeReceipt(UpgradeTrack.Armor, 0, 0, 100, unit, supply: true)));
            StringAssert.Contains("ButtonLabel(track, false)", Script("Game/TreadShredUpgradePanel.cs"), "the upgrade screen never shows MAXED");
            StringAssert.Contains("PricedLabel(track, false)", Script("ManagerScore/ManagerScore.cs"), "nor the victory card");
        }

        [Test(Description = "D-045-AC-05: levels already owned stay; a 1.9.5 save's running rentals become weapon units, one per 5 missions begun")]
        public void OldSavesKeepWhatTheyPaidFor()
        {
            var (progression, _) = Player(0, new ProgressionState { upgradeLevels = new[] { 4, 2, 1 }, rentals = new[] { 0, 5, 7, 0 } });
            Assert.AreEqual((4, 2, 1), (progression.Level(UpgradeTrack.Armor), progression.Level(UpgradeTrack.Firepower),
                progression.Level(UpgradeTrack.Speed)));
            Assert.AreEqual((1, 2, 0), (progression.Supply(SupplyTrack.ArcShot), progression.Supply(SupplyTrack.TripleShot),
                progression.Supply(SupplyTrack.Seeker)));
        }

        [Test(Description = "D-045-AC-06: a restored save carries the supply and its clock, and RESET GAME clears them with the rest")]
        public void RestoreCarriesTheSupply()
        {
            var (progression, _) = Player(0);
            progression.Replace(new ProgressionState { supply = new[] { 1, 0, 0, 2, 0, 0, 0 }, supplyWins = 3, supplyNext = 3 });
            Assert.AreEqual((1, 2), (progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Overdrive)));
            Assert.AreEqual((SupplyTrack.Overdrive, 2), (progression.NextSupplyUse.Value, progression.MissionsToNextSupplyUse));

            // RESET GAME (D-044) replaces the progression with a new one.
            StringAssert.Contains("progress.Progression.Replace(new ProgressionState());", Script("Game/TreadShredGameReset.cs"));
            progression.Replace(new ProgressionState());
            Assert.AreEqual((0, 0), (progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Overdrive)));
            Assert.IsNull(progression.NextSupplyUse);
        }
    }
}
