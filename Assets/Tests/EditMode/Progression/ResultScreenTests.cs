using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ResultScreenTests
    {
        private static string Results() =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", "ManagerScore/ManagerScore.cs"));

        [Test(Description = "D-032-AC-02: the result screen shows the player's rank on its LEADERBOARD button as soon as the platform reports it")]
        public void RankRidesOnTheLeaderboardButton()
        {
            Assert.AreEqual("LEADERBOARD #1", LeaderboardRules.ButtonLabel(1));
            Assert.AreEqual("LEADERBOARD #1,284", LeaderboardRules.ButtonLabel(1284));
            Assert.AreEqual("LEADERBOARD", LeaderboardRules.ButtonLabel(0), "no rank reported yet");
            StringAssert.Contains("TreadShredUi.SetLabel(board, LeaderboardRules.ButtonLabel(rank));", Results());
        }

        [Test(Description = "D-013-AC-02: the original game's medal disc no longer shows half hidden behind the result screen's offer")]
        public void LegacyMedalStaysHidden()
        {
            StringAssert.Contains("Medel.SetActive(false);", Results());
        }
    }
}
