using System.Collections.Generic;

namespace TreadShred.Analytics
{
    public sealed class AnalyticsEvent
    {
        public AnalyticsEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public string Name { get; }

        // Values are string, long or double, the types Firebase accepts.
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    // The custom events and user properties in R-ANALYTICS-EVENTS (D-021).
    public static class GameEvents
    {
        public const string AcquisitionSource = "acquisition_source";
        public const string AcquisitionCampaign = "acquisition_campaign";

        public static AnalyticsEvent MissionStart(int mission, string tier)
        {
            return Event("mission_start", ("mission", (long)mission), ("tier", tier));
        }

        public static AnalyticsEvent MissionEnd(int mission, string tier, bool won, double durationSeconds, int coinsAwarded)
        {
            return Event("mission_end", ("mission", (long)mission), ("tier", tier), ("result", won ? "win" : "loss"),
                ("duration_seconds", durationSeconds), ("coins_awarded", (long)coinsAwarded));
        }

        public static AnalyticsEvent UpgradePurchased(string track, int level, int coinsSpent, int gemsSpent)
        {
            return Event("upgrade_purchased", ("track", track), ("level", (long)level), ("coins_spent", (long)coinsSpent),
                ("gems_spent", (long)gemsSpent));
        }

        public static AnalyticsEvent OfferShown(string offer, int balanceGems)
        {
            return Event("offer_shown", ("offer", offer), ("balance_gems", (long)balanceGems));
        }

        public static AnalyticsEvent OfferAccepted(string offer, int gemsSpent, bool packBought)
        {
            return Event("offer_accepted", ("offer", offer), ("gems_spent", (long)gemsSpent), ("pack_bought", packBought ? 1L : 0L));
        }

        public static AnalyticsEvent GemPackPurchased(string productId, double price, string currency, string transactionId)
        {
            return Event("gem_pack_purchased", ("product_id", productId), ("price", price), ("currency", currency ?? string.Empty),
                ("transaction_id", transactionId ?? string.Empty));
        }

        private static AnalyticsEvent Event(string name, params (string Key, object Value)[] parameters)
        {
            var values = new Dictionary<string, object>();
            foreach (var parameter in parameters)
                values[parameter.Key] = parameter.Value;
            return new AnalyticsEvent(name, values);
        }
    }
}
