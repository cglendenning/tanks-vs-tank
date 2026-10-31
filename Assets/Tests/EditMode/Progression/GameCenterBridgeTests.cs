using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class GameCenterBridgeTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-032-AC-03: tapping LEADERBOARD while signed out offers Apple's sign-in or says where to sign in, and never restarts sign-in")]
        public void SignedOutTapOffersSignInWithoutFreezing()
        {
            var bridge = Read("Script/Game/TreadShredLeaderboard.cs");
            StringAssert.DoesNotContain("Time.timeScale", bridge);
            StringAssert.DoesNotContain("UnityPause", Read("Plugins/iOS/TreadShredGameCenter.m"));

            Assert.AreEqual("SIGN IN TO GAME CENTER IN SETTINGS", LeaderboardRules.SignInFromSettings);
            StringAssert.Contains("LeaderboardRules.SignInFromSettings", Read("Script/Game/TreadShredLeaderboardPanel.cs"));
        }
    }
}
