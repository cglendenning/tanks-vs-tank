using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // The mission-10 defect: one enemy reported dead twice in a frame (two colliders, or Overdrive's paired rounds)
    // counted as two kills, so the mission cleared with an enemy still on the field.
    public sealed class MissionKillsTests
    {
        [Test(Description = "D-032-AC-01: an enemy reported dead by two rounds or two colliders in one frame counts as one kill")]
        public void RepeatReportsCountOnce()
        {
            var kills = new MissionKills();

            Assert.IsTrue(kills.Claim(4021), "the first report is the kill");
            Assert.IsFalse(kills.Claim(4021), "the second collider or round on the same enemy");
            Assert.IsFalse(kills.Claim(4021));
            Assert.AreEqual(1, kills.Count);
        }

        [Test(Description = "D-032-AC-01: with repeat reports, a nine-enemy mission still needs nine different enemies destroyed to clear")]
        public void MissionNeedsEveryEnemy()
        {
            var kills = new MissionKills();
            var remaining = 9;
            foreach (var report in new[] { 1, 1, 2, 3, 3, 4, 5, 6, 7, 8, 8 })
            {
                if (kills.Claim(report))
                    remaining--;
            }
            Assert.AreEqual(1, remaining, "eight distinct enemies down, one left: no clear");
            Assert.IsTrue(kills.Claim(9));
            Assert.AreEqual(9, kills.Count);
        }

        [Test(Description = "D-032-AC-01: every kill path in the bullet and enemy scripts claims the enemy before counting it")]
        public void EveryKillPathClaimsFirst()
        {
            foreach (var script in new[] { "Bullet/Bullet.cs", "Bullet/Bullet3.cs", "Bullet/BulletFind.cs", "Bullet/BulletVong.cs", "Emnemy/EmnemyRed.cs" })
            {
                var lines = File.ReadAllLines(Path.Combine(Application.dataPath, "Script", script)).Where(l => !l.TrimStart().StartsWith("//"));
                var code = string.Join("\n", lines);
                var counts = Regex.Matches(code, @"CurDieEmnemy \+= 1").Count;
                var claims = Regex.Matches(code, @"ClaimKill\(").Count;
                Assert.Greater(counts, 0, script);
                Assert.GreaterOrEqual(claims, counts, script + " counts a kill without claiming the enemy first");
            }
        }
    }
}
