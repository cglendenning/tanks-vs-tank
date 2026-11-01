using System;
using System.Collections;
using System.Linq;
using System.Reflection;
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

        [UnityTest(Description = "D-035-AC-03: a first-time player's mission 1 shows Overdrive and TAP HERE TO FIRE on screen, enemies hold fire, and the hint goes after the first shot")]
        public IEnumerator FirstPlayIsAPowerMoment()
        {
            var firstMinute = GameType("TreadShredFirstMinute");
            var editorPreview = firstMinute.GetField("editorPreview", BindingFlags.NonPublic | BindingFlags.Static);
            var saved = PlayerPrefs.GetString(ProgressionKey, string.Empty);
            var progress = GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);
            var progression = (PlayerProgression)progress.GetType().GetProperty("Progression").GetValue(progress);
            try
            {
                editorPreview.SetValue(null, true);
                progression.Replace(new ProgressionState());
                progress.GetType().GetMethod("SelectTier").Invoke(progress, new object[] { DifficultyTier.Recruit });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv1");
                for (var i = 0; i < 5; i++)
                    yield return null;

                Assert.IsTrue((bool)firstMinute.GetProperty("EnemiesHoldFire").GetValue(null), "enemies hold fire on the first play");
                Assert.IsNotNull(Label(OfferLabels.OverdriveActive), "Overdrive is on, free for this mission");
                var hint = Label(FirstMinute.FireHint);
                Assert.IsNotNull(hint, "the fire hint shows above an enemy");
                var corners = new Vector3[4];
                hint.rectTransform.GetWorldCorners(corners);
                Assert.IsTrue(corners.All(c => c.x >= 0f && c.x <= Screen.width && c.y >= 0f && c.y <= Screen.height),
                    "the hint is fully on screen");

                var bulletsBefore = GameObject.FindGameObjectsWithTag("bulletemnemy").Length;
                foreach (var gun in Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None))
                    gun.GetType().GetMethod("BulletPlayer").Invoke(gun, null);
                yield return null;
                Assert.AreEqual(bulletsBefore, GameObject.FindGameObjectsWithTag("bulletemnemy").Length, "no enemy fires");

                var manager = GameType("ManagerScore");
                manager.GetField("CurCountbullet").SetValue(manager.GetField("instance").GetValue(null), 1);
                yield return null;
                yield return null;
                Assert.IsNull(Label(FirstMinute.FireHint), "the hint goes after the first shot");
            }
            finally
            {
                editorPreview.SetValue(null, false);
                if (!string.IsNullOrEmpty(saved))
                    progression.Replace(JsonUtility.FromJson<ProgressionState>(saved));
                GameType("TreadShredOverdriveBadge").GetMethod("Hide").Invoke(null, null);
            }
        }
    }
}
