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

        public static string Pack(GemPack pack, string localizedPrice, string prefix = "")
        {
            var label = prefix + pack.Gems + " GEMS";
            return string.IsNullOrEmpty(localizedPrice) ? label : label + " FOR " + localizedPrice;
        }
    }
}
