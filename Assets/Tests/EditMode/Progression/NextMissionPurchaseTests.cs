using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class NextMissionPurchaseTests
    {
        [Test(Description = "D-040-AC-05: an offer covered by coins or gems the player holds adds a pack, the Starter Pack while on sale and else the smallest pack; an offer whose own button buys a pack adds none")]
        public void CoveredOffersAddAPack()
        {
            Assert.AreSame(GemCatalog.StarterPack, GemCatalog.PackAlongside(true, true));
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.PackAlongside(true, false));
            Assert.IsNull(GemCatalog.PackAlongside(false, true));
            Assert.IsNull(GemCatalog.PackAlongside(false, false));
            Assert.AreEqual("+100 GEMS ADDED", OfferLabels.GemsAdded(GemCatalog.Packs[0]));
        }

        [Test(Description = "D-040-AC-05: the next-mission offer adds the pack as its main button when its own price is covered, records the purchase as pack_next_mission, and stays open so the gems can be spent")]
        public void TheOfferSellsThePack()
        {
            var offer = File.ReadAllText(Path.Combine(Application.dataPath, "Script", "Game", "TreadShredNextMissionOffer.cs"));
            StringAssert.Contains("pack = GemCatalog.PackAlongside(CoveredByHeldCurrency(), store.StarterPackOnSale);", offer);
            StringAssert.Contains("TreadShredUi.StyleStandard(buyButton);\n            TreadShredUi.StylePrimary(packButton);", offer);
            StringAssert.Contains("TreadShredAnalytics.Log(GameEvents.OfferAccepted(\"pack_next_mission\", 0, true));", offer);
            StringAssert.Contains("statusText.text = OfferLabels.GemsAdded(bought);", offer, "the offer stays open after the pack");
        }
    }
}
