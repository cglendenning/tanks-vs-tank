using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-014-AC-04 in the game: the settings screen names the installed version.
    public sealed class SettingsVersionTests
    {
        [UnityTest(Description = "D-014-AC-04: the settings screen shows VERSION <installed version> in small muted text beside BACK, and passes the screen audit on every phone and tablet shape")]
        public IEnumerator SettingsShowsTheVersion()
        {
            var findings = new List<Finding>();
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            var font = (Font)TreadShredUi.FindFont(GameObject.Find("Canvas"));
            var menu = UnityEngine.Object.FindFirstObjectByType<LoadLv>();
            var panel = (Component)TreadShredSettingsPanel.Show(menu, font);
            try
            {
                yield return null;
                var label = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.activeInHierarchy && t.text == "VERSION " + Application.version);
                Assert.IsNotNull(label, "VERSION " + Application.version + " on the settings screen");
                var back = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .First(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == "BACK");
                Assert.Less(label.fontSize, back.GetComponentInChildren<Text>().fontSize, "smaller than BACK");
                Assert.AreEqual((Color)TreadShredUi.DetailColor, label.color, "the muted detail color");
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "SETTINGS WITH VERSION"));
                }
            }
            finally
            {
                if (panel != null)
                    UnityEngine.Object.Destroy(panel.gameObject);
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
