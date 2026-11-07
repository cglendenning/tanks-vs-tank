using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-043-AC-03, D-043-AC-04: a LOCKED weapon in the drawer pauses the game on the weapon's sales screen (its clip, what
/// it does, the rental term), and the game stays paused through the whole purchase. Renting pays the gems, or one tap
/// buys the covering pack when short, puts the weapon in hand and waits on READY? (D-020-AC-06); NOT NOW, a cancelled
/// purchase or a failed one plays the 3-2-1 and returns to the mission unchanged.
/// </summary>
public static class TreadShredRentalOffer
{
    public const string OfferName = "weapon_rental";

    public static void Show(PlayerMoverment player, Weapon weapon, Font font)
    {
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(WeaponRental.CostGems);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        Time.timeScale = 0f;
        TreadShredAnalytics.Log(GameEvents.OfferShown(OfferName, store.Wallet.Balance));
        TreadShredSalesScreen.ShowWithDecline(SalesPitches.ForWeapon(weapon), font, OfferLabels.ActionButton("RENT", plan, price),
            OfferLabels.BalanceLine(store.Wallet.Balance, plan), true, () => Rent(player, weapon, !plan.CanSpendNow, font),
            WeaponRental.Decline, Resume);
    }

    private static void Rent(PlayerMoverment player, Weapon weapon, bool packBought, Font font)
    {
        TreadShredStore.Ensure().SpendOrBuy(WeaponRental.CostGems, () =>
        {
            TreadShredProgress.Ensure().Progression.StartRental(weapon);
            TreadShredAnalytics.Log(GameEvents.OfferAccepted(OfferName, WeaponRental.CostGems, packBought));
            if (player != null)
                player.Rent(weapon);
            TreadShredReadyPanel.Show(font);
        }, status =>
        {
            // The store opening or a purchase under way keeps the game paused; only an outcome returns to the mission.
            if (!PurchaseMessages.Ends(status))
                return;
            Debug.Log("[Rental] " + weapon + " not rented: " + status);
            Resume();
        });
    }

    // Back to the mission after the 3-2-1.
    private static void Resume()
    {
        if (Count.Replay())
            return;
        Debug.LogWarning("[Rental] no countdown in this scene; play resumes at once");
        Time.timeScale = 1f;
    }
}
