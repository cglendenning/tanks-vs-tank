using System.Linq;
using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class ShieldRulesTests
    {
        [Test(Description = "D-017-AC-09: the shields go out one at a time in line from the one beside +N, so the lit ones are always the last in line and a middle shield is never out alone")]
        public void ShieldsGoOutInLine()
        {
            const int shields = 3;
            for (var armor = -1; armor <= 8; armor++)
            {
                var lit = Enumerable.Range(0, shields).Select(place => ShieldRules.IsLit(place, armor, shields)).ToArray();
                var expected = Enumerable.Range(0, shields).Select(place => place >= shields - System.Math.Max(0, System.Math.Min(armor, shields))).ToArray();
                CollectionAssert.AreEqual(expected, lit, "armor " + armor);
                Assert.AreEqual(lit.Count(l => l), ShieldRules.Lit(armor, shields), "armor " + armor + ": one lit shield per hit left, at most three");
                for (var place = 1; place < shields; place++)
                    Assert.IsFalse(lit[place - 1] && !lit[place], "armor " + armor + ": a shield is out only when every shield before it in line is out");
            }
            Assert.AreEqual(0.15f, ShieldRules.DimAlpha, 1e-5f);
        }
    }
}
