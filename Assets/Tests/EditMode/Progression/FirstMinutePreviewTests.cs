using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Craig's review: the preview opened mission 1 on every launch, where a new player gets it once.
    public sealed class FirstMinutePreviewTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-035-AC-04: a preview build plays a first-time install: the instant start once per install, the first play until mission 1 is cleared in the preview")]
        public void PreviewPlaysAFirstTimeInstall()
        {
            var rules = Read("Script/Game/TreadShredFirstMinute.cs");
            StringAssert.Contains("if (!FirstMinute.InstantStart(MissionOneCleared, PlayerPrefs.GetInt(InstantStartUsedKey, 0) == 1))", rules,
                "the preview obeys the once-per-install rule like every build");
            StringAssert.Contains("Preview\n        ? PlayerPrefs.GetInt(PreviewMissionOneClearedKey, 0) == 1", rules.Replace("\r\n", "\n"),
                "it stands in for the player's mission 1 clear with its own install-local record");
            StringAssert.Contains("firstPlay = Applies && FirstMinute.IsFirstPlay(mission, TreadShredProgress.Ensure().SelectedTier, MissionOneCleared);", rules);
            StringAssert.Contains("TreadShredFirstMinute.MissionWon(TreadShredProgress.CurrentMission());", Read("Script/ManagerScore/ManagerScore.cs"),
                "winning mission 1 in the preview ends its first minute, as a real clear does");
            StringAssert.DoesNotContain("mission == FirstMinute.Mission\n", rules.Replace("\r\n", "\n"), "no preview path that ignores the rules");
        }
    }
}
