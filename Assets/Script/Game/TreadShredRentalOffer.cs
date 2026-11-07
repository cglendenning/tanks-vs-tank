using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-043-AC-03, D-043-AC-04: the LOCKED side of the weapon toggle pauses the game on the weapon's sales screen (its clip,
/// what it does, the rental term). Renting pays the gems, or one tap buys the covering pack when short, puts the weapon in
/// hand and resumes; NOT NOW, a cancelled purchase or a failed one resumes the mission unchanged.
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
            OfferLabels.BalanceLine(store.Wallet.Balance, plan), true, () => Rent(player, weapon, !plan.CanSpendNow),
            WeaponRental.Decline, Resume);
    }

    private static void Rent(PlayerMoverment player, Weapon weapon, bool packBought)
    {
        TreadShredStore.Ensure().SpendOrBuy(WeaponRental.CostGems, () =>
        {
            TreadShredProgress.Ensure().Progression.StartRental(weapon);
            TreadShredAnalytics.Log(GameEvents.OfferAccepted(OfferName, WeaponRental.CostGems, packBought));
            if (player != null)
                player.Rent(weapon);
            Resume();
        }, status =>
        {
            Debug.Log("[Rental] " + weapon + " not rented: " + status);
            Resume();
        });
    }

    private static void Resume() => Time.timeScale = 1f;
}
