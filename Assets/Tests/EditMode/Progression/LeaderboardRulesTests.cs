using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class LeaderboardRulesTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-032-AC-01: HIGH SCORE is the run's best SCORE, and is submitted only when it rises")]
        public void TotalScoreCountsEveryTierAndSubmitsOnlyWhenHigher()
        {
            Assert.IsTrue(LeaderboardRules.ShouldSubmit(5200, 5100));
            Assert.IsFalse(LeaderboardRules.ShouldSubmit(5100, 5100), "an unchanged HIGH SCORE is not resubmitted");
            Assert.IsFalse(LeaderboardRules.ShouldSubmit(4000, 5100));
            Assert.IsFalse(LeaderboardRules.ShouldSubmit(0, 0));

            var score = Script("ManagerScore/ManagerScore.cs");
            var record = score.IndexOf("TreadShredScores.RecordScore(lastMission.RunScore)");
            Assert.Greater(record, 0, "each finished mission checks the run's SCORE against HIGH SCORE");
            Assert.Greater(score.IndexOf("TreadShredLeaderboard.SubmitIfRaised();", record), record, "and submits after recording");
            StringAssert.Contains("TreadShredScores.HighScore", Script("LoadLv.cs"), "the stats screen shows HIGH SCORE");
        }

        [Test(Description = "D-032-AC-02: the start screen has a LEADERBOARD button and the result screen shows the player's rank")]
        public void LeaderboardButtonAndRankLine()
        {
            Assert.AreEqual("LEADERBOARD RANK // #1,284", LeaderboardRules.RankLine(1284));
            Assert.AreEqual("LEADERBOARD RANK // #7", LeaderboardRules.RankLine(7));
            StringAssert.Contains("CreateButton(\"LEADERBOARD\"", Script("LoadLv.cs"));
            StringAssert.Contains("TreadShredLeaderboardPanel.OpenFrom(", Script("LoadLv.cs"));
            StringAssert.Contains("TreadShredLeaderboard.RequestRank(", Script("ManagerScore/ManagerScore.cs"));
        }

        [Test(Description = "D-032-AC-03: signing in is offered, never required: nothing is submitted or loaded while signed out")]
        public void SignInIsOptional()
        {
            var bridge = Script("Game/TreadShredLeaderboard.cs");
            StringAssert.Contains("if (!Available || !SignedIn || submitting)", bridge);
            StringAssert.Contains("if (!Available || !SignedIn)", bridge);
            var open = bridge.Substring(bridge.IndexOf("public static LeaderboardOpenResult Open()"));
            StringAssert.Contains("TreadShredGameCenterOfferSignIn()", open, "the button offers sign-in");
            Assert.IsFalse(bridge.Contains("Time.timeScale"), "the leaderboard never pauses play");
        }

        [Test(Description = "D-032-AC-04: no score above what a run through three tiers of 40 missions can produce is submitted")]
        public void ScoreCeilingMatchesWhatTheGameCanProduce()
        {
            Assert.AreEqual(3, ScoreRules.TierCount);
            Assert.AreEqual(250800, ScoreRules.MaxRunScore);
            Assert.IsTrue(LeaderboardRules.ShouldSubmit(250800, 0));
            Assert.IsFalse(LeaderboardRules.ShouldSubmit(250801, 0));
        }

        [Test(Description = "D-032-AC-05: the leaderboard code inherited from the original game is gone")]
        public void InheritedLeaderboardIsRemoved()
        {
            var scripts = Directory.GetFiles(Path.Combine(Application.dataPath, "Script"), "*.cs", SearchOption.AllDirectories);
            Assert.IsFalse(scripts.Any(f => Path.GetFileName(f) == "LeaderboardController.cs"));
            foreach (var file in scripts)
                StringAssert.DoesNotContain("Tank_Heroes", File.ReadAllText(file), Path.GetFileName(file));
        }
    }
}
