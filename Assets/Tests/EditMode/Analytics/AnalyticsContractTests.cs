using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class AnalyticsContractTests
    {
        private static IEnumerable<string> GameScripts() =>
            Directory.GetFiles(Path.Combine(Application.dataPath, "Script"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Script/Analytics/"));

        [Test(Description = "D-021-AC-02: every event in R-ANALYTICS-EVENTS has its listed parameters and is logged from game code")]
        public void EveryEventIsLoggedWithItsParameters()
        {
            var catalog = new Dictionary<string, (AnalyticsEvent sample, string[] parameters)>
            {
                ["MissionStart"] = (GameEvents.MissionStart(3, "recruit"), new[] { "mission", "tier" }),
                ["MissionEnd"] = (GameEvents.MissionEnd(3, "recruit", true, 42.5, 100), new[] { "mission", "tier", "result", "duration_seconds", "coins_awarded" }),
                ["UpgradePurchased"] = (GameEvents.UpgradePurchased("armor", 2, 200, 0), new[] { "track", "level", "coins_spent", "gems_spent" }),
                ["OfferShown"] = (GameEvents.OfferShown("revive", 90), new[] { "offer", "balance_gems" }),
                ["OfferAccepted"] = (GameEvents.OfferAccepted("revive", 30, false), new[] { "offer", "gems_spent", "pack_bought" }),
                ["GemPackPurchased"] = (GameEvents.GemPackPurchased("treadshred.gems100", 0.99, "USD", "t1"), new[] { "product_id", "price", "currency", "transaction_id" }),
            };
            var sources = string.Join("\n", GameScripts().Select(File.ReadAllText));
            foreach (var entry in catalog)
            {
                CollectionAssert.AreEquivalent(entry.Value.parameters, entry.Value.sample.Parameters.Keys, entry.Key);
                StringAssert.Contains("GameEvents." + entry.Key + "(", sources, entry.Key + " is never logged");
            }
        }

        [Test(Description = "D-021-AC-03: gem pack purchases report revenue as a number with its currency code")]
        public void PurchasesReportRevenueAndCurrency()
        {
            var purchase = GameEvents.GemPackPurchased("treadshred.gems550", 4.99, "EUR", "txn-9");

            Assert.AreEqual(4.99, (double)purchase.Parameters["price"], 1e-9);
            Assert.AreEqual("EUR", purchase.Parameters["currency"]);
            Assert.AreEqual("txn-9", purchase.Parameters["transaction_id"]);
            var store = File.ReadAllText(Path.Combine(Application.dataPath, "Script/Store/TreadShredStore.cs"));
            StringAssert.Contains("(double)product.metadata.localizedPrice, product.metadata.isoCurrencyCode", store);
        }

        [Test(Description = "D-021-AC-04: analytics never blocks gameplay: events queue until ready and SDK failures are only logged")]
        public void AnalyticsFailuresNeverReachGameplay()
        {
            var sent = new List<string>();
            var warnings = new List<string>();
            var failing = true;
            var dispatcher = new AnalyticsDispatcher(e =>
            {
                if (failing)
                    throw new InvalidOperationException("no network");
                sent.Add(e.Name);
            }, warnings.Add);

            for (var i = 0; i < AnalyticsDispatcher.MaxQueued + 25; i++)
                dispatcher.Log(GameEvents.MissionStart(1, "recruit"));
            Assert.AreEqual(AnalyticsDispatcher.MaxQueued, dispatcher.Queued, "the queue is bounded while offline");

            Assert.DoesNotThrow(() => dispatcher.MarkReady());
            Assert.AreEqual(AnalyticsDispatcher.MaxQueued, warnings.Count);
            Assert.DoesNotThrow(() => dispatcher.Log(GameEvents.MissionEnd(1, "recruit", false, 3, 20)));

            failing = false;
            dispatcher.Log(GameEvents.OfferShown("revive", 10));
            Assert.AreEqual(new[] { "offer_shown" }, sent);
        }
    }
}
