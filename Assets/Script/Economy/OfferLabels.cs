namespace TreadShred.Economy
{
    public static class OfferLabels
    {
        // Plain-language copy for the result-screen offer and settings store.

        public static string ActionButton(string action, OfferPlan plan, string localizedPackPrice)
        {
            if (plan.CanSpendNow)
                return action + " FOR " + CountText.Format(plan.Cost) + " GEMS";
            return Pack(plan.PackToBuy, localizedPackPrice, "BUY ");
        }

        public static string Owned(int balance)
        {
            return "YOU HAVE " + CountText.Format(balance) + " GEMS";
        }

        public static string BalanceLine(int balance, OfferPlan plan)
        {
            return plan.CanSpendNow ? Owned(balance) : Owned(balance) + ". NEED " + CountText.Format(plan.Cost);
        }

        // Spending gems the player already holds is confirmed in a dialog first.
        // When the balance is short, the store's own purchase sheet is the
        // confirmation, so the boost still applies without a second tap (D-010).
        public static bool NeedsConfirmation(OfferPlan plan)
        {
            return plan.CanSpendNow;
        }

        public static GemConfirmation Confirmation(GemOffer offer, int balance)
        {
            var cost = GemCatalog.Cost(offer);
            var title = offer == GemOffer.Overdrive ? "ACTIVATE OVERDRIVE?" : "USE ARMORED REDEPLOY?";
            return new GemConfirmation(title, "SPEND " + CountText.Format(cost) + " OF YOUR " + CountText.Format(balance) + " GEMS",
                "SPEND " + CountText.Format(cost) + " GEMS", "CANCEL");
        }

        // Overdrive waits for the next mission, so the panel must say it is on.
        public const string OverdriveActive = "OVERDRIVE ACTIVE";

        public static string OverdriveActiveDetail(int gemsSpent)
        {
            return "NEXT MISSION: 2 SHOTS PER TAP. " + CountText.Format(gemsSpent) + " GEMS SPENT";
        }

        public static string Pack(GemPack pack, string localizedPrice, string prefix = "")
        {
            var label = prefix + CountText.Format(pack.Gems) + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + " FOR " + localizedPrice;
        }
    }
}
