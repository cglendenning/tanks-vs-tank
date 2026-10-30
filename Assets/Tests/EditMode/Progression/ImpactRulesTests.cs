using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public class ImpactRulesTests
    {
        [Test(Description = "D-027: combat feedback timings match R-IMPACT")]
        public void ImpactTimingsMatchSpecification()
        {
            Assert.AreEqual(0.15f, ImpactRules.ShakeOnHitTaken, 1e-5f);
            Assert.AreEqual(0.25f, ImpactRules.ShakeOnKill, 1e-5f);
            Assert.AreEqual(0.06f, ImpactRules.KillFreezeFrame, 1e-5f);
            Assert.AreEqual(1.0f, ImpactRules.KillCallout, 1e-5f);
        }
    }
}
