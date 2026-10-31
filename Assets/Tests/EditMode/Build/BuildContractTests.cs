using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>Checks on what the project ships: packages, manifests and store wiring.</summary>
    public sealed class BuildContractTests
    {
        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        private static string Read(string relative) => File.ReadAllText(Path.Combine(ProjectRoot, relative));

        private static string[] GameScripts() =>
            Directory.GetFiles(Path.Combine(Application.dataPath, "Script"), "*.cs", SearchOption.AllDirectories);

        // An Xcode project as Unity and the External Dependency Manager export it.
        private const string ExportedProject =
            "CAB9 /* FirebaseAnalytics in Frameworks */ = {isa = PBXBuildFile; productRef = 5EAB /* FirebaseAnalytics */; };\n" +
            "5EAB /* FirebaseAnalytics */ = {\n\tisa = XCSwiftPackageProductDependency;\n\tproductName = FirebaseAnalytics;\n};\n" +
            "7F32 /* UnityFirebaseAnalytics.m */ = {isa = PBXFileReference; name = UnityFirebaseAnalytics.m; };\n";

        [Test(Description = "D-021-AC-01: the iOS export links Firebase Analytics without advertising-ID support")]
        public void IosExportUsesAnalyticsWithoutAdvertisingId()
        {
            var patched = XcodeProjectPolicy.UseAnalyticsWithoutAdvertisingId(ExportedProject);

            StringAssert.Contains("productName = FirebaseAnalyticsCore;", patched);
            CollectionAssert.IsEmpty(XcodeProjectPolicy.AdvertisingIdViolations(patched, "<plist><dict/></plist>"));
            CollectionAssert.Contains(XcodeProjectPolicy.AdvertisingIdViolations(ExportedProject, ""),
                "productName = FirebaseAnalytics;", "the unpatched export is caught");
        }

        [Test(Description = "D-021-AC-01: Firebase never collects the Android advertising ID; the permission stays only for Tenjin's attribution (D-005, D-023)")]
        public void AndroidAdvertisingIdIsForTenjinOnly()
        {
            var manifest = Read("Assets/Plugins/Android/AndroidManifest.xml");

            StringAssert.Contains("<uses-permission android:name=\"com.google.android.gms.permission.AD_ID\" />", manifest);
            StringAssert.DoesNotContain("tools:node=\"remove\"", manifest, "Tenjin needs the advertising ID permission");
            StringAssert.Contains("<meta-data android:name=\"google_analytics_adid_collection_enabled\" android:value=\"false\" />", manifest);
            StringAssert.Contains("\"com.google.firebase.analytics\"", Read("Packages/manifest.json"), "the Firebase Analytics Unity SDK is integrated");
        }

        [Test(Description = "D-005-AC-01: no ad SDK is installed and no game script uses an ad API")]
        public void NoAdvertisingSdk()
        {
            var packages = Read("Packages/manifest.json") + Read("Packages/packages-lock.json");
            foreach (var sdk in new[] { "com.google.ads", "googlemobileads", "com.unity.ads", "com.unity.services.levelplay", "applovin", "ironsource", "com.unity.mediation" })
                StringAssert.DoesNotContain(sdk, packages.ToLowerInvariant(), sdk + " is installed");

            Assert.IsFalse(Directory.Exists(Path.Combine(Application.dataPath, "GoogleMobileAds")));
            var adApi = new Regex(@"GoogleMobileAds|UnityEngine\.Advertisements|RewardedAd|InterstitialAd|BannerView|ConsentInformation|MaxSdk");
            foreach (var script in GameScripts())
                Assert.IsFalse(adApi.IsMatch(File.ReadAllText(script)), Path.GetFileName(script) + " uses an ad API");
        }

        [Test(Description = "D-005-AC-02: nothing in the project requests App Tracking Transparency or declares a tracking usage description")]
        public void NoTrackingPermission()
        {
            CollectionAssert.Contains(XcodeProjectPolicy.AdvertisingIdViolations("", "<key>NSUserTrackingUsageDescription</key>"),
                "NSUserTrackingUsageDescription", "the build check rejects a tracking prompt");

            var sources = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".plist") || f.EndsWith(".xml") || f.EndsWith(".mm") || f.EndsWith(".m"))
                .Where(f => !f.Replace('\\', '/').Contains("/Editor/BuildPolicy/") && !f.Replace('\\', '/').Contains("/Tests/"));
            foreach (var file in sources)
            {
                var text = File.ReadAllText(file);
                Assert.IsFalse(text.Contains("NSUserTrackingUsageDescription") || text.Contains("ATTrackingManager"),
                    Path.GetFileName(file) + " asks for tracking permission");
            }
            StringAssert.DoesNotContain("ios-support", Read("Packages/manifest.json"));
        }

        [Test(Description = "D-006-AC-01: the manifest pins Unity IAP 5.x, which brings Google Play Billing Library 9 or later")]
        public void UnityIapAndPlayBillingVersions()
        {
            var pinned = Regex.Match(Read("Packages/manifest.json"), "\"com.unity.purchasing\":\\s*\"(\\d+)\\.");
            Assert.IsTrue(pinned.Success, "Unity IAP is not in the manifest");
            Assert.AreEqual("5", pinned.Groups[1].Value);

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.purchasing/package.json");
            Assert.IsNotNull(package, "Unity IAP is not resolved");
            var dependencies = File.ReadAllText(Path.Combine(package.resolvedPath,
                "Plugins/UnityPurchasing/Android/IAPResolver/IAPAndroidDependencies.cs"));
            var billing = Regex.Match(dependencies, @"com\.android\.billingclient:billing:(\d+)\.");
            Assert.IsTrue(billing.Success, "Unity IAP no longer declares Play Billing");
            Assert.GreaterOrEqual(int.Parse(billing.Groups[1].Value), 9);
            StringAssert.DoesNotContain("billingclient", Read("Assets/Plugins/Android/mainTemplate.gradle"),
                "the Gradle template must not pin an older Billing Library");
        }

        [Test(Description = "D-007-AC-03: displayed prices come from the store's localized price string, never hard-coded values")]
        public void PricesComeFromTheStore()
        {
            StringAssert.Contains("product.metadata.localizedPriceString", Read("Assets/Script/Store/TreadShredStore.cs"));
            var price = new Regex(@"""[^""]*([$€£¥]\s?\d|\d+[.,]\d\d\s?(USD|EUR|GBP))[^""]*""");
            foreach (var script in GameScripts())
            {
                var match = price.Match(File.ReadAllText(script));
                Assert.IsFalse(match.Success, Path.GetFileName(script) + " hard-codes a price: " + match.Value);
            }
            var pack = GemCatalog.Packs[0];
            Assert.AreEqual("BUY 100 GEMS", OfferLabels.Pack(pack, null, "BUY "), "no price is shown until the store reports one");
            StringAssert.EndsWith("1,09 €", OfferLabels.Pack(pack, "1,09 €", "BUY "), "the store's localized string is shown as given");
        }

        [Test(Description = "D-018-AC-03: only gem packs are store products; coins are never sold for real money")]
        public void OnlyGemsAreStoreProducts()
        {
            foreach (var pack in GemCatalog.Packs)
            {
                StringAssert.StartsWith("treadshred.gems", pack.ProductId);
                StringAssert.DoesNotContain("coin", pack.ProductId.ToLowerInvariant());
                Assert.Greater(pack.Gems, 0);
            }
            var store = Read("Assets/Script/Store/TreadShredStore.cs");
            StringAssert.Contains("GemCatalog.Packs", store);
            Assert.AreEqual(1, Regex.Matches(store, @"new ProductDefinition\(").Count, "products come only from the gem catalog");
        }
    }
}
