using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Fixes from the 2026-11-04 PlaytestCloud session (spec playtests/2026-11-01-first-session/RESULTS-taj.md).
    public sealed class PlaytestPolishTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-027-AC-04: the victory music plays once when the victory screen opens and never loops")]
        public void VictoryMusicPlaysOnce()
        {
            var music = Script("Music/MusicManager.cs");
            var victory = music.Substring(music.IndexOf("public void Victory()", System.StringComparison.Ordinal));
            victory = victory.Substring(0, victory.IndexOf("\n    }", System.StringComparison.Ordinal));
            StringAssert.Contains("source.loop = false;", victory);
            StringAssert.DoesNotContain("loop = true", victory);
        }

        [Test(Description = "D-009-AC-04: after a victory NEXT MISSION's label carries the solid accent style of the primary buy buttons")]
        public void NextMissionIsThePrimaryControl()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("BuildVictoryRewardActions();\n            HighlightNextMission();", results);
            StringAssert.Contains("VictoryObj.transform.Find(\"ButtonNext/LabelPlate\")", results);
            StringAssert.Contains("image.color = TreadShredUi.PrimaryFill;", results);
            StringAssert.Contains("button.targetGraphic.color = PrimaryFill;", Script("TreadShredUi.cs"), "the same fill as the primary buy buttons");
        }

        [Test(Description = "D-035-AC-06: the victory screen after the first play's free Overdrive offers it back as KEEP OVERDRIVE, with the same effect and price")]
        public void FirstPlayVictoryOffersToKeepOverdrive()
        {
            Assert.AreEqual("KEEP OVERDRIVE", OfferLabels.KeepOverdrive);
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("TreadShredFirstMinute.FirstPlay ? OfferLabels.KeepOverdrive : \"OVERDRIVE\",\n                OfferLabels.OverdriveEffect,", results);
        }
    }
}
