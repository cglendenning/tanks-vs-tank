using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Economy.Tests
{
    public class GemWalletTests
    {
        private sealed class InMemoryWalletStore : IWalletStore
        {
            public WalletState Saved;
            public int SaveCount;

            public WalletState Load() => Saved;

            public void Save(WalletState state)
            {
                Saved = state;
                SaveCount++;
            }
        }

        private static GemPack SmallPack => GemCatalog.Packs[0];

        [Test(Description = "D-008-AC-01: a new wallet holds no gems before the starter grant")]
        public void NewWalletStartsEmpty()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());

            Assert.AreEqual(0, wallet.Balance);
        }

        [Test(Description = "D-008-AC-01: the starter grant is applied once")]
        public void StarterGrantIsAppliedOnlyOnce()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());

            Assert.IsTrue(wallet.GrantStarterIfNeeded());
            Assert.IsFalse(wallet.GrantStarterIfNeeded());
            Assert.AreEqual(GemCatalog.StarterGrant, wallet.Balance);
        }

        [Test(Description = "D-008-AC-01: the starter grant is not repeated after the wallet reloads")]
        public void StarterGrantIsNotRepeatedAfterReload()
        {
            var store = new InMemoryWalletStore();
            new GemWallet(store).GrantStarterIfNeeded();

            var reloaded = new GemWallet(store);

            Assert.IsFalse(reloaded.GrantStarterIfNeeded());
            Assert.AreEqual(GemCatalog.StarterGrant, reloaded.Balance);
        }

        [Test(Description = "D-011-AC-01: a known pack credits its gems")]
        public void CreditingAKnownPackAddsItsGems()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());

            var result = wallet.CreditPurchase(SmallPack.ProductId, "txn-1");

            Assert.AreEqual(CreditResult.Credited, result);
            Assert.AreEqual(SmallPack.Gems, wallet.Balance);
        }

        [Test(Description = "D-011-AC-01: a redelivered transaction is not credited twice")]
        public void RedeliveredTransactionIsNotCreditedTwice()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());
            wallet.CreditPurchase(SmallPack.ProductId, "txn-1");

            var result = wallet.CreditPurchase(SmallPack.ProductId, "txn-1");

            Assert.AreEqual(CreditResult.Duplicate, result);
            Assert.AreEqual(SmallPack.Gems, wallet.Balance);
        }

        [Test(Description = "D-011-AC-01: a redelivered transaction is rejected after reload")]
        public void RedeliveredTransactionIsRejectedAfterReload()
        {
            var store = new InMemoryWalletStore();
            new GemWallet(store).CreditPurchase(SmallPack.ProductId, "txn-1");

            var reloaded = new GemWallet(store);

            Assert.AreEqual(CreditResult.Duplicate, reloaded.CreditPurchase(SmallPack.ProductId, "txn-1"));
            Assert.AreEqual(SmallPack.Gems, reloaded.Balance);
        }

        [Test(Description = "D-011-AC-02: an unknown product is not credited")]
        public void UnknownProductIsRejectedWithoutChangingBalance()
        {
            var store = new InMemoryWalletStore();
            var wallet = new GemWallet(store);

            var result = wallet.CreditPurchase("treadshred.unknown", "txn-1");

            Assert.AreEqual(CreditResult.UnknownProduct, result);
            Assert.AreEqual(0, wallet.Balance);
            Assert.AreEqual(0, store.SaveCount);
        }

        [Test(Description = "D-011-AC-01: a purchase without a transaction ID is still credited")]
        public void PurchaseWithoutTransactionIdIsStillCredited()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());

            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(SmallPack.ProductId, string.Empty));
            Assert.AreEqual(SmallPack.Gems, wallet.Balance);
        }

        [Test(Description = "D-011-AC-01: transaction history is bounded and keeps recent IDs")]
        public void TransactionHistoryIsTrimmedButKeepsRecentIds()
        {
            var store = new InMemoryWalletStore();
            var wallet = new GemWallet(store);
            for (var i = 0; i <= GemWallet.RememberedTransactionLimit; i++)
                wallet.CreditPurchase(SmallPack.ProductId, "txn-" + i);

            Assert.AreEqual(GemWallet.RememberedTransactionLimit, store.Saved.creditedTransactions.Count);
            Assert.IsFalse(store.Saved.creditedTransactions.Contains("txn-0"));
            Assert.AreEqual(
                CreditResult.Duplicate,
                wallet.CreditPurchase(SmallPack.ProductId, "txn-" + GemWallet.RememberedTransactionLimit));
        }

        [Test(Description = "D-010-AC-01: spending within the balance deducts gems")]
        public void SpendingWithinBalanceDeducts()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());
            wallet.GrantStarterIfNeeded();

            Assert.IsTrue(wallet.TrySpend(40));
            Assert.AreEqual(GemCatalog.StarterGrant - 40, wallet.Balance);
        }

        [Test(Description = "D-010-AC-01: spending beyond the balance fails and keeps it")]
        public void SpendingBeyondBalanceFailsAndKeepsBalance()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());
            wallet.GrantStarterIfNeeded();

            Assert.IsFalse(wallet.TrySpend(GemCatalog.StarterGrant + 1));
            Assert.AreEqual(GemCatalog.StarterGrant, wallet.Balance);
        }

        [Test(Description = "D-010-AC-01: a non-positive spend is rejected")]
        public void SpendingANonPositiveAmountThrows()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(0));
        }

        [Test(Description = "D-012: the wallet reports every balance change so screens can update")]
        public void BalanceChangedReportsEveryMutation()
        {
            var wallet = new GemWallet(new InMemoryWalletStore());
            var reported = new List<int>();
            wallet.BalanceChanged += reported.Add;

            wallet.GrantStarterIfNeeded();
            wallet.CreditPurchase(SmallPack.ProductId, "txn-1");
            wallet.TrySpend(40);

            CollectionAssert.AreEqual(
                new[] { GemCatalog.StarterGrant, GemCatalog.StarterGrant + SmallPack.Gems, GemCatalog.StarterGrant + SmallPack.Gems - 40 },
                reported);
        }

        [Test(Description = "D-011-AC-01: saved state without a transaction list loads safely")]
        public void StateWithMissingTransactionListLoadsSafely()
        {
            var store = new InMemoryWalletStore { Saved = new WalletState { balance = 7, creditedTransactions = null } };

            var wallet = new GemWallet(store);

            Assert.AreEqual(7, wallet.Balance);
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(SmallPack.ProductId, "txn-1"));
        }
    }
}
