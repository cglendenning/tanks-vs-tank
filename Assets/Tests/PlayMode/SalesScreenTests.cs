using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TreadShred.Screens.Tests
{
    public sealed class SalesScreenTests
    {
        // The sales screen lives in the game assembly, which test assemblies reach by name.
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [UnityTest(Description = "D-034-AC-04: every sales screen, with the longest labels it can show, fits on every supported screen shape in the game's typeface")]
        public IEnumerator EverySalesScreenFits()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
            var show = GameType("TreadShredSalesScreen").GetMethod("Show");
            var press = GameType("TreadShredBack").GetMethod("Press");
            var longestButton = ProgressionLabels.UpgradeButton(new UpgradeQuote(2000, 1900), false);
            var findings = new List<Finding>();
            foreach (var item in SalesPitches.All)
            {
                var screen = (Component)show.Invoke(null, new object[]
                    { item, font, longestButton, "YOU HAVE 1,200 GEMS. NEED 1,250", true, (UnityAction)(() => { }) });
                yield return null;
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "sales " + item));
                }
                // The system Back closes it, as its own BACK button does (D-014-AC-03).
                press.Invoke(null, null);
                yield return null;
                if (screen != null)
                    findings.Add(new Finding(Defect.BackDoesNotClose, "[sales " + item + "] the system Back left it open"));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
