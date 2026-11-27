using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TreadShred.Screens.Tests
{
    /// <summary>A first-time player's first play of mission 1, played in the editor through the test switch.</summary>
    public sealed class FirstMinuteScreenTests
    {
        private const string ProgressionKey = "TreadShredProgression";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static Text Label(string text) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.text == text && t.gameObject.activeInHierarchy);

        [UnityTest(Description = "D-035-AC-03: a first-time player's mission 1 shows Overdrive and TAP HERE TO FIRE on screen, the enemies fire back, and the hint goes after the first shot")]
        public IEnumerator FirstPlayIsAPowerMoment()
        {
            var saved = PlayerPrefs.GetString(ProgressionKey, string.Empty);
            var progress = TreadShredProgress.Ensure();
            var progression = (PlayerProgression)progress.Progression;
            try
            {
                TreadShredFirstMinute.editorPreview = true;
                progression.Replace(new ProgressionState());
                progress.SelectTier(DifficultyTier.Recruit);
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv1");
                for (var i = 0; i < 5; i++)
                    yield return null;

                Assert.IsNull(GameType("TreadShredFirstMinute").GetProperty("EnemiesHoldFire"), "the enemies fight back from the start of the first play");
                Assert.IsNotNull(Label(OfferLabels.OverdriveActive), "Overdrive is on, free for this mission");
                var hint = Label(FirstMinute.FireHint);
                Assert.IsNotNull(hint, "the fire hint shows above an enemy");
                var corners = new Vector3[4];
                hint.rectTransform.GetWorldCorners(corners);
                Assert.IsTrue(corners.All(c => c.x >= 0f && c.x <= Screen.width && c.y >= 0f && c.y <= Screen.height),
                    "the hint is fully on screen");

                var bulletsBefore = GameObject.FindGameObjectsWithTag("bulletemnemy").Length;
                foreach (var gun in Object.FindObjectsByType<BulletEmnemy>(FindObjectsSortMode.None))
                    gun.BulletPlayer();
                yield return null;
                Assert.Greater(GameObject.FindGameObjectsWithTag("bulletemnemy").Length, bulletsBefore, "the enemies fire back on the first play");
                ((ManagerScore)ManagerScore.instance).CurCountbullet = 1;
                yield return null;
                yield return null;
                Assert.IsNull(Label(FirstMinute.FireHint), "the hint goes after the first shot");
            }
            finally
            {
                TreadShredFirstMinute.editorPreview = false;
                if (!string.IsNullOrEmpty(saved))
                    progression.Replace(JsonUtility.FromJson<ProgressionState>(saved));
                TreadShredOverdriveBadge.Hide();
            }
        }
    }
}
