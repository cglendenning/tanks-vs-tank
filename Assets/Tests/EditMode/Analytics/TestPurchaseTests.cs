using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class TestPurchaseTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        private static GooglePlayPurchase Purchase(string json) => new GooglePlayPurchase(json, "signature");

        [Test(Description = "D-021-AC-07: an App Store sandbox purchase and a Google Play test purchase are told from real ones, and a purchase the game cannot judge counts as real")]
        public void TestPurchasesAreRecognised()
        {
            Assert.IsTrue(TestPurchases.AppStore("sandboxReceipt"));
            Assert.IsFalse(TestPurchases.AppStore("receipt"));
            Assert.IsFalse(TestPurchases.AppStore(string.Empty));
            var real = Purchase("{\"orderId\":\"GPA.3311-5521-4405-12345\",\"productId\":\"treadshred.gems100\"}");
            var orderless = Purchase("{\"productId\":\"treadshred.gems100\",\"purchaseToken\":\"abc\"}");
            Assert.IsFalse(TestPurchases.GooglePlay(TestPurchases.PlayStoreInstaller, real), "a Play Store install with an order ID is real");
            Assert.IsTrue(TestPurchases.GooglePlay(TestPurchases.PlayStoreInstaller, orderless), "Google leaves the order ID out of test purchases");
            Assert.IsTrue(TestPurchases.GooglePlay(null, real), "a build installed by hand is a test build");
            Assert.IsTrue(TestPurchases.GooglePlay("com.example.files", real));
            Assert.IsFalse(TestPurchases.GooglePlay(TestPurchases.PlayStoreInstaller, null), "an unreadable receipt counts as real");
        }

        [Test(Description = "D-021-AC-07: a test purchase is logged as test_purchase with the purchase fields and never reaches attribution; the editor's fake store is always a test")]
        public void TestPurchasesAreRecordedApart()
        {
            var test = GameEvents.TestPurchase("treadshred.gems100", 0.99, "USD", "txn-1");
            Assert.AreEqual(("test_purchase", "treadshred.gems100", "USD", "txn-1"),
                (test.Name, (string)test.Parameters["product_id"], (string)test.Parameters["currency"], (string)test.Parameters["transaction_id"]));
            Assert.AreEqual(0.99, (double)test.Parameters["price"], 1e-9);
            var store = Read("Script/Store/TreadShredStore.cs");
            StringAssert.Contains("var test = TreadShredTestPurchase.IsTest(order.Info.Receipt);", store);
            StringAssert.Contains("if (!test)\n                awaitingAttribution[order.Info.TransactionID] =", store);
            StringAssert.Contains("? TreadShred.Analytics.GameEvents.TestPurchase(productId, price, currency, transactionId)", store);
            StringAssert.Contains("#if UNITY_EDITOR\n        // The editor's fake store never charges anyone.\n        return true;", Read("Script/Store/TreadShredTestPurchase.cs"));
            StringAssert.Contains("NSBundle.mainBundle.appStoreReceiptURL.lastPathComponent", Read("Plugins/iOS/TreadShredStoreEnvironment.m"));
        }
    }
}
