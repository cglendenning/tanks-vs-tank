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

        [Test(Description = "D-032-AC-02: the result screen shows SCORE and the HIGH SCORE the leaderboard ranks")]
        public void ResultScreenShowsTheRankedTotal()
        {
            Assert.AreEqual("SCORE: 1,100", ScoreRules.ScoreLine(1100));
            Assert.AreEqual("HIGH SCORE: 2,195", ScoreRules.HighScoreLine(2195, false));
            Assert.AreEqual("NEW HIGH SCORE: 221,400", ScoreRules.HighScoreLine(221400, true));
            var score = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "Script/ManagerScore/ManagerScore.cs"));
            StringAssert.Contains("ScoreRules.HighScoreLine(TreadShredScores.HighScore, newHighScore)", score);
        }
    }
}
