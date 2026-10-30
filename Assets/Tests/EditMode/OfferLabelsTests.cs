using NUnit.Framework;

namespace TreadShred.Economy.Tests
{
    public class OfferLabelsTests
    {
        [Test]
        public void ActionButtonNamesTheGemCostWhenAffordable()
        {
            var plan = GemCatalog.Plan(GemOffer.Overdrive, 100);

            Assert.AreEqual("ACTIVATE FOR 60 GEMS", OfferLabels.ActionButton("ACTIVATE", plan, "$0.99"));
        }

        [Test]
        public void ActionButtonOffersThePackWithPriceWhenShort()
        {
            var plan = GemCatalog.Plan(GemOffer.Overdrive, 40);

            Assert.AreEqual("BUY 100 GEMS FOR $0.99", OfferLabels.ActionButton("ACTIVATE", plan, "$0.99"));
        }

        [Test]
        public void ActionButtonOmitsPriceUntilTheStoreReportsIt()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 0);

            Assert.AreEqual("BUY 100 GEMS", OfferLabels.ActionButton("REDEPLOY", plan, null));
        }

        [Test]
        public void BalanceLineShowsOnlyTheBalanceWhenAffordable()
        {
            var plan = GemCatalog.Plan(GemOffer.ArmoredRedeploy, 100);

            Assert.AreEqual("YOU HAVE 100 GEMS", OfferLabels.BalanceLine(100, plan));
        }

        [Test]
        public void BalanceLineShowsWhatIsNeededWhenShort()
        {
            var plan = GemCatalog.Plan(GemOffer.Overdrive, 40);

            Assert.AreEqual("YOU HAVE 40 GEMS // NEED 60", OfferLabels.BalanceLine(40, plan));
        }

        [Test]
        public void PackShowsGemsAndPrice()
        {
            Assert.AreEqual("550 GEMS FOR $4.99", OfferLabels.Pack(GemCatalog.Packs[1], "$4.99"));
            Assert.AreEqual("1200 GEMS", OfferLabels.Pack(GemCatalog.Packs[2], string.Empty));
        }
    }
}
