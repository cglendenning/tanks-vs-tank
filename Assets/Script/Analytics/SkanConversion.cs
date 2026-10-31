using System;

namespace TreadShred.Analytics
{
    // D-023-AC-04: the iOS player's gem pack purchases as SKAdNetwork conversion values, so AppLovin and Tenjin
    // can measure ROAS without the tracking prompt. Fine value n means purchases totalling more than n - 1 and at
    // most n US dollars, capped at 63; Tenjin's conversion value settings must decode it the same way. The coarse
    // value, the only one Apple sends after the first two days, splits non-payers, payers and big payers.
    public static class SkanConversion
    {
        public const int MaxFineValue = 63;
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";

        // The largest gem pack's price: one big purchase makes a high-value player.
        public const int HighFromUsdCents = 999;

        public static int FineValue(int totalUsdCents)
        {
            if (totalUsdCents <= 0)
                return 0;
            return Math.Min(MaxFineValue, (totalUsdCents + 99) / 100);
        }

        public static string CoarseValue(int totalUsdCents)
        {
            if (totalUsdCents <= 0)
                return Low;
            return totalUsdCents < HighFromUsdCents ? Medium : High;
        }
    }
}
