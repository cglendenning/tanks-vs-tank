using System;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>
    /// One tap on an upgrade (D-017, D-018, D-010): buys the next level with coins, covers a coin shortfall with
    /// gems, and when gems are short too buys the smallest covering pack and upgrades once it is delivered. The
    /// screen calls Release when it closes, so a purchase still arriving credits its gems and upgrades nothing (D-011).
    /// </summary>
    public sealed class UpgradePurchase
    {
        private readonly PlayerProgression progression;
        private readonly GemWallet wallet;
        private readonly PurchaseFlow purchases;
        private readonly Action<UpgradeTrack, UpgradeQuote> onUpgraded;
        private readonly Action<string> onStatus;

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

        /// <summary>The screen closed: a purchase it started still credits its gems but upgrades nothing.</summary>
        public void Release()
        {
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
            var quote = progression.Quote(track);
            var result = progression.TryUpgrade(track);
            if (result == UpgradeResult.Upgraded)
                onUpgraded(track, quote);
            return result;
        }
    }
}
