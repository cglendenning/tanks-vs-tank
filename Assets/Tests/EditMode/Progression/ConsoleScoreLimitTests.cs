using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ConsoleScoreLimitTests
    {
        [Test(Description = "D-032-AC-04: the App Store Connect leaderboard is configured with the game's own score ceiling and ID")]
        public void AppStoreConnectLimitMatchesTheGame()
        {
            var root = Path.GetDirectoryName(Application.dataPath);
            var setup = File.ReadAllText(Path.Combine(root, "scripts/app-store-connect.py"));
            var rangeEnd = Regex.Match(setup, "\"scoreRangeEnd\":\\s*\"(\\d+)\"");
            Assert.IsTrue(rangeEnd.Success, "the setup script sets an upper score limit");
            Assert.AreEqual(ScoreRules.MaxRunScore, int.Parse(rangeEnd.Groups[1].Value));
            StringAssert.Contains("\"scoreRangeStart\": \"0\"", setup);

            var bridge = File.ReadAllText(Path.Combine(Application.dataPath, "Script/Game/TreadShredLeaderboard.cs"));
            var id = Regex.Match(setup, "\"vendorIdentifier\":\\s*\"([^\"]+)\"").Groups[1].Value;
            StringAssert.Contains("GameCenterId = \"" + id + "\"", bridge, "the game submits to the board the script creates");
        }
    }
}
