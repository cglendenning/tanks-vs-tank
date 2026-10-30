using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public class CommandCopyTests
    {
        [Test(Description = "D-028-AC-03: legacy decorative two-part labels are replaced with single labels")]
        public void LegacyLabelsLoseDecorativeSeparators()
        {
            foreach (var entry in CommandCopy.LegacyLabels)
            {
                StringAssert.Contains("//", entry.Key);
                StringAssert.DoesNotContain("//", entry.Value);
                Assert.AreEqual(entry.Value, CommandCopy.Replace(entry.Key));
            }
            Assert.AreEqual("SCORE: 100", CommandCopy.Replace("SCORE // 100"));
        }
    }
}
