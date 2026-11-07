using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Economy.Tests
{
    public sealed class OfferConfirmationTests
    {
        [Test(Description = "D-009: spending gems already held asks first; a short balance goes straight to the store's own purchase sheet")]
        public void ConfirmsOnlyWhenSpendingHeldGems()
        {
            Assert.IsTrue(OfferLabels.NeedsConfirmation(GemCatalog.Plan(GemOffer.Overdrive, 120)));
            Assert.IsFalse(OfferLabels.NeedsConfirmation(GemCatalog.Plan(GemOffer.Overdrive, 10)),
                "when short, one tap opens the purchase sheet (D-010)");
        }

        [Test(Description = "D-009: the confirmation names the boost, its gem cost and the balance it comes from")]
        public void ConfirmationStatesWhatIsBoughtAndItsCost()
        {
            var overdrive = OfferLabels.Confirmation(GemOffer.Overdrive, 120);
            Assert.AreEqual("ACTIVATE OVERDRIVE?", overdrive.Title);
            Assert.AreEqual("SPEND 100 OF YOUR 120 GEMS", overdrive.Detail);
            Assert.AreEqual("SPEND 100 GEMS", overdrive.ConfirmButton);
            Assert.AreEqual("CANCEL", overdrive.CancelButton);

            var redeploy = OfferLabels.Confirmation(GemOffer.ArmoredRedeploy, 45);
            Assert.AreEqual("USE ARMORED REDEPLOY?", redeploy.Title);
            Assert.AreEqual("SPEND 40 OF YOUR 45 GEMS", redeploy.Detail);
            Assert.AreEqual("SPEND 40 GEMS", redeploy.ConfirmButton);
        }

        [Test(Description = "D-009: once bought, the Overdrive panel says it is active, what it does and what it cost")]
        public void OverdriveActiveStateIsExplicit()
        {
            Assert.AreEqual("OVERDRIVE ACTIVE", OfferLabels.OverdriveActive);
            Assert.AreEqual("60 GEMS SPENT", OfferLabels.OverdriveActiveDetail(60));
        }
    }
}
