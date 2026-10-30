using System;

namespace TreadShred.Economy
{
    /// <summary>The platform store as the purchase flow sees it.</summary>
    public interface IPurchaseGateway
    {
        /// <summary>Opens the native purchase sheet; false when the product is unavailable.</summary>
        bool TryStartPurchase(string productId);
    }

    /// <summary>What the store bridge must do with an order the store delivered.</summary>
    public enum OrderOutcome
    {
        /// <summary>Gems are in the wallet (now or earlier): confirm so the store stops redelivering.</summary>
        Confirm,
        /// <summary>Unknown product: leave unconfirmed so the store refunds it.</summary>
        LeaveUnconfirmed
    }

    public static class PurchaseMessages
    {
        public const string InProgress = "PURCHASE IN PROGRESS";
        public const string NotEnoughGems = "NOT ENOUGH GEMS";
        public const string Unavailable = "STORE UNAVAILABLE. TRY AGAIN SOON";
        public const string Cancelled = "PURCHASE CANCELLED";
        public const string Failed = "PURCHASE FAILED. TRY AGAIN";
        public const string AwaitingApproval = "WAITING FOR APPROVAL. GEMS ARRIVE WHEN APPROVED";
    }

    /// <summary>
    /// The gem purchase decisions behind every buy button (D-010, D-011):
    /// spend when the balance covers a cost, otherwise buy the smallest
    /// covering pack and spend as soon as the store delivers it. Orders the
    /// store delivers with no purchase waiting (an interrupted purchase
    /// replayed at launch) are still credited exactly once.
    /// </summary>
    public sealed class PurchaseFlow
    {
        private sealed class Pending
        {
            public int SpendCost;
            public Action OnApplied;
            public Action<string> OnStatus;
        }

        private readonly GemWallet wallet;
        private readonly IPurchaseGateway gateway;
        private Pending pending;

        public PurchaseFlow(GemWallet wallet, IPurchaseGateway gateway)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public bool InProgress => pending != null;

        /// <summary>Spends a gem cost, or buys the smallest covering pack and then spends.</summary>
        public void SpendOrBuy(int cost, Action onApplied, Action<string> onStatus)
        {
            if (pending != null)
            {
                onStatus(PurchaseMessages.InProgress);
                return;
            }

            var plan = GemCatalog.PlanFor(cost, wallet.Balance);
            if (plan.CanSpendNow)
            {
                if (wallet.TrySpend(plan.Cost))
                    onApplied();
                else
                    onStatus(PurchaseMessages.NotEnoughGems);
                return;
            }

            Start(plan.PackToBuy, new Pending { SpendCost = cost, OnApplied = onApplied, OnStatus = onStatus });
        }

        /// <summary>Buys a pack outright; onCredited runs once the gems are in the wallet.</summary>
        public void BuyPack(GemPack pack, Action onCredited, Action<string> onStatus)
        {
            if (pending != null)
            {
                onStatus(PurchaseMessages.InProgress);
                return;
            }

            Start(pack, new Pending { OnApplied = onCredited, OnStatus = onStatus });
        }

        /// <summary>The store delivered a paid order; returns whether to confirm it.</summary>
        public OrderOutcome OrderDelivered(string productId, string transactionId, out CreditResult result)
        {
            result = wallet.CreditPurchase(productId, transactionId);
            if (result == CreditResult.UnknownProduct)
                return OrderOutcome.LeaveUnconfirmed;

            if (pending != null)
            {
                var purchase = pending;
                pending = null;
                if (purchase.SpendCost == 0 || wallet.TrySpend(purchase.SpendCost))
                    purchase.OnApplied();
                else
                    purchase.OnStatus(PurchaseMessages.NotEnoughGems);
            }
            return OrderOutcome.Confirm;
        }

        public void PurchaseFailed(bool cancelledByPlayer)
        {
            Finish(cancelledByPlayer ? PurchaseMessages.Cancelled : PurchaseMessages.Failed);
        }

        public void PurchaseDeferred()
        {
            Finish(PurchaseMessages.AwaitingApproval);
        }

        /// <summary>Drops the waiting purchase's callbacks (their scene is gone); the order still credits on delivery.</summary>
        public void Abandon()
        {
            pending = null;
        }

        private void Start(GemPack pack, Pending purchase)
        {
            // Set first: the store may report the outcome before TryStartPurchase returns.
            pending = purchase;
            if (!gateway.TryStartPurchase(pack.ProductId))
            {
                pending = null;
                purchase.OnStatus(PurchaseMessages.Unavailable);
            }
        }

        private void Finish(string status)
        {
            if (pending == null)
                return;

            var onStatus = pending.OnStatus;
            pending = null;
            onStatus(status);
        }
    }
}
