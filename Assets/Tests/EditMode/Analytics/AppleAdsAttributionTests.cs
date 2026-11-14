using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class AppleAdsAttributionTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        // Apple's documented answer for an install an Apple Ads campaign brought.
        private const string Attributed = "{\"attribution\":true,\"orgId\":40669820,\"campaignId\":542370539,\"conversionType\":\"Download\"," +
                                          "\"clickDate\":\"2020-04-08T17:17Z\",\"adGroupId\":542317095,\"countryOrRegion\":\"US\",\"keywordId\":87675432,\"adId\":542317136}";

        [Test(Description = "D-023-AC-01: an iPhone install an Apple Ads campaign brought is tagged apple_ads with its campaign; any other answer tags nothing")]
        public void AppleAdsInstallsAreTagged()
        {
            var acquisition = Attribution.FromAppleAds(Attributed);
            Assert.IsTrue(acquisition.HasValue);
            Assert.AreEqual(("apple_ads", "542370539"), (acquisition.Value.Source, acquisition.Value.Campaign));
            Assert.AreEqual(("apple_ads", Attribution.NoCampaign), (Attribution.FromAppleAds("{\"attribution\":true}").Value.Source,
                Attribution.FromAppleAds("{\"attribution\":true}").Value.Campaign));
            Assert.IsNull(Attribution.FromAppleAds("{\"attribution\":false}"), "another channel's install is not called organic");
            Assert.IsNull(Attribution.FromAppleAds(string.Empty));
            Assert.IsNull(Attribution.FromAppleAds("not json"));
            Assert.AreEqual("https://api-adservices.apple.com/api/v1/", Attribution.AppleAdsEndpoint);
        }

        [Test(Description = "D-023-AC-01: the iPhone game reads Apple's AdServices token once per install and asks Apple's attribution API, retrying only Apple's not-ready answer, with no advertising identifier and no tracking prompt")]
        public void AppleAdsAskedOncePerInstall()
        {
            var plugin = Read("Plugins/iOS/TreadShredAdAttribution.m");
            StringAssert.Contains("#import <AdServices/AdServices.h>", plugin);
            StringAssert.Contains("[AAAttribution attributionTokenWithError:&error]", plugin);
            foreach (var forbidden in new[] { "AdSupport", "ASIdentifierManager", "AppTrackingTransparency", "advertisingIdentifier" })
                StringAssert.DoesNotContain(forbidden, plugin, forbidden);
            var game = Read("Script/Game/TreadShredAttribution.cs");
            StringAssert.Contains("if (PlayerPrefs.GetInt(AppleAdsCheckedKey, 0) == 1)\n            yield break;", game);
            StringAssert.Contains("new UnityEngine.Networking.UnityWebRequest(Attribution.AppleAdsEndpoint, \"POST\")", game);
            StringAssert.Contains("for (var attempt = 1; attempt <= 3; attempt++)", game);
            StringAssert.Contains("if (request.responseCode != 404)\n                    yield break;", game);
            StringAssert.Contains("yield return new WaitForSecondsRealtime(5f);", game);
            Assert.Less(game.IndexOf("TreadShredAnalytics.SetUserProperty(GameEvents.AcquisitionSource, acquisition.Value.Source);",
                    game.IndexOf("private System.Collections.IEnumerator AppleAds()", StringComparison.Ordinal), StringComparison.Ordinal),
                game.IndexOf("PlayerPrefs.SetInt(AppleAdsCheckedKey, 1);", StringComparison.Ordinal), "tagged before it is marked done");
        }
    }
}
