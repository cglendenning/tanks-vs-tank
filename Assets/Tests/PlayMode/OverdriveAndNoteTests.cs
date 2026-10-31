using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    public sealed class OverdriveAndNoteTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static bool Showing(string label) =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == label && t.isActiveAndEnabled);

        [UnityTest(Description = "D-009-AC-03: with Overdrive active the mission HUD shows OVERDRIVE ACTIVE, hidden while paused, and it fits on every screen shape")]
        public IEnumerator HudShowsActiveOverdrive()
        {
            PlayerPrefs.SetInt(CombatBoosts.OverdriveKey, 1);
            try
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv1");
                yield return null;
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.IsTrue(Showing(OfferLabels.OverdriveActive), "the HUD says Overdrive is on");
                Assert.AreEqual(1, PlayerPrefs.GetInt(CombatBoosts.OverdriveKey, 0), "starting a mission does not use it up");
                var findings = new List<Finding>();
                yield return Audit("mission with overdrive", findings);
                Assert.IsEmpty(findings, ScreenAudit.Report(findings));
            }
            finally
            {
                PlayerPrefs.DeleteKey(CombatBoosts.OverdriveKey);
                GameType("TreadShredOverdriveBadge").GetMethod("Hide").Invoke(null, null);
            }
        }

        [UnityTest(Description = "D-018-AC-04: COINS AND GEMS on the upgrade screen opens the note, which fits on every screen shape and Back closes")]
        public IEnumerator CurrencyNoteOpensFromUpgrades()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
            var panel = (Component)GameType("TreadShredUpgradePanel").GetMethod("Show").Invoke(null, new object[] { font });
            yield return null;
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.GetComponentInChildren<Text>()?.text == ProgressionLabels.CurrencyNoteTitle).onClick.Invoke();
            yield return null;
            Assert.IsTrue(ProgressionLabels.CurrencyNote().All(Showing), "every line of the note shows");
            var findings = new List<Finding>();
            yield return Audit("coins and gems note", findings);

            GameType("TreadShredBack").GetMethod("Press").Invoke(null, null);
            yield return null;
            Assert.IsFalse(Showing(ProgressionLabels.CurrencyNote()[0]), "Back closes the note");
            Assert.IsTrue(panel != null, "and leaves the upgrade screen open");
            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
