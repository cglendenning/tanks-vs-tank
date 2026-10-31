namespace TreadShred.Analytics
{
    public readonly struct Acquisition
    {
        public Acquisition(string source, string campaign)
        {
            Source = source;
            Campaign = campaign;
        }

        public string Source { get; }
        public string Campaign { get; }
    }

    // D-021-AC-05: what Tenjin reports about an Android install, as the acquisition user properties.
    public static class Attribution
    {
        public const string AppLovin = "applovin";
        public const string Organic = "organic";
        public const string NoCampaign = "none";

        // Firebase rejects user property values longer than 36 characters.
        public const int MaxPropertyLength = 36;

        // Null until Tenjin has attributed the install: an empty answer is not evidence of an organic install.
        public static Acquisition? FromTenjin(string adNetwork, string campaignName, string campaignId)
        {
            var network = (adNetwork ?? string.Empty).Trim().ToLowerInvariant();
            if (network.Length == 0)
                return null;
            if (network == Organic)
                return new Acquisition(Organic, NoCampaign);

            // Tenjin names AppLovin's channel after the network or after its Axon ad platform.
            var source = network.Contains("applovin") || network.Contains("axon") ? AppLovin : network;
            var campaign = !string.IsNullOrWhiteSpace(campaignName) ? campaignName.Trim()
                : !string.IsNullOrWhiteSpace(campaignId) ? campaignId.Trim()
                : NoCampaign;
            return new Acquisition(Fit(source), Fit(campaign));
        }

        private static string Fit(string value)
        {
            return value.Length <= MaxPropertyLength ? value : value.Substring(0, MaxPropertyLength);
        }
    }
}
