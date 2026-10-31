using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// What a person checking every screen on a phone, a 16:9 device and an iPad would catch,
    /// checked by ScreenSweep instead. Any screen the sweep cannot reach fails every check.
    /// </summary>
    public sealed class ScreenLayoutTests
    {
        private static ScreenSweep sweep;

        private static IEnumerator Swept()
        {
            if (sweep != null)
                yield break;
            var run = new ScreenSweep();
            yield return run.Run();
            sweep = run;
            Debug.Log("[ScreenSweep] audited " + run.Reached.Count + " screens on " + ScreenAudit.Shapes.Length +
                      " screen shapes: " + string.Join(", ", run.Reached));
            if (run.Errors.Count > 0)
                Debug.LogWarning("[ScreenSweep] errors logged during play:\n" + string.Join("\n", run.Errors.Distinct()));
        }

        private static void AssertNone(params Defect[] defects)
        {
            var found = sweep.Findings.Where(f => f.Defect == Defect.NotReached || defects.Contains(f.Defect)).ToList();
            Assert.IsEmpty(found, ScreenAudit.Report(found));
        }

        [UnityTest(Description = "D-013-AC-04: on every supported screen shape no text overlaps other text, is cut off by the screen edge, spills past its button, runs into another button, sits partly under anything drawn over it, or reads upside down or mirrored")]
        public IEnumerator TextFitsOnEveryScreen()
        {
            yield return Swept();
            AssertNone(Defect.Overlap, Defect.CutOff, Defect.SpillsPastButton, Defect.CrowdsButton, Defect.PartlyHidden,
                Defect.NotUpright);
        }

        [UnityTest(Description = "D-013-AC-03: every visible label on every screen uses the Black Ops One typeface")]
        public IEnumerator EveryLabelUsesTheTypeface()
        {
            yield return Swept();
            AssertNone(Defect.WrongFont);
        }

        [UnityTest(Description = "D-028-AC-01: all player-facing copy on every screen is all caps")]
        public IEnumerator AllCopyIsAllCaps()
        {
            yield return Swept();
            AssertNone(Defect.NotAllCaps);
        }
    }
}
