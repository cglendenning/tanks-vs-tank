namespace TreadShred.Analytics
{
    /// <summary>
    /// D-021-AC-07: purchases made with a store's test account (the App Store sandbox, Google Play test purchases). They
    /// credit gems like any purchase but are recorded apart and never reach attribution, so they never count as real
    /// sales or train the ad channels. When a check cannot tell, the purchase counts as real, so real money is never
    /// hidden.
    /// </summary>
    public static class TestPurchases
    {
        /// <summary>Apple names the app's receipt file this in the sandbox (TestFlight, Ad Hoc and Xcode builds).</summary>
        public const string AppStoreSandboxReceipt = "sandboxReceipt";

        /// <summary>The Google Play Store's package, the installer of every build players get from Google Play.</summary>
        public const string PlayStoreInstaller = "com.android.vending";

        public static bool AppStore(string receiptFileName) => receiptFileName == AppStoreSandboxReceipt;

        /// <summary>
        /// A build that did not come from the Play Store (a test build installed by hand), or a purchase Google issued
        /// without an order ID, which Google does for test purchases.
        /// </summary>
        public static bool GooglePlay(string installerPackage, GooglePlayPurchase? purchase) =>
            installerPackage != PlayStoreInstaller ||
            (purchase != null && string.IsNullOrEmpty(JsonText.String(purchase.Value.PurchaseData, "orderId")));
    }
}
