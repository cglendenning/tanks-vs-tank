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

        // Tenjin credits a fresh install on its server moments after the first launch reports it, so the answer to
        // the launch's own question is usually empty. The game asks again this many seconds after launch, and on every
        // return to the game, until the answer is settled.
        public static readonly float[] AskAgainAfterSeconds = { 15f, 60f, 180f, 600f };

        // Settled once Tenjin credits a network; organic may still turn into a network once Tenjin matches the click.
        public static bool Settled(Acquisition? acquisition) => acquisition != null && acquisition.Value.Source != Organic;

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
