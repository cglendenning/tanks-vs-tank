using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Craig's review of the first-minute preview: the opening needs cues; since 2026-11-20 the enemies also fight
    // back from the first second (players cleared a fight they could not lose, then quit).
    public sealed class FirstMinuteCombatTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-035-AC-03: mission 1's first play is a real fight: enemies fire at their normal rate with follow-up shells, and a hit can take the last armor")]
        public void FirstPlayIsARealFight()
        {
            var gun = Read("Script/Emnemy/BulletEmnemy.cs");
            StringAssert.DoesNotContain("TreadShredFirstMinute", gun, "enemy guns fire as on any mission");
            var player = Read("Script/Player/PlayerMoverment.cs");
            StringAssert.Contains("countdie -= index;", player, "every hit costs its armor");
            StringAssert.DoesNotContain("ArmorAfterHit", player);
        }

        [Test(Description = "D-035-AC-03: the first play opens with GET READY over the countdown and MOVE WITH YOUR THUMB by the joystick")]
        public void OpeningCues()
        {
            Assert.AreEqual("GET READY", FirstMinute.GetReady);
            Assert.AreEqual("MOVE WITH YOUR THUMB", FirstMinute.MoveHint);
            StringAssert.Contains("TreadShredFirstMinuteCues.Show(font, joystick);", Read("Script/Player/PlayerMoverment.cs"));
            var cues = Read("Script/Game/TreadShredFirstMinuteCues.cs");
            StringAssert.Contains("CreateOverlay(\"TreadShredGetReady\", hudOrder - 1, receivesTaps: false)", cues, "behind the countdown numerals, never blocking a tap");
            StringAssert.Contains("if (card != null && Time.timeScale > 0f)", cues, "the card lifts when the countdown ends");
        }
    }
}
