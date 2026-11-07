using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-017-AC-09 in a mission: shells landing faster than a shield's pulse still take the shields in line.
    public sealed class ShieldOrderTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static PlayerProgression Progression
        {
            get
            {
                var progress = GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);
                return (PlayerProgression)progress.GetType().GetProperty("Progression").GetValue(progress);
            }
        }

        private static float Alpha(GameObject shield) => shield.GetComponent<Image>().color.a;

        [UnityTest(Description = "D-017-AC-09: two shells inside one shield's pulse put out the first two shields in line from the left and leave the last lit, every shield back at full size")]
        public IEnumerator FastHitsTakeShieldsInLine()
        {
            try
            {
                Progression.Replace(new ProgressionState { clearedMissions = new[] { (1L << 40) - 1, 0L, 0L } });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv2");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);

                var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
                var line = ((List<GameObject>)player.GetType().GetField("Heart").GetValue(player))
                    .Where(h => h != null).OrderBy(h => h.transform.position.x).ToList();
                Assert.AreEqual(3, line.Count, "three shields on the HUD");
                Assert.IsTrue(line.All(h => Alpha(h) > 0.9f), "all lit at the start");

                var hit = player.GetType().GetMethod("DeadPlayer");
                hit.Invoke(player, new object[] { 1 });
                yield return new WaitForSecondsRealtime(0.1f);
                hit.Invoke(player, new object[] { 1 });
                yield return new WaitForSecondsRealtime(1f);

                var alphas = string.Join(", ", line.Select(Alpha));
                Assert.IsTrue(Alpha(line[0]) < 0.5f && Alpha(line[1]) < 0.5f, "the first two in line are out: " + alphas);
                Assert.IsTrue(Alpha(line[2]) > 0.9f, "the last in line is lit: " + alphas);
                Assert.IsTrue(line.All(h => h.transform.localScale == Vector3.one), "no shield is left mid-pulse");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
            }
        }
    }
}
