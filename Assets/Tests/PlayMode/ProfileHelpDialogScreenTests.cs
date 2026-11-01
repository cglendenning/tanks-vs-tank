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
    /// <summary>The how-to dialog behind GO PUBLIC AND COMPETE, drawn and audited on every screen shape.</summary>
    public sealed class ProfileHelpDialogScreenTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static Button ButtonLabelled(string label) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .SingleOrDefault(b => b.GetComponentInChildren<Text>()?.text == label);

        private static IEnumerator AuditAll(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        [UnityTest(Description = "D-032-AC-02: GO PUBLIC AND COMPETE shows both numbered steps with their pictures, fits every screen shape, and NOT NOW or Back closes it")]
        public IEnumerator DialogShowsStepsAndCloses()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return null;
            var font = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.font).First(f => f != null && f.name.Replace(" ", string.Empty).StartsWith("BlackOps"));
            var name = new string('W', LeaderboardPage.MaxNameLength);
            var rows = Enumerable.Range(1, 20).Select(rank => new LeaderboardEntry(rank, name, 90000 - rank, false)).ToList();
            var panelType = GameType("TreadShredLeaderboardPanel");
            panelType.GetMethod("OpenWithPage", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { font, new LeaderboardPage(rows, new LeaderboardEntry(0, name, 1234567, true)) });
            var panel = Object.FindFirstObjectByType(panelType);
            panelType.GetMethod("ShowPrivateProfileHelp", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);

            ButtonLabelled(LeaderboardRules.MakePublicButton).onClick.Invoke();
            yield return null;
            var dialogType = GameType("TreadShredProfileHelp");
            Assert.IsNotNull(Object.FindFirstObjectByType(dialogType), "the dialog opens before Play Games");
            var labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            CollectionAssert.IsSubsetOf(new[] { LeaderboardRules.ProfileHelpStepOne, LeaderboardRules.ProfileHelpStepTwo,
                LeaderboardRules.ProfileHelpStepThree }, labels);
            var pictures = Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None).Where(r => r.name == "StepPicture").ToList();
            Assert.AreEqual(2, pictures.Count, "a picture under each step");
            Assert.IsTrue(pictures.All(p => p.texture != null && p.texture.width == 720));

            var findings = new List<Finding>();
            yield return AuditAll("PROFILE HELP", findings);
            // No Play Games screen opens in the editor: the written steps appear and the dialog stays open.
            ButtonLabelled(LeaderboardRules.OpenPlayGames).onClick.Invoke();
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType(dialogType));
            Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Count(t => t.text == LeaderboardRules.MakeProfilePublic) >= 1);
            yield return AuditAll("PROFILE HELP STEPS", findings);
            var layout = findings.Where(f => new[] { Defect.Overlap, Defect.CutOff, Defect.SpillsPastButton, Defect.CrowdsButton,
                Defect.PartlyHidden }.Contains(f.Defect)).ToList();
            Assert.IsEmpty(layout, ScreenAudit.Report(layout));

            ButtonLabelled(LeaderboardRules.NotNow).onClick.Invoke();
            yield return null;
            Assert.IsNull(Object.FindFirstObjectByType(dialogType), "NOT NOW closes it");
            ButtonLabelled(LeaderboardRules.MakePublicButton).onClick.Invoke();
            yield return null;
            Assert.IsTrue((bool)GameType("TreadShredBack").GetMethod("Press").Invoke(null, null));
            yield return null;
            Assert.IsNull(Object.FindFirstObjectByType(dialogType), "the system Back closes it");
            Assert.IsNotNull(Object.FindFirstObjectByType(panelType), "Back closes only the dialog, not the leaderboard");
        }
    }
}
