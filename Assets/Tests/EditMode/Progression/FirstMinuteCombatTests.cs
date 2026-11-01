using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Craig's review of the first-minute preview: enemies must be seen to fire, sparingly, and the opening needs cues.
    public sealed class FirstMinuteCombatTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-035-AC-03: on a first play the enemies fire one shell between them at most every 4 seconds")]
        public void EnemiesFireSparingly()
        {
            Assert.AreEqual(4f, FirstMinute.EnemyShotIntervalSeconds);
            Assert.IsFalse(FirstMinute.EnemyMayFire(now: 13.9f, lastEnemyShot: 10f));
            Assert.IsTrue(FirstMinute.EnemyMayFire(now: 14f, lastEnemyShot: 10f));
            var gun = Read("Script/Emnemy/BulletEmnemy.cs");
            StringAssert.Contains("TreadShredFirstMinute.EnemyFired();", gun, "each shell starts the interval again");
            StringAssert.Contains("if (TreadShredFirstMinute.FirstPlay)\r\n           yield break;", gun, "no follow-up shell");
            StringAssert.Contains("lastEnemyShot = Time.time;", Read("Script/Game/TreadShredFirstMinute.cs"), "nothing fires until one interval into play");
        }

        [Test(Description = "D-035-AC-03: on a first play a hit costs armor but never the last, so the first play cannot be lost")]
        public void FirstPlayCannotBeLost()
        {
            Assert.AreEqual(2, FirstMinute.ArmorAfterHit(armor: 3, damage: 1, firstPlay: true), "a hit still costs armor");
            Assert.AreEqual(1, FirstMinute.ArmorAfterHit(armor: 1, damage: 1, firstPlay: true));
            Assert.AreEqual(1, FirstMinute.ArmorAfterHit(armor: 2, damage: 5, firstPlay: true));
            Assert.AreEqual(0, FirstMinute.ArmorAfterHit(armor: 1, damage: 1, firstPlay: false), "every other mission can be lost");
            StringAssert.Contains("countdie = TreadShred.Progression.FirstMinute.ArmorAfterHit(countdie, index, TreadShredFirstMinute.FirstPlay);",
                Read("Script/Player/PlayerMoverment.cs"));
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
