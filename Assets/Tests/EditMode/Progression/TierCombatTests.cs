using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class TierCombatTests
    {
        private static string Script(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-046-AC-01: Recruit fights in one wave, Veteran in two and Elite in three; the next wave comes once half of the current one is destroyed, before the victory count drops")]
        public void WavesComeAtHalf()
        {
            Assert.AreEqual((1, 2, 3), (TierCombat.For(DifficultyTier.Recruit).Waves, TierCombat.For(DifficultyTier.Veteran).Waves,
                TierCombat.For(DifficultyTier.Elite).Waves));
            Assert.IsFalse(TierCombat.NextWaveDue(2, 6, 1), "two of six is not half");
            Assert.IsTrue(TierCombat.NextWaveDue(3, 6, 1), "three of six is");
            Assert.IsTrue(TierCombat.NextWaveDue(1, 1, 1), "a lone enemy brings the next wave when it falls");
            Assert.IsFalse(TierCombat.NextWaveDue(6, 6, 0), "no waves left");
            StringAssert.Contains("TreadShredWaves.EnemyDestroyed();\n        Victory();", Script("ManagerScore/ManagerScore.cs"), "waves before the count drops");
            foreach (var killer in new[] { "Emnemy/EmnemyRed.cs", "Bullet/Bullet3.cs", "Bullet/BulletVong.cs", "Bullet/BulletFind.cs" })
                StringAssert.DoesNotContain("ManagerScore.instance.Victory();", Script(killer).Replace("//", "#"), killer + " counts kills through EnemyKilled");
            StringAssert.Contains("TreadShredWaves.Begin(this);", Script("ManagerScore/ManagerScore.cs"));
        }

        [Test(Description = "D-046-AC-02: Veteran and Elite enemies spot the tank from 1.5x as far and their shells fly 1.3x as fast; Recruit is unchanged")]
        public void SightAndShellSpeed()
        {
            foreach (var tier in new[] { DifficultyTier.Veteran, DifficultyTier.Elite })
                Assert.AreEqual((1.5f, 1.3f), (TierCombat.For(tier).SightRange, TierCombat.For(tier).ShellSpeed), tier.ToString());
            Assert.AreEqual((1f, 1f), (TierCombat.For(DifficultyTier.Recruit).SightRange, TierCombat.For(DifficultyTier.Recruit).ShellSpeed));
            StringAssert.Contains("viewRadius *= TreadShred.Progression.TierCombat.For(TreadShredProgress.Ensure().SelectedTier).SightRange;",
                Script("Player/FieldOfView.cs").Replace("\r\n", "\n"));
            StringAssert.Contains("var speed = 15f * combat.ShellSpeed;", Script("Emnemy/BulletEmnemy.cs"));
        }

        [Test(Description = "D-046-AC-03: only Elite enemies lead their shots, aiming where a moving tank will be when the shell arrives")]
        public void EliteLeadsItsShots()
        {
            Assert.AreEqual((false, false, true), (TierCombat.For(DifficultyTier.Recruit).LeadsShots,
                TierCombat.For(DifficultyTier.Veteran).LeadsShots, TierCombat.For(DifficultyTier.Elite).LeadsShots));
            Assert.AreEqual((0f, 20f), TierCombat.Lead(0f, 0f, 0f, 20f, 0f, 0f, 20f), "a tank standing still is aimed at directly");
            var ahead = TierCombat.Lead(0f, 0f, 0f, 20f, 5f, 0f, 20f);
            Assert.Greater(ahead.x, 4.5f, "about a second of flight ahead of a tank moving sideways at 5");
            var flight = (float)System.Math.Sqrt(ahead.x * ahead.x + ahead.z * ahead.z) / 20f;
            Assert.AreEqual(5f * flight, ahead.x, 0.3f, "the shell and the tank arrive together");
        }

        [Test(Description = "D-046-AC-06: a one-hit target takes the tier's enemy health, 2 hits on Veteran and 3 on Elite, firepower cutting it as for every enemy, and every shell that can destroy a target checks it first")]
        public void TargetsTakeTierHealth()
        {
            int Hits(float health, float firepower) => (TierCombat.TargetHealth(health) + TierCombat.ShellDamage(firepower) - 1) / TierCombat.ShellDamage(firepower);
            Assert.AreEqual((1, 2, 3), (Hits(1f, 1f), Hits(2f, 1f), Hits(3f, 1f)), "Recruit, Veteran, Elite with base firepower");
            Assert.AreEqual((1, 2), (Hits(2f, 2f), Hits(3f, 2f)), "x2.0 firepower halves it");
            foreach (var shell in new[] { "Bullet/Bullet3.cs", "Bullet/BulletVong.cs", "Bullet/BulletFind.cs" })
                StringAssert.Contains("TreadShredTargetHealth.Survives(other.gameObject)", Script(shell), shell);
        }

        [Test(Description = "D-046-AC-04: each Veteran attack is a volley of 3 shells and each Elite one of 4, 0.25 s apart, cycling cannon, triple shot, arc shot and seeker; the enemy shells load for every map")]
        public void VolleysCycleEveryWeapon()
        {
            Assert.AreEqual((0, 3, 4), (TierCombat.For(DifficultyTier.Recruit).VolleyShots, TierCombat.For(DifficultyTier.Veteran).VolleyShots,
                TierCombat.For(DifficultyTier.Elite).VolleyShots));
            Assert.AreEqual(0.25f, TierCombat.VolleyGap);
            CollectionAssert.AreEqual(
                new[] { EnemyWeapon.Cannon, EnemyWeapon.TripleShot, EnemyWeapon.ArcShot, EnemyWeapon.Seeker, EnemyWeapon.Cannon },
                Enumerable.Range(0, 5).Select(TierCombat.VolleyWeapon));
            foreach (var shell in new[] { "BulletEmnemy", "BulletRed", "BulletFindPlayer" })
                Assert.IsNotNull(Resources.Load<GameObject>("EnemyShells/" + shell), shell + " loads on any map");
            var fire = Script("Emnemy/BulletEmnemy.cs");
            StringAssert.Contains("Fire(TierCombat.VolleyWeapon(volleyShot++), combat);", fire);
            StringAssert.Contains("TreadShredEnemyShells.Fired(shell);", fire, "a revive clears volley shells too (D-020-AC-07)");
        }
    }
}
