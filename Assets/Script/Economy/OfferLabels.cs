namespace TreadShred.Economy
{
    public static class OfferLabels
    {
        public static string Button(string action, OfferPlan plan, string localizedPackPrice)
        {
            if (plan.CanSpendNow)
                return action + " // " + plan.Cost + " GEMS";
            if (string.IsNullOrEmpty(localizedPackPrice))
                return action + " // GET " + plan.PackToBuy.Gems + " GEMS";
            return action + " // " + plan.PackToBuy.Gems + " GEMS " + localizedPackPrice;
        }

        public static string Balance(int balance)
        {
            return "GEMS // " + balance;
        }
    }
}
