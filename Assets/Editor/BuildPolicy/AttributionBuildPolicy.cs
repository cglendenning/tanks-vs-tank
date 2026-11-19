using System;
using System.Text.RegularExpressions;

namespace TreadShred.BuildPolicy
{
    /// <summary>
    /// D-023: build settings for install attribution. Android carries Tenjin's SDK key in its manifest; iOS asks
    /// Apple to copy SKAdNetwork install postbacks to Tenjin.
    /// </summary>
    public static class AttributionBuildPolicy
    {
        public const string SkanReportEndpointKey = "NSAdvertisingAttributionReportEndpoint";
        public const string SkanReportEndpoint = "https://tenjin-skan.com";

        // Read by Plugins/Android/TreadShredTenjin.java.
        public const string TenjinKeyMetaData = "com.tgts.treadshred.TENJIN_SDK_KEY";

        // The key stays out of the public repository, like the Firebase configuration files.
        public const string TenjinKeyVariable = "TREAD_SHRED_TENJIN_SDK_KEY";

        // Set by scripts/build-tank-android-release.sh for the bundle that goes to Google Play.
        public const string AndroidReleaseVariable = "TREAD_SHRED_ANDROID_RELEASE";

        private static readonly Regex KeyFormat = new Regex("^[A-Za-z0-9_-]+$");

        /// <summary>
        /// Why this Android build must stop, or null when it may go on: a release without Tenjin's SDK key would ship a
        /// game that credits no install to any channel or to organic (1.9.7 and 1.9.8 version code 17 did, 2026-11-18).
        /// A development build without the key still builds and only reports nothing.
        /// </summary>
        public static string MissingKeyStopsBuild(string sdkKey, bool release) =>
            release && string.IsNullOrWhiteSpace(sdkKey)
                ? TenjinKeyVariable + " is not set: a release would credit no Android install to its channel or to organic"
                : null;

        /// <summary>The Android manifest with Tenjin's SDK key added to its application element.</summary>
        public static string WithTenjinSdkKey(string manifest, string sdkKey)
        {
            var key = (sdkKey ?? string.Empty).Trim();
            if (!KeyFormat.IsMatch(key))
                throw new ArgumentException(TenjinKeyVariable + " must be letters, digits, '-' or '_'");
            if (manifest.Contains(TenjinKeyMetaData))
                throw new ArgumentException("the manifest already carries a Tenjin SDK key");
            var end = manifest.LastIndexOf("</application>", StringComparison.Ordinal);
            if (end < 0)
                throw new ArgumentException("the manifest has no application element");
            return manifest.Insert(end, "<meta-data android:name=\"" + TenjinKeyMetaData + "\" android:value=\"" + key + "\" />\n");
        }
    }
}
