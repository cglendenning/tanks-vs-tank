namespace TreadShred.Economy
{
    public static class OfferLabels
    {
        // Plain-language copy for the result-screen offer and settings store.

        public static string ActionButton(string action, OfferPlan plan, string localizedPackPrice)
        {
            if (plan.CanSpendNow)
                return action + " FOR " + plan.Cost + " GEMS";
            return Pack(plan.PackToBuy, localizedPackPrice, "BUY ");
        }

        public static string Owned(int balance)
        {
            return "YOU HAVE " + balance + " GEMS";
        }

        public static string BalanceLine(int balance, OfferPlan plan)
        {
            return plan.CanSpendNow ? Owned(balance) : Owned(balance) + " // NEED " + plan.Cost;
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
            return new GemConfirmation(title, "SPEND " + cost + " OF YOUR " + balance + " GEMS",
                "SPEND " + cost + " GEMS", "CANCEL");
        }

        // Overdrive waits for the next mission, so the panel must say it is on.
        public const string OverdriveActive = "OVERDRIVE ACTIVE";

        public static string OverdriveActiveDetail(int gemsSpent)
        {
            return "NEXT MISSION: 2 SHOTS PER TAP // " + gemsSpent + " GEMS SPENT";
        }

        public static string Pack(GemPack pack, string localizedPrice, string prefix = "")
        {
            var label = prefix + pack.Gems + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + " FOR " + localizedPrice;
        }
    }
}
