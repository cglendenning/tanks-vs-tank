using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // The Android half of D-025 and D-032: the Play Games plugin and its wiring.
    public sealed class PlayGamesTests
    {
        private static string Asset(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));
        private static string Plugin() => Asset("Plugins/Android/TreadShredPlayGames.java");

        [Test(Description = "D-032-AC-03: Android signs in quietly through Play Games and offers its sign-in only when LEADERBOARD is tapped")]
        public void QuietSignInOfferedOnlyFromLeaderboard()
        {
            var plugin = Plugin();
            StringAssert.Contains("PlayGamesSdk.initialize(", plugin);
            StringAssert.Contains(".isAuthenticated()", plugin);
            StringAssert.Contains("getGamesSignInClient(activity).signIn()", Regex.Match(plugin, @"public static boolean offerSignIn\(\)[\s\S]*?\n    }").Value);
            var bridge = Asset("Script/Game/TreadShredLeaderboard.cs");
            StringAssert.Contains("Application.platform == RuntimePlatform.Android", bridge);
            StringAssert.Contains("TreadShredGameCenterOfferSignIn() => TreadShredPlayGames.OfferSignIn() ? 1 : 0;", bridge);
        }

        [Test(Description = "D-032-AC-02: Android's leaderboard screen reads Play Games' all-time public top entries and the player's own rank")]
        public void LeaderboardReadsPlayGames()
        {
            var plugin = Plugin();
            StringAssert.Contains("loadTopScores(board, LeaderboardVariant.TIME_SPAN_ALL_TIME, LeaderboardVariant.COLLECTION_PUBLIC, count)", plugin);
            StringAssert.Contains("loadCurrentPlayerLeaderboardScore(board, LeaderboardVariant.TIME_SPAN_ALL_TIME, LeaderboardVariant.COLLECTION_PUBLIC)", plugin);
            StringAssert.Contains("submitScoreImmediate(board, score)", plugin);
            StringAssert.Contains("LEADERBOARD_RECEIVER = \"TreadShredGameCenter\"", plugin, "answers the same receiver the iOS plugin does");
            foreach (var message in new[] { "\"OnSignIn\"", "\"OnSubmitted\"", "\"OnRank\"", "\"OnTop\"" })
                StringAssert.Contains(message, plugin);
        }

        [Test(Description = "D-025-AC-01: Android saves progress to Play Games saved games, and the app is tied to its Play Games project")]
        public void ProgressSavesToPlayGames()
        {
            var plugin = Plugin();
            StringAssert.Contains("commitAndClose(snapshot", plugin);
            StringAssert.Contains("CLOUD_RECEIVER = \"TreadShredCloudSave\"", plugin);
            StringAssert.Contains("TreadShredPlayGames.WriteSave(JsonUtility.ToJson(snapshot))", Asset("Script/Game/TreadShredCloudSave.cs"));
            StringAssert.Contains("com.google.android.gms.games.APP_ID", Asset("Plugins/Android/TreadShredPlayGames.androidlib/AndroidManifest.xml"));
            StringAssert.Contains("com.google.android.gms:play-services-games-v2:22.1.0", Asset("Editor/TreadShredPlayGamesDependencies.xml"));
            StringAssert.Contains("implementation 'com.google.android.gms:play-services-games-v2:22.1.0'", Asset("Plugins/Android/mainTemplate.gradle"));
        }

        [Test(Description = "D-025-AC-03: when two devices wrote the Android saved game, Play Games keeps the most recently written one")]
        public void MostRecentSavedGameWins()
        {
            StringAssert.Contains("SnapshotsClient.RESOLUTION_POLICY_MOST_RECENTLY_MODIFIED", Plugin());
        }

        [Test(Description = "D-032-AC-04: the Play Console leaderboard is configured with the game's own score ceiling")]
        public void PlayConsoleLimitMatchesTheGame()
        {
            var setup = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "scripts/play-console.py"));
            var max = Regex.Match(setup, "\"scoreMax\":\\s*\"(\\d+)\"");
            Assert.IsTrue(max.Success, "the setup script sets an upper score limit");
            Assert.AreEqual(ScoreRules.MaxRunScore, int.Parse(max.Groups[1].Value));
            StringAssert.Contains("\"scoreMin\": \"0\"", setup);
            StringAssert.Contains("\"scoreOrder\": \"LARGER_IS_BETTER\"", setup);
        }
    }
}
