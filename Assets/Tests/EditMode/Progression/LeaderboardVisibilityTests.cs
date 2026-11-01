using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // D-032-AC-02: Google Play Games keeps private profiles off public leaderboards and reports their own entry with
    // rank -1, so the screen must still show the player's best and say why it is not on the board.
    public sealed class LeaderboardVisibilityTests
    {
        private static List<LeaderboardEntry> Others(int count, long topScore = 90000, long step = 1000)
        {
            var rows = new List<LeaderboardEntry>();
            for (var i = 0; i < count; i++)
                rows.Add(new LeaderboardEntry(i + 1, "rival" + i, topScore - i * step, false));
            return rows;
        }

        private static LeaderboardEntry Me(long score, int rank = 0) => new LeaderboardEntry(rank, "GildedRebellion146", score, true);

        [Test(Description = "D-032-AC-02: a private profile's score, left off a board with room for it, is shown as YOU and flagged as hidden")]
        public void PrivateProfileScoreIsShownAndFlagged()
        {
            var page = new LeaderboardPage(new List<LeaderboardEntry>(), Me(3800));

            Assert.IsTrue(page.PlayerHidden);
            Assert.IsTrue(page.ShowsPlayerRow, "the player's best is shown instead of NO SCORES YET");
            Assert.AreEqual("YOU", LeaderboardPage.RankText(page.Player.Value));
            Assert.AreEqual("3,800", LeaderboardPage.ScoreText(page.Player.Value));
            Assert.IsTrue(new LeaderboardPage(Others(25, topScore: 3000, step: 10), Me(3800)).PlayerHidden,
                "a full board whose lowest score is below the player's would have listed them");
        }

        [Test(Description = "D-032-AC-02: a public player listed on the board is highlighted in place, with no extra row or note")]
        public void ListedPlayerNeedsNoExtraRow()
        {
            var rows = Others(3);
            rows.Add(new LeaderboardEntry(4, "GildedRebellion146", 3800, true));
            var page = new LeaderboardPage(rows, Me(3800));

            Assert.IsFalse(page.PlayerHidden);
            Assert.IsFalse(page.ShowsPlayerRow);
        }

        [Test(Description = "D-032-AC-02: a player below a full top 25 without a rank still sees their best, and is not called private")]
        public void BelowFullBoardIsNotPrivate()
        {
            var page = new LeaderboardPage(Others(25, topScore: 90000, step: 1000), Me(1200));

            Assert.IsFalse(page.PlayerHidden, "their score is simply below the 25th");
            Assert.IsTrue(page.ShowsPlayerRow);
            Assert.IsFalse(new LeaderboardPage(Others(3), null).ShowsPlayerRow, "no score, no row");
        }

        [Test(Description = "D-032-AC-02: the plugin keeps unranked own scores, matches the player's row by ID, and only Android shows the private-profile note")]
        public void BridgeAndScreenHandleUnrankedScores()
        {
            string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));
            StringAssert.Contains("top.player.score > 0", Read("Script/Game/TreadShredLeaderboard.cs"));
            StringAssert.Contains("holder.getPlayerId().equals(self.getPlayerId())", Read("Plugins/Android/TreadShredPlayGames.java"));
            var panel = Read("Script/Game/TreadShredLeaderboardPanel.cs");
            StringAssert.Contains("page.Top.Count == 0 && !page.ShowsPlayerRow", panel, "NO SCORES YET only without a score of the player's own");
            StringAssert.Contains("page.PlayerHidden && Application.platform == RuntimePlatform.Android", panel);
            StringAssert.Contains("PLAY GAMES PROFILE > PUBLIC", LeaderboardRules.MakeProfilePublic);
        }
    }
}
