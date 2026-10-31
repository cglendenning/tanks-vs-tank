using NUnit.Framework;

namespace TreadShred.Analytics.Tests
{
    public sealed class AttributionTests
    {
        private static string Quote(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        // A Unity IAP Google Play receipt: the purchase JSON is a string inside the payload, which is a string inside
        // the receipt. The purchase carries its own "signature" field to prove only the payload's top-level one is read.
        private const string PurchaseJson =
            "{\"orderId\":\"GPA.3371-0042\",\"packageName\":\"com.tgts.tankkvstank\",\"productId\":\"treadshred.gems550\"," +
            "\"purchaseToken\":\"tok\\u00e9n\",\"developerPayload\":\"{\\\"signature\\\":\\\"fake\\\"}\"}";

        private static string Receipt(string store, string payload) =>
            "{\"Payload\":" + Quote(payload) + ",\"Store\":" + Quote(store) + ",\"TransactionID\":\"GPA.3371-0042\"}";

        private static string Payload(string json, string signature) =>
            "{\"json\":" + Quote(json) + ", \"signature\" : " + Quote(signature) +
            ",\"skuDetails\":[" + Quote("{\"productId\":\"treadshred.gems550\"}") + "]}";

        [Test(Description = "D-021-AC-05: an AppLovin install is labelled with its campaign; Axon is AppLovin's ad platform")]
        public void AppLovinInstallCarriesItsCampaign()
        {
            var install = Attribution.FromTenjin("AppLovin", "TS_US_Video_Hook3", "8812").Value;
            Assert.AreEqual(("applovin", "TS_US_Video_Hook3"), (install.Source, install.Campaign));
            Assert.AreEqual("applovin", Attribution.FromTenjin("axon", "c", "1").Value.Source);
            Assert.AreEqual("8812", Attribution.FromTenjin("applovin", " ", "8812").Value.Campaign, "campaign ID when the name is missing");
        }

        [Test(Description = "D-021-AC-05: organic installs are labelled organic, and an install Tenjin has not attributed yet is left unlabelled")]
        public void OrganicAndUnknownInstalls()
        {
            var organic = Attribution.FromTenjin("organic", "", "").Value;
            Assert.AreEqual(("organic", "none"), (organic.Source, organic.Campaign));
            Assert.IsNull(Attribution.FromTenjin("", "", ""), "no answer is not evidence of an organic install");
            Assert.IsNull(Attribution.FromTenjin(null, null, null));
        }

        [Test(Description = "D-021-AC-05: labels fit Firebase's 36-character user property limit")]
        public void LabelsFitFirebaseLimits()
        {
            var install = Attribution.FromTenjin("applovin", "TreadShred_US_Android_Video_Hook_Revenge_v12", "").Value;
            Assert.AreEqual(Attribution.MaxPropertyLength, install.Campaign.Length);
            StringAssert.StartsWith("TreadShred_US_Android", install.Campaign);
        }

        [Test(Description = "D-023-AC-04: purchase revenue becomes a fine value of whole US dollars rounded up, capped at 63")]
        public void FineValueCountsDollars()
        {
            Assert.AreEqual(0, SkanConversion.FineValue(0));
            Assert.AreEqual(1, SkanConversion.FineValue(99));
            Assert.AreEqual(1, SkanConversion.FineValue(100));
            Assert.AreEqual(5, SkanConversion.FineValue(499));
            Assert.AreEqual(6, SkanConversion.FineValue(99 + 499));
            Assert.AreEqual(10, SkanConversion.FineValue(999));
            Assert.AreEqual(63, SkanConversion.FineValue(6300));
            Assert.AreEqual(63, SkanConversion.FineValue(50000), "capped at Apple's largest fine value");
        }

        [Test(Description = "D-023-AC-04: the coarse value separates non-payers, payers and buyers of the largest pack")]
        public void CoarseValueSplitsPayers()
        {
            Assert.AreEqual("low", SkanConversion.CoarseValue(0));
            Assert.AreEqual("medium", SkanConversion.CoarseValue(99));
            Assert.AreEqual("medium", SkanConversion.CoarseValue(998));
            Assert.AreEqual("high", SkanConversion.CoarseValue(999));
        }

        [Test(Description = "D-023-AC-02: Tenjin receives the Google Play purchase data and signature exactly as the store signed them")]
        public void GooglePlayReceiptYieldsSignedPurchase()
        {
            var purchase = PurchaseReceipt.GooglePlay(Receipt("GooglePlay", Payload(PurchaseJson, "c2ln/+Rk=="))).Value;

            Assert.AreEqual(PurchaseJson, purchase.PurchaseData);
            Assert.AreEqual("c2ln/+Rk==", purchase.Signature);
        }

        [Test(Description = "D-023-AC-02: receipts that are not complete Google Play receipts yield nothing to report")]
        public void OtherReceiptsYieldNothing()
        {
            Assert.IsNull(PurchaseReceipt.GooglePlay(Receipt("AppleAppStore", "MIIT...")));
            Assert.IsNull(PurchaseReceipt.GooglePlay(Receipt("GooglePlay", "{\"json\":" + Quote(PurchaseJson) + "}")), "no signature");
            Assert.IsNull(PurchaseReceipt.GooglePlay("{\"Store\":\"GooglePlay\",\"Payload\":\"{\\\"json\\\":\\\"unterminated"));
            Assert.IsNull(PurchaseReceipt.GooglePlay(""));
            Assert.IsNull(PurchaseReceipt.GooglePlay(null));
        }
    }
}
