using System.Collections.Generic;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Economy.Tests
{
    public sealed class PurchaseFlowTests
    {
        // Persists a copy, like PlayerPrefs, so a reload sees only what was saved.
        private sealed class CopyingWalletStore : IWalletStore
        {
            private WalletState saved;

            public WalletState Load() => saved == null ? null : Copy(saved);

            public void Save(WalletState state) => saved = Copy(state);

            private static WalletState Copy(WalletState state) => new WalletState
            {
                balance = state.balance,
                starterGranted = state.starterGranted,
                creditedTransactions = new List<string>(state.creditedTransactions)
            };
        }

        private sealed class FakeGateway : IPurchaseGateway
        {
            public bool Available = true;
            public readonly List<string> Started = new List<string>();

            public bool TryStartPurchase(string productId)
            {
                Started.Add(productId);
                return Available;
            }
        }

        private static GemPack SmallPack => GemCatalog.Packs[0];

        private static GemWallet WalletWith(IWalletStore store, int gems)
        {
            var wallet = new GemWallet(store);
            if (gems > 0)
                wallet.CreditReward(gems);
            return wallet;
        }

        [Test(Description = "D-010-AC-02: a short balance buys the covering pack and applies the boost when it is delivered, with no second tap")]
        public void ShortBalanceAppliesBoostOnDelivery()
        {
            var wallet = WalletWith(new CopyingWalletStore(), 10);
            var gateway = new FakeGateway();
            var flow = new PurchaseFlow(wallet, gateway);
            var applied = 0;
            var statuses = new List<string>();

            flow.SpendOrBuy(60, () => applied++, statuses.Add);

            Assert.AreEqual(new[] { SmallPack.ProductId }, gateway.Started);
            Assert.AreEqual(0, applied, "nothing applies before the store delivers");
            Assert.IsTrue(flow.InProgress);

            var outcome = flow.OrderDelivered(SmallPack.ProductId, "txn-1", out var result);

            Assert.AreEqual(OrderOutcome.Confirm, outcome);
            Assert.AreEqual(CreditResult.Credited, result);
            Assert.AreEqual(1, applied);
            Assert.AreEqual(10 + SmallPack.Gems - 60, wallet.Balance);
            Assert.IsFalse(flow.InProgress);
            CollectionAssert.IsEmpty(statuses);
        }

        [Test(Description = "D-010-AC-02: an affordable cost is spent and applied immediately without opening the store")]
        public void AffordableCostAppliesImmediately()
        {
            var wallet = WalletWith(new CopyingWalletStore(), 100);
            var gateway = new FakeGateway();
            var applied = 0;

            new PurchaseFlow(wallet, gateway).SpendOrBuy(40, () => applied++, _ => Assert.Fail("no status expected"));

            Assert.AreEqual(1, applied);
            Assert.AreEqual(60, wallet.Balance);
            CollectionAssert.IsEmpty(gateway.Started);
        }

        [Test(Description = "D-011-AC-03: a purchase interrupted before confirmation credits its gems exactly once on the next launch")]
        public void InterruptedPurchaseCreditsOnceOnNextLaunch()
        {
            var store = new CopyingWalletStore();
            var firstRun = new PurchaseFlow(WalletWith(store, 0), new FakeGateway());
            firstRun.BuyPack(SmallPack, () => { }, _ => { });
            // The app dies here: the store charged the player but never delivered or confirmed the order.

            var relaunchedWallet = new GemWallet(store);
            var relaunched = new PurchaseFlow(relaunchedWallet, new FakeGateway());

            var replay = relaunched.OrderDelivered(SmallPack.ProductId, "txn-interrupted", out var first);
            var redelivery = relaunched.OrderDelivered(SmallPack.ProductId, "txn-interrupted", out var second);
            var afterAnotherLaunch = new PurchaseFlow(new GemWallet(store), new FakeGateway())
                .OrderDelivered(SmallPack.ProductId, "txn-interrupted", out var third);

            Assert.AreEqual(CreditResult.Credited, first);
            Assert.AreEqual(CreditResult.Duplicate, second);
            Assert.AreEqual(CreditResult.Duplicate, third);
            Assert.AreEqual(OrderOutcome.Confirm, replay);
            Assert.AreEqual(OrderOutcome.Confirm, redelivery, "a duplicate is confirmed so the store stops redelivering");
            Assert.AreEqual(OrderOutcome.Confirm, afterAnotherLaunch);
            Assert.AreEqual(SmallPack.Gems, new GemWallet(store).Balance);
        }

        [Test(Description = "D-011-AC-04: cancelled, failed and awaiting-approval purchases show a plain status and leave the balance unchanged")]
        public void UnsuccessfulPurchasesReportStatusWithoutChangingBalance()
        {
            var expectations = new (System.Action<PurchaseFlow> outcome, string status)[]
            {
                (flow => flow.PurchaseFailed(true), "PURCHASE CANCELLED"),
                (flow => flow.PurchaseFailed(false), "PURCHASE FAILED // TRY AGAIN"),
                (flow => flow.PurchaseDeferred(), "WAITING FOR APPROVAL // GEMS ARRIVE WHEN APPROVED"),
            };
            foreach (var (outcome, expected) in expectations)
            {
                var wallet = WalletWith(new CopyingWalletStore(), 10);
                var flow = new PurchaseFlow(wallet, new FakeGateway());
                var statuses = new List<string>();
                var applied = 0;

                flow.SpendOrBuy(60, () => applied++, statuses.Add);
                outcome(flow);

                Assert.AreEqual(new[] { expected }, statuses);
                Assert.AreEqual(10, wallet.Balance);
                Assert.AreEqual(0, applied);
                Assert.IsFalse(flow.InProgress, "the player can try again");
            }
        }

        [Test(Description = "D-011-AC-04: an unavailable store says so and leaves the balance unchanged")]
        public void UnavailableStoreReportsStatus()
        {
            var wallet = WalletWith(new CopyingWalletStore(), 10);
            var statuses = new List<string>();

            new PurchaseFlow(wallet, new FakeGateway { Available = false }).SpendOrBuy(60, () => Assert.Fail("nothing applies"), statuses.Add);

            Assert.AreEqual(new[] { "STORE UNAVAILABLE // TRY AGAIN SOON" }, statuses);
            Assert.AreEqual(10, wallet.Balance);
        }

        [Test(Description = "D-012-AC-02: a completed pack purchase raises the wallet's balance change with the new balance")]
        public void CompletedPurchaseRaisesBalanceChange()
        {
            var wallet = WalletWith(new CopyingWalletStore(), 25);
            var flow = new PurchaseFlow(wallet, new FakeGateway());
            var shown = new List<int>();
            var credited = 0;
            wallet.BalanceChanged += shown.Add;

            flow.BuyPack(SmallPack, () => credited++, _ => Assert.Fail("no status expected"));
            flow.OrderDelivered(SmallPack.ProductId, "txn-shop", out _);

            Assert.AreEqual(new[] { 25 + SmallPack.Gems }, shown);
            Assert.AreEqual(1, credited);
        }
    }
}
