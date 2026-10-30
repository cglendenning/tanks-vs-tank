using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class LeaderboardScreenTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        private static LeaderboardEntry[] TopRows(int count, int playerRank = 0)
        {
            var rows = new LeaderboardEntry[count];
            for (var i = 0; i < count; i++)
                rows[i] = new LeaderboardEntry(i + 1, "player " + (i + 1), 50000 - i * 100, i + 1 == playerRank);
            return rows;
        }

        [Test(Description = "D-032-AC-02: the screen lists the top 25 and adds the player's own row when they rank below it")]
        public void PlayerRowIsShownBelowTheTopWhenNeeded()
        {
            Assert.AreEqual(25, LeaderboardPage.TopCount);
            var below = new LeaderboardPage(TopRows(25), new LeaderboardEntry(1284, "me", 23400, true));
            Assert.IsTrue(below.PlayerBelowTop);

            var inside = new LeaderboardPage(TopRows(25, playerRank: 7), new LeaderboardEntry(7, "me", 49400, true));
            Assert.IsFalse(inside.PlayerBelowTop, "a player already in the top 25 is highlighted in place");
            Assert.IsFalse(new LeaderboardPage(TopRows(3), null).PlayerBelowTop, "no rank, no extra row");
        }

        [Test(Description = "D-032-AC-02: rows show rank, the platform nickname in capitals cut to fit, and the score")]
        public void RowsReadClearly()
        {
            var entry = new LeaderboardEntry(1284, "  averyveryverylongnickname ", 154000, false);
            Assert.AreEqual("#1,284", LeaderboardPage.RankText(entry));
            Assert.AreEqual("154,000", LeaderboardPage.ScoreText(entry));
            Assert.AreEqual("AVERYVERYVERYLONGN", LeaderboardPage.NameText(entry));
            Assert.AreEqual("UNKNOWN", LeaderboardPage.NameText(new LeaderboardEntry(1, " ", 0, false)));
        }

        [Test(Description = "D-032-AC-02: LEADERBOARD opens the screen from the start and result screens; BACK closes it; it loads after the latest submission")]
        public void ScreenIsReachableAndClosesWithBack()
        {
            StringAssert.Contains("TreadShredLeaderboardPanel.OpenFrom(", Script("LoadLv.cs"));
            StringAssert.Contains("TreadShredLeaderboardPanel.OpenFrom(", Script("ManagerScore/ManagerScore.cs"));
            var panel = Script("Game/TreadShredLeaderboardPanel.cs");
            StringAssert.Contains("LeaderboardRules.Back", panel);
            StringAssert.Contains("Destroy(gameObject)", panel);
            StringAssert.Contains("afterSubmitTop.Add(onPage)", Script("Game/TreadShredLeaderboard.cs"),
                "a page requested during a submission waits for it");
            Assert.AreEqual("BACK", LeaderboardRules.Back);
        }
    }
}
