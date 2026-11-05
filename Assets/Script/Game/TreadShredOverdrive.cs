using System;
using TreadShred.Analytics;
using TreadShred.Economy;
using UnityEngine;

/// <summary>
/// D-009, D-036: arms Overdrive for the next mission from the upgrade screen's row (D-017-AC-08) and the next-mission
/// interstitial (D-040). Gems the player already holds are confirmed once; a purchase goes straight to the store's
/// sheet and arms Overdrive when the store delivers.
/// </summary>
public static class TreadShredOverdrive
{
    public static bool Active => TreadShredBoosts.Current.OverdriveActive;

    /// <summary>The one-price button: ACTIVATE FOR 60 GEMS, or ACTIVATE: price when the gems fall short.</summary>
    public static string ButtonLabel()
    {
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(GemOffer.Overdrive);
        return OfferLabels.ActionButton("ACTIVATE", plan, plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy));
    }

    /// <param name="offerName">Where it was bought, for the offer_accepted event.</param>
    public static void Activate(Font font, string offerName, Action onArmed, Action<string> onStatus)
    {
        if (Active)
            return;
        var store = TreadShredStore.Ensure();
        void Redeem()
        {
            var packBought = !store.PlanFor(GemOffer.Overdrive).CanSpendNow;
            store.RedeemOffer(GemOffer.Overdrive, () =>
            {
                TreadShredAnalytics.Log(GameEvents.OfferAccepted(offerName, GemCatalog.Cost(GemOffer.Overdrive), packBought));
                TreadShredBoosts.Current.ArmOverdrive();
                onArmed();
            }, onStatus);
        }

        if (OfferLabels.NeedsConfirmation(store.PlanFor(GemOffer.Overdrive)))
            TreadShredConfirmDialog.Show(OfferLabels.Confirmation(GemOffer.Overdrive, store.Wallet.Balance), font, Redeem);
        else
            Redeem();
    }
}
