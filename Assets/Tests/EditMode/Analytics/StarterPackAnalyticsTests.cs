using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class StarterPackAnalyticsTests
    {
        // R-STARTER-PACK's product ID; this assembly sees analytics only.
        private const string StarterProductId = "treadshred.starter300";

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-037-AC-05: each Starter Pack offer is recorded as offer_shown with offer starter_pack, and its purchase as gem_pack_purchased with its product ID")]
        public void OffersAndPurchasesAreRecorded()
        {
            var shown = GameEvents.OfferShown("starter_pack", 0);
            Assert.AreEqual(("offer_shown", "starter_pack"), (shown.Name, (string)shown.Parameters["offer"]));
            var bought = GameEvents.GemPackPurchased(StarterProductId, 0.99, "USD", "order-1");
            Assert.AreEqual(("gem_pack_purchased", StarterProductId), (bought.Name, (string)bought.Parameters["product_id"]));
            StringAssert.Contains("GameEvents.OfferShown(\"starter_pack\", store.Wallet.Balance)", Script("Game/TreadShredRevivePanel.cs"));
            StringAssert.Contains("GameEvents.OfferShown(\"starter_pack\", store.Wallet.Balance)", Script("TreadShredSettingsPanel.cs"));
        }
    }
}
