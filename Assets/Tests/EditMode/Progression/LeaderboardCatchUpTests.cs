using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class LeaderboardCatchUpTests
    {
        [Test(Description = "D-032-AC-02: the leaderboard re-reads after a new score until Game Center shows it, then stops")]
        public void ReReadsUntilTheNewScoreShows()
        {
            Assert.IsTrue(LeaderboardRules.BoardBehind(1100, 2400, 1), "the board still shows the older total");
            Assert.IsFalse(LeaderboardRules.BoardBehind(2400, 2400, 1), "the new total is on the board");
            Assert.IsFalse(LeaderboardRules.BoardBehind(0, 0, 1), "nothing submitted this session: show the board as it is");
            Assert.IsFalse(LeaderboardRules.BoardBehind(1100, 2400, LeaderboardRules.CatchUpReads),
                "after the last re-read the board is shown as it is");
            Assert.AreEqual(4, LeaderboardRules.CatchUpReads);
            Assert.AreEqual(2f, LeaderboardRules.CatchUpSeconds);
        }

        [Test(Description = "D-032-AC-02: the result screen shows this mission's score and the TOTAL SCORE the leaderboard ranks")]
        public void ResultScreenShowsTheRankedTotal()
        {
            Assert.AreEqual("MISSION SCORE // 1,100", LeaderboardRules.MissionLine(1100));
            Assert.AreEqual("TOTAL SCORE // 1,100", LeaderboardRules.TotalLine(1100));
            Assert.AreEqual("TOTAL SCORE // 156,000", LeaderboardRules.TotalLine(156000));
            var score = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "Script/ManagerScore/ManagerScore.cs"));
            StringAssert.Contains("LeaderboardRules.TotalLine(TreadShredScores.TotalScore())", score);
        }
    }
}
