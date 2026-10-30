using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Economy.Tests
{
    public class GemCatalogTests
    {
        [Test(Description = "D-007-AC-01: pack product IDs are unique and sizes ascend")]
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

        [Test(Description = "D-007-AC-01: product IDs are valid for both stores")]
        public void ProductIdsAreValidForBothStores()
        {
            // Google Play: lowercase letters, digits, underscores and periods,
            // starting with a letter or digit. Apple accepts the same set.
            foreach (var pack in GemCatalog.Packs)
                StringAssert.IsMatch("^[a-z0-9][a-z0-9_.]*$", pack.ProductId);
        }

        [Test(Description = "D-008-AC-02: every boost is affordable with the starter grant")]
        public void EveryOfferIsAffordableWithTheStarterGrant()
        {
            Assert.LessOrEqual(GemCatalog.Cost(GemOffer.ArmoredRedeploy), GemCatalog.StarterGrant);
            Assert.LessOrEqual(GemCatalog.Cost(GemOffer.Overdrive), GemCatalog.StarterGrant);
        }

        [Test(Description = "D-011-AC-02: unknown product IDs resolve to no pack")]
        public void FindPackReturnsNullForUnknownProduct()
        {
            Assert.IsNull(GemCatalog.FindPack("treadshred.unknown"));
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.FindPack(GemCatalog.Packs[1].ProductId));
        }

        [Test(Description = "D-010-AC-01: the plan spends directly when the balance covers the cost")]
        public void PlanSpendsDirectlyWhenBalanceCoversCost()
        {
            var cost = GemCatalog.Cost(GemOffer.Overdrive);

            var plan = GemCatalog.Plan(GemOffer.Overdrive, cost);

            Assert.IsTrue(plan.CanSpendNow);
            Assert.AreEqual(cost, plan.Cost);
            Assert.IsNull(plan.PackToBuy);
        }

        [Test(Description = "D-010-AC-01: the plan picks the smallest pack covering the shortfall")]
        public void PlanPicksSmallestPackThatCoversShortfall()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 0);

            Assert.IsFalse(plan.CanSpendNow);
            Assert.AreSame(GemCatalog.Packs[0], plan.PackToBuy);
        }

        [Test(Description = "D-010-AC-01: packs too small for the shortfall are skipped")]
        public void SmallestPackCoveringSkipsPacksThatAreTooSmall()
        {
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.SmallestPackCovering(1));
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.SmallestPackCovering(GemCatalog.Packs[0].Gems + 1));
        }

        [Test(Description = "D-010-AC-01: the largest pack is used when none covers the shortfall")]
        public void SmallestPackCoveringFallsBackToLargestPack()
        {
            var largest = GemCatalog.Packs[GemCatalog.Packs.Count - 1];

            Assert.AreSame(largest, GemCatalog.SmallestPackCovering(largest.Gems * 10));
        }
    }
}
