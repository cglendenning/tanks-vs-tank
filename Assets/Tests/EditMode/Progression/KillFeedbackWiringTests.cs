using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class KillFeedbackWiringTests
    {
        private static readonly Regex KillCount = new Regex(@"^\s*ManagerScore\.instance\.CurDieEmnemy\s*\+=\s*1;", RegexOptions.Multiline);
        private static readonly Regex ImpactKill = new Regex(@"^\s*TreadShredImpact\.Kill\(", RegexOptions.Multiline);

        [Test(Description = "D-027-AC-02: every code path that counts an enemy kill also triggers the kill feedback")]
        public void EveryKillPathTriggersImpactFeedback()
        {
            var scripts = Path.Combine(Application.dataPath, "Script");
            var checkedFiles = 0;
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                var kills = KillCount.Matches(source).Count;
                if (kills == 0)
                    continue;
                checkedFiles++;
                Assert.That(ImpactKill.Matches(source).Count, Is.GreaterThanOrEqualTo(kills),
                    Path.GetFileName(file) + " counts a kill without calling TreadShredImpact.Kill");
            }
            Assert.That(checkedFiles, Is.GreaterThan(0), "no kill paths found; the scan pattern is stale");
        }
    }
}
