using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // GO PUBLIC AND COMPETE first shows the two taps, with pictures of Google's screens, then opens Play Games.
    public sealed class ProfileHelpDialogTests
    {
        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-032-AC-02: the how-to dialog numbers the taps in Play Games' own words and offers OPEN PLAY GAMES and NOT NOW")]
        public void StepsUseGooglesWords()
        {
            StringAssert.Contains("PRIVATE", LeaderboardRules.ProfileHelpIntro);
            Assert.AreEqual("1. TAP PRIVACY & SETTINGS", LeaderboardRules.ProfileHelpStepOne);
            Assert.AreEqual("2. TURN OFF MAKE PROFILE PRIVATE", LeaderboardRules.ProfileHelpStepTwo);
            StringAssert.StartsWith("3. COME BACK HERE", LeaderboardRules.ProfileHelpStepThree);
            Assert.AreEqual("OPEN PLAY GAMES", LeaderboardRules.OpenPlayGames);
            Assert.AreEqual("NOT NOW", LeaderboardRules.NotNow);
        }

        [Test(Description = "D-032-AC-02: both step pictures ship as full-quality PNGs, wide enough to read on a phone")]
        public void StepPicturesDecode()
        {
            foreach (var name in new[] { "step-1-privacy-settings", "step-2-make-profile-private" })
            {
                var path = Path.Combine(Application.dataPath, "Resources", "ProfileHelp", name + ".bytes");
                var texture = new Texture2D(2, 2);
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), name);
                Assert.AreEqual(720, texture.width, name);
                Assert.That(texture.height, Is.InRange(120, 240), name);
                Object.DestroyImmediate(texture);
            }
        }

        [Test(Description = "D-032-AC-02: GO PUBLIC AND COMPETE opens the dialog, whose OPEN PLAY GAMES closes it only when Play Games opened")]
        public void ButtonOpensDialogFirst()
        {
            StringAssert.Contains("() => TreadShredProfileHelp.Show(font, OpenProfileSettings)", Read("Script/Game/TreadShredLeaderboardPanel.cs"));
            var dialog = Read("Script/Game/TreadShredProfileHelp.cs");
            StringAssert.Contains("if (openSettings())\n            Close();\n        else\n            fallback.text = LeaderboardRules.MakeProfilePublic;", dialog);
            StringAssert.Contains("TreadShredBack.Opened(this, Close)", dialog, "the system Back closes it");
        }
    }
}
