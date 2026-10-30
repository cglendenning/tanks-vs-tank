using System.Linq;
using NUnit.Framework;

namespace TreadShred.Analytics.Tests
{
    public class GameEventsTests
    {
        private static void AssertEvent(AnalyticsEvent analyticsEvent, string name, params string[] parameters)
        {
            Assert.AreEqual(name, analyticsEvent.Name);
            CollectionAssert.AreEquivalent(parameters, analyticsEvent.Parameters.Keys.ToArray());
            foreach (var value in analyticsEvent.Parameters.Values)
                Assert.IsTrue(value is string || value is long || value is double, "Firebase accepts string, long or double");
        }

        [Test(Description = "D-021: every custom event carries exactly the parameters in R-ANALYTICS-EVENTS")]
        public void EventsMatchSpecification()
        {
            AssertEvent(GameEvents.MissionStart(3, "recruit"), "mission_start", "mission", "tier");
            AssertEvent(GameEvents.MissionEnd(3, "recruit", true, 41.5, 100), "mission_end",
                "mission", "tier", "result", "duration_seconds", "coins_awarded");
            AssertEvent(GameEvents.UpgradePurchased("armor", 2, 200, 5), "upgrade_purchased",
                "track", "level", "coins_spent", "gems_spent");
            AssertEvent(GameEvents.OfferShown("revive", 40), "offer_shown", "offer", "balance_gems");
            AssertEvent(GameEvents.OfferAccepted("revive", 30, true), "offer_accepted", "offer", "gems_spent", "pack_bought");
            AssertEvent(GameEvents.GemPackPurchased("treadshred.gems100", 0.99, "USD", "t1"), "gem_pack_purchased",
                "product_id", "price", "currency", "transaction_id");
        }

        [Test(Description = "D-021: purchase events report revenue and currency for LTV")]
        public void PurchaseEventReportsRevenue()
        {
            var purchase = GemEventsValues();
            Assert.AreEqual(4.99, (double)purchase.Parameters["price"], 1e-9);
            Assert.AreEqual("USD", purchase.Parameters["currency"]);
            Assert.AreEqual("win", GameEvents.MissionEnd(1, "recruit", true, 1, 100).Parameters["result"]);
            Assert.AreEqual("acquisition_source", GameEvents.AcquisitionSource);
            Assert.AreEqual("acquisition_campaign", GameEvents.AcquisitionCampaign);
        }

        private static AnalyticsEvent GemEventsValues()
        {
            return GameEvents.GemPackPurchased("treadshred.gems550", 4.99, "USD", "t2");
        }
    }
}
