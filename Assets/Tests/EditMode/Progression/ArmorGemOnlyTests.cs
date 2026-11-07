using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    public sealed class ArmorGemOnlyTests
    {
        private sealed class MemoryStore : IProgressionStore
        {
            private ProgressionState state = new ProgressionState();
            public ProgressionState Load() => state;
            public void Save(ProgressionState saved) => state = saved;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            private WalletState state = new WalletState();
            public WalletState Load() => state;
            public void Save(WalletState saved) => state = saved;
        }

        [Test(Description = "D-017-AC-03: armor is bought with gems only, a unit of supply at 100 gems (D-045) that coins never buy; firepower and speed stay on coins; levels already owned stay")]
        public void ArmorIsBoughtWithGemsOnly()
        {
            var wallet = new GemWallet(new MemoryWalletStore());
            var progression = new PlayerProgression(new MemoryStore(), wallet);
            progression.Replace(new ProgressionState { coins = 100000, upgradeLevels = new[] { 2, 0, 0 } });
            Assert.AreEqual(2, progression.Level(UpgradeTrack.Armor), "levels already owned stay");
            var armor = progression.Quote(UpgradeTrack.Armor);
            Assert.AreEqual((2000, 100), (armor.CostCoins, armor.GemsNeeded), "a unit of armor supply: 100 gems, kept in the quote at the coin rate");
            Assert.IsFalse(armor.AffordableWithCoins, "100,000 coins still do not buy armor");
            Assert.AreEqual(UpgradeResult.NotEnoughGems, progression.TryUpgrade(UpgradeTrack.Armor));
            wallet.CreditReward(100);
            Assert.AreEqual(UpgradeResult.Upgraded, progression.TryUpgrade(UpgradeTrack.Armor, out var receipt));
            Assert.AreEqual((0, 100, 100000, 0), (receipt.CoinsPaid, receipt.GemsSpent, progression.Coins, wallet.Balance));
            Assert.AreEqual((2, 1), (progression.Level(UpgradeTrack.Armor), progression.Supply(SupplyTrack.Armor)), "a unit, not a level");
            Assert.IsTrue(progression.Refund(receipt), "UNDO gives the gems back");
            Assert.AreEqual(100, wallet.Balance);
            Assert.IsTrue(progression.Quote(UpgradeTrack.Firepower).AffordableWithCoins, "firepower stays on coins");
            Assert.IsTrue(ProgressionRules.GemOnly(UpgradeTrack.Armor));
            Assert.IsFalse(ProgressionRules.GemOnly(UpgradeTrack.Speed));
        }

        [Test(Description = "D-036-AC-01: the armor button states gems or the pack price, never coins")]
        public void ArmorButtonNeverShowsCoins()
        {
            var progression = new PlayerProgression(new MemoryStore(), new GemWallet(new MemoryWalletStore()));
            progression.Replace(new ProgressionState { coins = 100000 });
            var quote = progression.Quote(UpgradeTrack.Armor);
            Assert.AreEqual(SupplyRules.UnitGems, quote.GemsNeeded, "every armor purchase is a 100-gem unit (D-045)");
            Assert.AreEqual(quote.CostCoins, quote.CoinShortfall, "the whole price falls to gems, so the button never quotes coins");
        }

        [Test(Description = "D-018-AC-04: the COINS AND GEMS note says coins buy firepower and speed and gems buy armor, with the gem-to-coin rate")]
        public void NoteNamesWhatEachCurrencyBuys()
        {
            var note = string.Join(" ", ProgressionLabels.CurrencyNote());
            StringAssert.Contains("THEY BUY FIREPOWER AND SPEED", note);
            StringAssert.Contains("THEY BUY ARMOR", note);
            StringAssert.Contains("1 GEM = " + ProgressionRules.CoinsPerGem + " COINS", note);
        }
    }
}
