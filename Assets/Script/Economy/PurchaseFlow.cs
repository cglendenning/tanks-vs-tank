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
        public const string Opening = "OPENING THE STORE";
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
            public string ProductId;
            public int SpendCost;
            public Action OnApplied;
            public Action<string> OnStatus;
        }

        private readonly GemWallet wallet;
        private readonly IPurchaseGateway gateway;
        private readonly Func<bool> starterPackAvailable;
        private readonly Func<DateTime> utcNow;
        private Pending pending;

        /// <param name="starterPackAvailable">Whether the store reports the Starter Pack's product (D-037-AC-02).</param>
        /// <param name="utcNow">The clock the Starter Pack's window runs on.</param>
        public PurchaseFlow(GemWallet wallet, IPurchaseGateway gateway, Func<bool> starterPackAvailable = null,
            Func<DateTime> utcNow = null)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            this.starterPackAvailable = starterPackAvailable ?? (() => false);
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public bool InProgress => pending != null;

        /// <summary>D-037-AC-02: on sale until bought, inside its window, and only while the store offers its product.</summary>
        public bool StarterPackOnSale => StarterPackTimeLeft > TimeSpan.Zero;

        /// <summary>D-037-AC-07: the countdown every Starter Pack offer shows; zero when it is not on sale.</summary>
        public TimeSpan StarterPackTimeLeft => starterPackAvailable() ? wallet.StarterPackTimeLeft(utcNow()) : TimeSpan.Zero;

        /// <summary>What a gem cost spends now, or the pack one tap buys for it (D-010, D-037-AC-04).</summary>
        public OfferPlan PlanFor(int cost) => GemCatalog.PlanFor(cost, wallet.Balance, StarterPackOnSale);

        /// <summary>The pack one tap buys to cover a gem shortfall (D-036, D-037-AC-04).</summary>
        public GemPack PackCovering(int shortfall) => GemCatalog.SmallestPackCovering(shortfall, StarterPackOnSale);

        /// <summary>Spends a gem cost, or buys the smallest covering pack and then spends.</summary>
        public void SpendOrBuy(int cost, Action onApplied, Action<string> onStatus)
        {
            if (pending != null)
            {
                onStatus(PurchaseMessages.InProgress);
                return;
            }

            var plan = PlanFor(cost);
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

            // Only a fresh delivery of the pack this purchase opened completes it. A replayed order, or one for
            // another pack, is credited above and the purchase keeps waiting for its own outcome.
            if (pending != null && result == CreditResult.Credited && productId == pending.ProductId)
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

        /// <summary>
        /// Runs a request that may open the store sheet and, when it did, says so at once: the sheet can take
        /// seconds to appear, and a screen that does not react invites the player to leave it.
        /// </summary>
        public void Request(Action request, Action<string> onStatus)
        {
            var busy = InProgress;
            request();
            if (!busy && InProgress)
                onStatus(PurchaseMessages.Opening);
        }

        /// <summary>
        /// The screen that asked for the waiting purchase closed: drop its callbacks, so a later delivery credits
        /// the gems and spends or applies nothing. Returns false when the purchase belongs to another screen.
        /// </summary>
        public bool Release(Action<string> onStatus)
        {
            if (pending == null || pending.OnStatus != onStatus)
                return false;
            pending = null;
            return true;
        }

        /// <summary>Drops the waiting purchase's callbacks (their scene is gone); the order still credits on delivery.</summary>
        public void Abandon()
        {
            pending = null;
        }

        private void Start(GemPack pack, Pending purchase)
        {
            // Set first: the store may report the outcome before TryStartPurchase returns.
            purchase.ProductId = pack.ProductId;
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
