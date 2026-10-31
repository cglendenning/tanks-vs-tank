using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>D-023: how attribution is wired into both platform builds.</summary>
    public sealed class AttributionBuildTests
    {
        private static string Read(string relative) =>
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), relative));

        private const string GeneratedManifest =
            "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">\n" +
            "  <application android:label=\"@string/app_name\">\n    <activity android:name=\"Main\" />\n  </application>\n</manifest>\n";

        [Test(Description = "D-023-AC-01: the Android build writes Tenjin's SDK key into the application element and rejects malformed keys")]
        public void AndroidBuildCarriesTheTenjinKey()
        {
            var manifest = AttributionBuildPolicy.WithTenjinSdkKey(GeneratedManifest, " 7XKQ2HDLWBTQKZ5JNSG3EPR6FCVMUAY4 ");

            StringAssert.Contains("<meta-data android:name=\"com.tgts.treadshred.TENJIN_SDK_KEY\" android:value=\"7XKQ2HDLWBTQKZ5JNSG3EPR6FCVMUAY4\" />\n</application>",
                manifest.Replace("  </application>", "</application>"));
            Assert.Throws<ArgumentException>(() => AttributionBuildPolicy.WithTenjinSdkKey(GeneratedManifest, "abc\" evil=\""));
            Assert.Throws<ArgumentException>(() => AttributionBuildPolicy.WithTenjinSdkKey(manifest, "OTHERKEY"), "one key only");
            Assert.Throws<ArgumentException>(() => AttributionBuildPolicy.WithTenjinSdkKey("<manifest/>", "KEY"));
        }

        [Test(Description = "D-023-AC-01: Android ships Tenjin's SDK through a bridge that reads the build's key and reports each purchase once")]
        public void AndroidShipsTenjin()
        {
            StringAssert.Contains("com.tenjin:android-sdk:2.0.2", Read("Assets/Editor/TreadShredTenjinDependencies.xml"));
            StringAssert.Contains("implementation 'com.tenjin:android-sdk:2.0.2'", Read("Assets/Plugins/Android/mainTemplate.gradle"));
            var bridge = Read("Assets/Plugins/Android/TreadShredTenjin.java");
            StringAssert.Contains("\"" + AttributionBuildPolicy.TenjinKeyMetaData + "\"", bridge);
            StringAssert.Contains("setAppStore(TenjinSDK.AppStoreType.googleplay)", bridge);
            StringAssert.Contains("disableAutoPurchaseTracking()", bridge, "the game's own report is the only one");
            StringAssert.Contains("AttributionBuildPolicy.TenjinKeyVariable", Read("Assets/Editor/TenjinAndroidBuild.cs"));
        }

        [Test(Description = "D-023-AC-04: iOS attribution uses StoreKit's SKAdNetwork only, with postback copies sent to Tenjin, and no Tenjin iOS SDK")]
        public void IosUsesSkAdNetworkOnly()
        {
            Assert.AreEqual("https://tenjin-skan.com", AttributionBuildPolicy.SkanReportEndpoint);
            Assert.AreEqual("NSAdvertisingAttributionReportEndpoint", AttributionBuildPolicy.SkanReportEndpointKey);
            StringAssert.Contains("SetString(AttributionBuildPolicy.SkanReportEndpointKey, AttributionBuildPolicy.SkanReportEndpoint)",
                Read("Assets/Editor/TankBuildAutomation.cs"));

            var plugin = Read("Assets/Plugins/iOS/TreadShredAdAttribution.m");
            StringAssert.Contains("#import <StoreKit/StoreKit.h>", plugin);
            StringAssert.Contains("updatePostbackConversionValue:fineValue coarseValue:value lockWindow:NO", plugin);
            foreach (var forbidden in new[] { "AdSupport", "ASIdentifierManager", "AppTrackingTransparency", "advertisingIdentifier" })
                StringAssert.DoesNotContain(forbidden, plugin, forbidden);
            Assert.IsFalse(Directory.GetFiles(Path.Combine(Application.dataPath, "Plugins", "iOS"))
                .Any(f => Path.GetFileName(f).StartsWith("Tenjin", StringComparison.OrdinalIgnoreCase)), "no Tenjin iOS SDK");
        }

        [Test(Description = "D-023-AC-04: conversion values use each gem pack's R-GEM-PACKS reference US price")]
        public void GemPacksCarryReferenceUsdPrices()
        {
            CollectionAssert.AreEqual(new[] { 99, 499, 999 }, GemCatalog.Packs.Select(pack => pack.UsdCents));
            StringAssert.Contains("pack.UsdCents", Read("Assets/Script/Game/TreadShredAttribution.cs"));
        }

        [Test(Description = "D-023-AC-02: the store reports a purchase to attribution only after crediting it and once the store confirms it")]
        public void StoreReportsConfirmedCreditedPurchases()
        {
            var store = Read("Assets/Script/Store/TreadShredStore.cs");
            var pending = store.Substring(store.IndexOf("private void OnPurchasePending", StringComparison.Ordinal));
            pending = pending.Substring(0, pending.IndexOf("private void OnPurchaseConfirmed", StringComparison.Ordinal));
            var credited = pending.Substring(pending.IndexOf("if (result == CreditResult.Credited)", StringComparison.Ordinal));
            StringAssert.Contains("awaitingAttribution[order.Info.TransactionID]", credited.Substring(0, credited.IndexOf('}')));

            var confirmed = store.Substring(store.IndexOf("private void OnPurchaseConfirmed", StringComparison.Ordinal));
            confirmed = confirmed.Substring(0, confirmed.IndexOf("private void OnPurchaseFailed", StringComparison.Ordinal));
            Assert.Less(confirmed.IndexOf("return;", StringComparison.Ordinal),
                confirmed.IndexOf("TreadShredAttribution.PurchaseConfirmed(", StringComparison.Ordinal), "a failed confirmation reports nothing");
            StringAssert.Contains("order.Info.Receipt", confirmed);
        }

        [Test(Description = "D-023-AC-06: the game has no AppLovin API client and cannot start, change or schedule ad spend")]
        public void GameCannotSpend()
        {
            var sources = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".java") || f.EndsWith(".m") || f.EndsWith(".mm"))
                .Where(f => !f.Replace('\\', '/').Contains("/Tests/"));
            foreach (var file in sources)
            {
                var text = File.ReadAllText(file).ToLowerInvariant();
                foreach (var host in new[] { "applovin.com", "axon.ai" })
                    StringAssert.DoesNotContain(host, text, Path.GetFileName(file) + " calls " + host);
            }
        }
    }
}
