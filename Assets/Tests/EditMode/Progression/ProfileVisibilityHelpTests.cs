using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Play Games makes new profiles private, which keeps the player off everyone else's board. The game must take the
    // player to the setting that changes it, not only describe it.
    public sealed class ProfileVisibilityHelpTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-032-AC-02: the private-profile help offers GO PUBLIC AND COMPETE and names Play Games' controls exactly as Google labels them")]
        public void HelpNamesGooglesControls()
        {
            Assert.AreEqual("GO PUBLIC AND COMPETE", LeaderboardRules.MakePublicButton, "the label says why: to compete with other players");
            StringAssert.Contains("PRIVACY & SETTINGS", LeaderboardRules.MakePublicHint);
            StringAssert.Contains("TURN OFF MAKE PROFILE PRIVATE", LeaderboardRules.MakePublicHint);
        }

        [Test(Description = "D-032-AC-02: the button opens Play Games' settings, falls back to the Play Store profile and then to written steps, and the board reloads on return")]
        public void ButtonOpensSettingsAndReloads()
        {
            var plugin = Read("Plugins/Android/TreadShredPlayGames.java");
            StringAssert.Contains("\"com.google.android.gms.games.ui.settings.LAUNCH_GAMES_SETTINGS_V2\"", plugin);
            StringAssert.Contains("open(new Intent(GAMES_SETTINGS).setPackage(\"com.google.android.gms\"))", plugin);
            StringAssert.Contains("\"https://play.google.com/games/profile\"", plugin);
            StringAssert.Contains("Log.w(TAG, \"Screen unavailable: \" + intent, error);", plugin, "a screen that will not open is logged, never swallowed");

            StringAssert.Contains("Plugin.CallStatic<bool>(\"openProfileSettings\")", Read("Script/Game/TreadShredPlayGames.cs"));

            var panel = Read("Script/Game/TreadShredLeaderboardPanel.cs");
            StringAssert.Contains("TreadShredPlayGames.OpenProfileSettings()", panel);
            StringAssert.Contains("hint.text = LeaderboardRules.MakeProfilePublic;", panel, "written steps when no screen opens");
            StringAssert.Contains("private void OnApplicationFocus(bool focused)", panel);
            StringAssert.Contains("awaitingProfile = false;\n        Load();", panel, "the board reloads when the player returns");
        }
    }
}
