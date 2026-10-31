using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Economy.Tests
{
    public sealed class CountTextTests
    {
        [Test(Description = "D-028-AC-04: counts of 1,000 or more use comma thousands separators, in every culture")]
        public void CountsUseCommaSeparators()
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.AreEqual("999", CountText.Format(999));
                Assert.AreEqual("1,200", CountText.Format(1200));
                Assert.AreEqual("250,800", CountText.Format(250800));
                Assert.AreEqual("536,861,911", CountText.Format(536861911));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test(Description = "D-028-AC-04: gem labels format their counts")]
        public void GemLabelsFormatCounts()
        {
            Assert.AreEqual("1,200 GEMS", OfferLabels.Pack(GemCatalog.Packs[2], string.Empty));
            Assert.AreEqual("YOU HAVE 99,999 GEMS", OfferLabels.Owned(99999));
            StringAssert.Contains("99,999 GEMS", OfferLabels.Confirmation(GemOffer.ArmoredRedeploy, 99999).Detail);
        }
    }
}
