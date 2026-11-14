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

    // D-021-AC-05: what Tenjin reports about an Android install, and what Apple Ads reports about an iPhone install, as
    // the acquisition user properties.
    public static class Attribution
    {
        public const string AppLovin = "applovin";
        public const string AppleAds = "apple_ads";

        /// <summary>D-023-AC-01: Apple's attribution API, which answers for an AdServices token with no tracking prompt.</summary>
        public const string AppleAdsEndpoint = "https://api-adservices.apple.com/api/v1/";

        /// <summary>
        /// D-023-AC-01: an install an Apple Ads campaign brought, from Apple's answer; null for any other install (which
        /// may still have come from another channel, so it is not called organic) or an answer that cannot be read.
        /// </summary>
        public static Acquisition? FromAppleAds(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer) ||
                !System.Text.RegularExpressions.Regex.IsMatch(answer, "\"attribution\"\\s*:\\s*true\\b"))
                return null;
            // Apple's answer is one flat JSON object; its campaign is a whole number.
            var campaign = System.Text.RegularExpressions.Regex.Match(answer, "\"campaignId\"\\s*:\\s*(\\d+)");
            return new Acquisition(AppleAds, campaign.Success ? Fit(campaign.Groups[1].Value) : NoCampaign);
        }
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
