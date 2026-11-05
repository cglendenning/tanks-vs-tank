using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-041 on the real mission list: the play arrow sits in the selected row, which opens on the next mission.
    public sealed class MissionListScreenTests
    {
        private const int Missions = 40;
        private const int Cleared = 25;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Progress => GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression => (PlayerProgression)Progress.GetType().GetProperty("Progression").GetValue(Progress);

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        // Twenty-five missions cleared at RECRUIT with medals, so mission 26 is next and 27 on are locked.
        private static float?[] Seed()
        {
            var saved = Enumerable.Range(1, Missions).Select(m => PlayerPrefs.HasKey("Lv" + m) ? PlayerPrefs.GetFloat("Lv" + m) : (float?)null).ToArray();
            for (var mission = 1; mission <= Missions; mission++)
                PlayerPrefs.SetFloat("Lv" + mission, mission <= Cleared ? 1f : 0f);
            Progression.Replace(new ProgressionState { clearedMissions = new[] { (1L << Cleared) - 1, 0L, 0L } });
            Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { DifficultyTier.Recruit });
            return saved;
        }

        private static void Restore(float?[] saved)
        {
            for (var mission = 1; mission <= Missions; mission++)
            {
                if (saved[mission - 1].HasValue)
                    PlayerPrefs.SetFloat("Lv" + mission, saved[mission - 1].Value);
                else
                    PlayerPrefs.DeleteKey("Lv" + mission);
            }
            Progression.Replace(new ProgressionState());
            Time.timeScale = 1f;
        }

        private static IEnumerator OpenMissionList()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            ButtonStarting("START MISSION").onClick.Invoke();
            // The list fades in, then glides to the selected row.
            yield return new WaitForSecondsRealtime(3.5f);
        }

        private static Dictionary<int, Button> Rows() =>
            GameObject.Find("Canvas/PanelMenu/Scroll/Panel").GetComponentsInChildren<Button>()
                .Where(b => b.transform.parent.name == "Panel")
                .ToDictionary(b => int.Parse(b.transform.Find("Text").GetComponent<Text>().text.Substring(8)));

        private static List<Button> Arrows() =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && b.name == "Play").ToList();

        private static bool InView(RectTransform view, RectTransform item)
        {
            var v = new Vector3[4];
            var i = new Vector3[4];
            view.GetWorldCorners(v);
            item.GetWorldCorners(i);
            return i[0].y >= v[0].y - 1f && i[1].y <= v[1].y + 1f;
        }

        [UnityTest(Description = "D-041-AC-03: the mission list opens on mission 26 with the play arrow in its row and in view, no arrow or MISSION label at the bottom, and every row label whole on one line at one size, and passes the screen audit on every shape")]
        public IEnumerator OpensWithTheArrowInTheNextMission()
        {
            var findings = new List<Finding>();
            var saved = Seed();
            try
            {
                yield return OpenMissionList();
                Assert.IsFalse(GameObject.Find("Canvas/PanelMenu").transform.Find("Play").gameObject.activeSelf, "no arrow at the bottom");
                Assert.IsFalse(GameObject.Find("Canvas/PanelMenu").transform.Find("Level").gameObject.activeSelf, "no MISSION: n label");
                var arrows = Arrows();
                Assert.AreEqual(1, arrows.Count, "one play arrow on the list");
                var rows = Rows();
                Assert.AreSame(rows[Cleared + 1].transform, arrows[0].transform.parent, "in the row of the next mission");
                Assert.IsTrue(rows[Cleared + 1].transform.Find("ImageWhite").gameObject.activeSelf, "that row is lit");
                Assert.IsTrue(InView((RectTransform)GameObject.Find("Canvas/PanelMenu/Scroll").transform, (RectTransform)arrows[0].transform),
                    "scrolled into view");
                var labels = rows.Values.Select(r => r.transform.Find("Text").GetComponent<Text>()).ToList();
                foreach (var label in labels)
                    Assert.LessOrEqual(label.preferredWidth, label.rectTransform.rect.width + 0.5f, "'" + label.text + "' fits on one line");
                Assert.AreEqual(1, labels.Select(l => l.fontSize).Distinct().Count(), "every row label the same size");
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "MISSIONS ON 26"));
                }
            }
            finally
            {
                Restore(saved);
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-041-AC-02: tapping an open mission moves the arrow into its row, a locked one does nothing, and the arrow or a second tap on the selected row starts that mission")]
        public IEnumerator TheArrowAndASecondTapStartTheMission()
        {
            var saved = Seed();
            try
            {
                yield return OpenMissionList();
                var rows = Rows();
                rows[3].onClick.Invoke();
                Assert.AreSame(rows[3].transform, Arrows().Single().transform.parent, "the arrow moves to mission 3");
                Assert.IsTrue(rows[3].transform.Find("ImageWhite").gameObject.activeSelf);
                Assert.IsFalse(rows[Cleared + 1].transform.Find("ImageWhite").gameObject.activeSelf, "the last row goes dark");
                rows[30].onClick.Invoke();
                Assert.AreSame(rows[3].transform, Arrows().Single().transform.parent, "a locked mission cannot be selected");

                Arrows().Single().onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                Assert.AreEqual("Lv3", SceneManager.GetActiveScene().name, "the arrow starts the selected mission");

                yield return OpenMissionList();
                rows = Rows();
                rows[2].onClick.Invoke();
                rows[2].onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                Assert.AreEqual("Lv2", SceneManager.GetActiveScene().name, "a second tap on the selected row starts it");
            }
            finally
            {
                Restore(saved);
            }
        }
    }
}
