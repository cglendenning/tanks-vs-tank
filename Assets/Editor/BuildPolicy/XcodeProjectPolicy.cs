using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TreadShred.BuildPolicy
{
    /// <summary>
    /// D-005 and D-021-AC-01: the iOS app links Firebase Analytics without
    /// advertising-identifier support or ads measurement, and never asks for
    /// tracking permission.
    /// </summary>
    public static class XcodeProjectPolicy
    {
        // FirebaseAnalytics pulls in GoogleAppMeasurementIdentitySupport (IDFA)
        // and GoogleAdsOnDeviceConversion; FirebaseAnalyticsCore is the same
        // analytics on GoogleAppMeasurementCore without either.
        public const string AnalyticsProduct = "FirebaseAnalyticsCore";

        private static readonly Regex FullAnalyticsProduct = new Regex(@"productName = FirebaseAnalytics;");

        private static readonly string[] Forbidden =
        {
            "productName = FirebaseAnalytics;",
            "FirebaseAnalyticsIdentitySupport",
            "GoogleAppMeasurementIdentitySupport",
            "AdSupport.framework",
            "AppTrackingTransparency.framework",
        };

        /// <summary>Points the Firebase Analytics package reference at the product without ad-ID support.</summary>
        public static string UseAnalyticsWithoutAdvertisingId(string pbxproj)
        {
            return FullAnalyticsProduct.Replace(pbxproj, "productName = " + AnalyticsProduct + ";");
        }

        /// <summary>Everything in an Xcode project or Info.plist that would collect the ad ID or prompt for tracking.</summary>
        public static IReadOnlyList<string> AdvertisingIdViolations(string pbxproj, string infoPlist)
        {
            var found = new List<string>();
            foreach (var marker in Forbidden)
            {
                if (pbxproj.Contains(marker))
                    found.Add(marker);
            }
            if (infoPlist.Contains("NSUserTrackingUsageDescription"))
                found.Add("NSUserTrackingUsageDescription");
            return found;
        }
    }
}
