using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// The leaderboard's private-profile help appears only on Android devices, so the sweep never reaches it in the
    /// editor; this draws it directly and audits it on every screen shape.
    /// </summary>
    public sealed class PrivateProfileHelpTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [UnityTest(Description = "D-032-AC-02: the private-profile note, GO PUBLIC AND COMPETE beside BACK, and the written-steps fallback fit every screen shape without overlap")]
        public IEnumerator PrivateProfileHelpFitsEveryScreen()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return null;
            var font = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.font).First(f => f != null && f.name.Replace(" ", string.Empty).StartsWith("BlackOps"));
            var name = new string('W', LeaderboardPage.MaxNameLength);
            var rows = Enumerable.Range(1, 20).Select(rank => new LeaderboardEntry(rank, name, 90000 - rank, false)).ToList();
            var page = new LeaderboardPage(rows, new LeaderboardEntry(0, name, 1234567, true));
            Assert.IsTrue(page.PlayerHidden, "a 20-name board leaves room for the player, so the profile is hiding them");

            var panelType = GameType("TreadShredLeaderboardPanel");
            panelType.GetMethod("OpenWithPage", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { font, page });
            var panel = Object.FindFirstObjectByType(panelType);
            panelType.GetMethod("ShowPrivateProfileHelp", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);

            var findings = new List<Finding>();
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, "LEADERBOARD PRIVATE PROFILE"));
            }
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Single(b => b.GetComponentInChildren<Text>()?.text == LeaderboardRules.MakePublicButton);
            Assert.IsTrue(Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Any(b => b.GetComponentInChildren<Text>()?.text == LeaderboardRules.Back), "BACK stays on screen");

            // The button opens the how-to dialog. No Play Games screen opens in the editor, so OPEN PLAY GAMES there
            // shows the written steps.
            button.onClick.Invoke();
            yield return null;
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Single(b => b.GetComponentInChildren<Text>()?.text == LeaderboardRules.OpenPlayGames).onClick.Invoke();
            Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == LeaderboardRules.MakeProfilePublic));
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, "LEADERBOARD PROFILE STEPS"));
            }

            var layout = findings.Where(f => new[] { Defect.Overlap, Defect.CutOff, Defect.SpillsPastButton, Defect.CrowdsButton,
                Defect.PartlyHidden }.Contains(f.Defect)).ToList();
            Assert.IsEmpty(layout, ScreenAudit.Report(layout));
        }
    }
}
