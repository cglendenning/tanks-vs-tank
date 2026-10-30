using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Economy.Tests
{
    public class GemCatalogTests
    {
        [Test]
        public void PackProductIdsAreUniqueAndSizesAscend()
        {
            var ids = new HashSet<string>();
            for (var i = 0; i < GemCatalog.Packs.Count; i++)
            {
                Assert.IsTrue(ids.Add(GemCatalog.Packs[i].ProductId), "Duplicate product ID " + GemCatalog.Packs[i].ProductId);
                if (i > 0)
                    Assert.Greater(GemCatalog.Packs[i].Gems, GemCatalog.Packs[i - 1].Gems);
            }
        }

        [Test]
        public void ProductIdsAreValidForBothStores()
        {
            // Google Play: lowercase letters, digits, underscores and periods,
            // starting with a letter or digit. Apple accepts the same set.
            foreach (var pack in GemCatalog.Packs)
                StringAssert.IsMatch("^[a-z0-9][a-z0-9_.]*$", pack.ProductId);
        }

        [Test]
        public void EveryOfferIsAffordableWithTheStarterGrant()
        {
            Assert.LessOrEqual(GemCatalog.Cost(GemOffer.ArmoredRedeploy), GemCatalog.StarterGrant);
            Assert.LessOrEqual(GemCatalog.Cost(GemOffer.Overdrive), GemCatalog.StarterGrant);
        }

        [Test]
        public void FindPackReturnsNullForUnknownProduct()
        {
            Assert.IsNull(GemCatalog.FindPack("treadshred.unknown"));
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.FindPack(GemCatalog.Packs[1].ProductId));
        }

        [Test]
        public void PlanSpendsDirectlyWhenBalanceCoversCost()
        {
            var cost = GemCatalog.Cost(GemOffer.Overdrive);

            var plan = GemCatalog.Plan(GemOffer.Overdrive, cost);

            Assert.IsTrue(plan.CanSpendNow);
            Assert.AreEqual(cost, plan.Cost);
            Assert.IsNull(plan.PackToBuy);
        }

        [Test]
        public void PlanPicksSmallestPackThatCoversShortfall()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 0);

            Assert.IsFalse(plan.CanSpendNow);
            Assert.AreSame(GemCatalog.Packs[0], plan.PackToBuy);
        }

        [Test]
        public void SmallestPackCoveringSkipsPacksThatAreTooSmall()
        {
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.SmallestPackCovering(1));
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.SmallestPackCovering(GemCatalog.Packs[0].Gems + 1));
        }

        [Test]
        public void SmallestPackCoveringFallsBackToLargestPack()
        {
            var largest = GemCatalog.Packs[GemCatalog.Packs.Count - 1];

            Assert.AreSame(largest, GemCatalog.SmallestPackCovering(largest.Gems * 10));
        }

        [Test]
        public void ButtonShowsGemCostWhenAffordable()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 100);

            Assert.AreEqual("REDEPLOY // 40 GEMS", OfferLabels.Button("REDEPLOY", plan, "$0.99"));
        }

        [Test]
        public void ButtonShowsPackAndLocalizedPriceWhenShort()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 0);

            Assert.AreEqual("REDEPLOY // 100 GEMS $0.99", OfferLabels.Button("REDEPLOY", plan, "$0.99"));
        }

        [Test]
        public void ButtonOmitsPriceUntilStoreReportsIt()
        {
            var plan = GemCatalog.Plan(GemOffer.Overdrive, 0);

            Assert.AreEqual("OVERDRIVE // GET 100 GEMS", OfferLabels.Button("OVERDRIVE", plan, null));
        }

        [Test]
        public void BalanceLabelShowsCount()
        {
            Assert.AreEqual("GEMS // 25", OfferLabels.Balance(25));
        }
    }
}
