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
    // D-017-AC-09 in a real mission: shields show the hits left, behind the revive panel and after a revive.
    public sealed class ShieldDisplayTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        [UnityTest(Description = "D-017-AC-09: three one-hit shells leave no shield lit behind the revive panel even after a second, and a revive lights all three again and keeps them lit")]
        public IEnumerator ShieldsShowTheHitsLeft()
        {
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            progression.Replace(new ProgressionState());
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv2");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);

            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            var hearts = ((List<GameObject>)player.GetType().GetField("Heart").GetValue(player)).Where(h => h != null).ToList();
            Assert.AreEqual(3, hearts.Count, "three shields on the HUD");
            float Alpha(GameObject heart) => heart.GetComponent<Image>().color.a;
            var hit = player.GetType().GetMethod("DeadPlayer");
            try
            {
                for (var shell = 0; shell < 3; shell++)
                {
                    hit.Invoke(player, new object[] { 1 });
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                Assert.IsNotNull(GameObject.Find("TreadShredRevive"), "the third shell destroyed the tank: the revive offer is up");
                Assert.AreEqual(0f, Time.timeScale, "and the game is paused behind it");
                yield return new WaitForSecondsRealtime(1f);
                Assert.IsTrue(hearts.All(h => Alpha(h) < 0.5f), "no shield lit once the tank is destroyed: " + string.Join(", ", hearts.Select(Alpha)));

                player.GetType().GetMethod("Revive").Invoke(player, null);
                UnityEngine.Object.Destroy(GameObject.Find("TreadShredRevive"));
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.IsTrue(hearts.All(h => Alpha(h) > 0.9f), "a revive lights all three and nothing dims them after: " + string.Join(", ", hearts.Select(Alpha)));
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
