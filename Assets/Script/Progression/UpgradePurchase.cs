using System;
using System.Collections.Generic;
using System.Linq;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>
    /// One tap on an upgrade (D-017, D-018, D-010): buys the next level with coins, covers a coin shortfall with
    /// gems, and when gems are short too buys the smallest covering pack and upgrades once it is delivered. The
    /// screen calls Release when it closes, so a purchase still arriving credits its gems and upgrades nothing (D-011).
    /// Until then, Undo gives back the upgrades made on the screen, latest first (D-017-AC-06).
    /// </summary>
    /// <summary>D-036 (P-8): what a tap on an upgrade does.</summary>
    public enum UpgradeCheckout
    {
        /// <summary>Coins cover it: it applies on the tap, and UNDO UPGRADE reverses it (D-036-AC-03).</summary>
        ApplyNow,
        /// <summary>The gems the player holds cover the rest: confirmed once (D-036-AC-03).</summary>
        ConfirmGems,
        /// <summary>Gems fall short: the store's purchase sheet is the only confirmation (D-036-AC-02).</summary>
        BuyGems
    }

    public sealed class UpgradePurchase
    {
        public static UpgradeCheckout Checkout(UpgradeQuote quote, int gemBalance) =>
            quote.AffordableWithCoins ? UpgradeCheckout.ApplyNow
            : gemBalance >= quote.GemsNeeded ? UpgradeCheckout.ConfirmGems
            : UpgradeCheckout.BuyGems;

        private readonly PlayerProgression progression;
        private readonly GemWallet wallet;
        private readonly PurchaseFlow purchases;
        private readonly Action<UpgradeTrack, UpgradeQuote> onUpgraded;
        private readonly Action<string> onStatus;
        private readonly Stack<UpgradeReceipt> receipts = new Stack<UpgradeReceipt>();

        public UpgradePurchase(PlayerProgression progression, GemWallet wallet, PurchaseFlow purchases,
            Action<UpgradeTrack, UpgradeQuote> onUpgraded, Action<string> onStatus)
        {
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            this.onUpgraded = onUpgraded ?? throw new ArgumentNullException(nameof(onUpgraded));
            this.onStatus = onStatus ?? throw new ArgumentNullException(nameof(onStatus));
        }

        public void Buy(UpgradeTrack track)
        {
            if (TryUpgrade(track) != UpgradeResult.NotEnoughGems)
                return;
            var pack = GemCatalog.SmallestPackCovering(progression.Quote(track).GemsNeeded - wallet.Balance);
            purchases.Request(() => purchases.BuyPack(pack, () => AfterPack(track), onStatus), onStatus);
        }

        public bool CanUndo => receipts.Count > 0;

        /// <summary>The most recent upgrade still standing on this screen, if any.</summary>
        public UpgradeReceipt? LastUpgrade => receipts.Count > 0 ? receipts.Peek() : (UpgradeReceipt?)null;

        /// <summary>The upgrades made on this screen and not undone, oldest first: final once the screen closes.</summary>
        public IReadOnlyList<UpgradeReceipt> Kept => receipts.Reverse().ToList();

        /// <summary>Gives back the most recent upgrade; false when there is none or its track has changed since.</summary>
        public bool Undo()
        {
            if (receipts.Count == 0)
                return false;
            if (progression.Refund(receipts.Pop()))
                return true;
            // Progress changed under the screen (a cloud restore): nothing older can be given back safely.
            receipts.Clear();
            return false;
        }

        /// <summary>
        /// The screen closed: its upgrades are final, and a purchase it started still credits its gems but
        /// upgrades nothing.
        /// </summary>
        public void Release()
        {
            receipts.Clear();
            purchases.Release(onStatus);
        }

        private void AfterPack(UpgradeTrack track)
        {
            // One upgrade with the delivered gems; another store sheet only ever opens on a tap.
            if (TryUpgrade(track) == UpgradeResult.NotEnoughGems)
                onStatus(PurchaseMessages.NotEnoughGems);
        }

        private UpgradeResult TryUpgrade(UpgradeTrack track)
        {
            var result = progression.TryUpgrade(track, out var receipt);
            if (result == UpgradeResult.Upgraded)
            {
                receipts.Push(receipt);
                onUpgraded(track, receipt.Quote);
            }
            return result;
        }
    }
}
