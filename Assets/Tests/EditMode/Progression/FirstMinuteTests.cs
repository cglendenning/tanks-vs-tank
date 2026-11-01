using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // A first-time player's first minute: straight into mission 1, then a guaranteed power moment.
    public sealed class FirstMinuteTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-035-AC-01: the first launch opens in mission 1 only for a first-time player, once per install")]
        public void InstantStartOnlyForFirstTimePlayers()
        {
            Assert.IsTrue(FirstMinute.InstantStart(missionOneCleared: false, instantStartUsed: false));
            Assert.IsFalse(FirstMinute.InstantStart(missionOneCleared: true, instantStartUsed: false), "a returning player's progress shows mission 1 cleared");
            Assert.IsFalse(FirstMinute.InstantStart(missionOneCleared: false, instantStartUsed: true), "once per install");
            Assert.AreEqual(1, FirstMinute.Mission);
        }

        [Test(Description = "D-035-AC-03: the first-minute rules cover mission 1 on Recruit only, until mission 1 is first cleared")]
        public void FirstPlayIsMissionOneOnRecruitBeforeItsFirstClear()
        {
            Assert.IsTrue(FirstMinute.IsFirstPlay(1, DifficultyTier.Recruit, missionOneCleared: false));
            Assert.IsFalse(FirstMinute.IsFirstPlay(1, DifficultyTier.Recruit, missionOneCleared: true), "after the first clear, mission 1 plays normally");
            Assert.IsFalse(FirstMinute.IsFirstPlay(2, DifficultyTier.Recruit, missionOneCleared: false), "enemy fire begins in mission 2");
            Assert.IsFalse(FirstMinute.IsFirstPlay(1, DifficultyTier.Veteran, missionOneCleared: false));
            Assert.AreEqual("TAP HERE TO FIRE", FirstMinute.FireHint);
        }

        [Test(Description = "D-035-AC-01: the start screen asks for the instant start, which skips capture runs and the editor and happens once per launch")]
        public void StartScreenRoutesFirstTimePlayers()
        {
            var start = Read("Script/LoadLv.cs");
            StringAssert.Contains("if (TreadShredFirstMinute.TakeInstantStart())", start);
            StringAssert.Contains("Application.LoadLevel(\"Lv\" + TreadShred.Progression.FirstMinute.Mission);", start);
            var rules = Read("Script/Game/TreadShredFirstMinute.cs");
            StringAssert.Contains("!TreadShredStoreCaptureMode.IsInstalled && (!Application.isEditor || editorPreview)", rules);
            StringAssert.Contains("if (launchChecked || !Applies)", rules);
            StringAssert.Contains("PlayerPrefs.SetInt(InstantStartUsedKey, 1);", rules);
        }

        [Test(Description = "D-035-AC-03: the first play gets Overdrive free for that mission only, the fire hint, and enemies that hold fire")]
        public void FirstPlayWiring()
        {
            var player = Read("Script/Player/PlayerMoverment.cs");
            StringAssert.Contains("var firstPlay = TreadShredFirstMinute.BeginMission(TreadShredProgress.CurrentMission());", player);
            StringAssert.Contains("overdriveActive = loadout.RoundsPerTap > 1 || firstPlay;", player, "free for this mission: nothing is armed for later ones");
            StringAssert.Contains("TreadShredFireHint.Show(", player);
            StringAssert.Contains("if (TreadShredFirstMinute.EnemiesHoldFire)\r\n            return;", Read("Script/Emnemy/BulletEmnemy.cs"));
            StringAssert.DoesNotContain("ArmOverdrive", Read("Script/Game/TreadShredFirstMinute.cs"), "the purchased Overdrive is untouched");
        }
    }
}
