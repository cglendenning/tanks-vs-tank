using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ArmorHitTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-017-AC-09: every enemy shell takes exactly one hit of the player's armor, the lob shell included and only once, and the HUD shields clear on destruction and refill on a revive")]
        public void EveryShellTakesOneHit()
        {
            var bullets = Directory.GetFiles(Path.Combine(Application.dataPath, "Script", "Bullet"), "*.cs");
            var hits = bullets.SelectMany(f => Regex.Matches(File.ReadAllText(f), @"^(?!\s*//).*PlayerMoverment\.instance\.DeadPlayer\((\d+)\)", RegexOptions.Multiline)
                .Cast<Match>().Select(m => (File: Path.GetFileName(f), Damage: m.Groups[1].Value))).ToList();
            CollectionAssert.IsNotEmpty(hits);
            foreach (var hit in hits)
                Assert.AreEqual("1", hit.Damage, hit.File + " takes one hit");
            Assert.IsTrue(hits.Any(h => h.File == "BulletRed.cs"), "the enemy lob shell");
            StringAssert.Contains("if (collision.gameObject.tag == \"player\" && !hitPlayer)", Script("Bullet/BulletRed.cs"), "a shell hits once");

            var player = Script("Player/PlayerMoverment.cs");
            var fade = player.Substring(player.IndexOf("IEnumerator DelayHeath()", System.StringComparison.Ordinal));
            StringAssert.DoesNotContain("new WaitForSeconds(", fade, "the revive panel's pause cannot freeze a fade");
            StringAssert.Contains("SetShields(0.15f);\n            // D-020: offer a revive before the tank is lost.", player);
            var revive = player.Substring(player.IndexOf("public void Revive()", System.StringComparison.Ordinal));
            revive = revive.Substring(0, revive.IndexOf("SetShields(1f);", System.StringComparison.Ordinal));
            StringAssert.Contains("StopCoroutine(shieldFade);", revive, "a revive stops any fade before refilling");
        }
    }
}
