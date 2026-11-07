using System;
using TreadShred.Analytics;
using TreadShred.Economy;
using UnityEngine;

/// <summary>
/// D-009, D-036, D-045: adds a unit of Overdrive supply from the upgrade screen's row (D-017-AC-08) and the
/// next-mission interstitial (D-040), more on top while some is held. Gems the player already holds are confirmed once;
/// a purchase goes straight to the store's sheet and adds the unit when the store delivers.
/// </summary>
public static class TreadShredOverdrive
{
    public static bool Active => TreadShredBoosts.Current.OverdriveActive;

    /// <summary>The one-price button: ACTIVATE FOR 100 GEMS (ADD while some is held), or the price when gems fall short.</summary>
    public static string ButtonLabel()
    {
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(GemOffer.Overdrive);
        return OfferLabels.ActionButton(Active ? "ADD" : "ACTIVATE", plan, plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy));
    }

    /// <param name="offerName">Where it was bought, for the offer_accepted event.</param>
    public static void Activate(Font font, string offerName, Action onArmed, Action<string> onStatus)
    {
        var store = TreadShredStore.Ensure();
        void Redeem()
        {
            var packBought = !store.PlanFor(GemOffer.Overdrive).CanSpendNow;
            store.RedeemOffer(GemOffer.Overdrive, () =>
            {
                TreadShredAnalytics.Log(GameEvents.OfferAccepted(offerName, GemCatalog.Cost(GemOffer.Overdrive), packBought));
                TreadShredProgress.Ensure().Progression.AddSupply(TreadShred.Progression.SupplyTrack.Overdrive);
                onArmed();
            }, onStatus);
        }

        if (OfferLabels.NeedsConfirmation(store.PlanFor(GemOffer.Overdrive)))
            TreadShredConfirmDialog.Show(OfferLabels.Confirmation(GemOffer.Overdrive, store.Wallet.Balance), font, Redeem);
        else
            Redeem();
    }
}
