using System;
using System.Collections;
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
    /// <summary>The first play's opening cues and its armor floor, played in the editor through the test switch.</summary>
    public sealed class FirstMinuteCuesTests
    {
        private const string ProgressionKey = "TreadShredProgression";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static Text Label(string text) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.text == text && t.gameObject.activeInHierarchy);

        [UnityTest(Description = "D-035-AC-03: GET READY shows over the countdown and lifts at ENGAGE, the thumb hint sits by the joystick, and a first play cannot be lost")]
        public IEnumerator OpeningCuesAndArmorFloor()
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

                Assert.AreEqual(0f, Time.timeScale, "the countdown is running");
                Assert.IsNotNull(Label(FirstMinute.GetReady), "GET READY shows during the countdown");
                var move = Label(FirstMinute.MoveHint);
                Assert.IsNotNull(move, "the thumb hint shows");
                var corners = new Vector3[4];
                move.rectTransform.GetWorldCorners(corners);
                Assert.IsTrue(corners.All(c => c.x >= 0f && c.x <= Screen.width && c.y >= 0f && c.y <= Screen.height), "the thumb hint is on screen");
                Assert.Less(corners.Average(c => c.x), Screen.width / 2f, "it sits on the joystick's side, bottom left");
                Assert.Less(corners.Average(c => c.y), Screen.height / 2f);

                var count = Object.FindFirstObjectByType(GameType("Count"));
                count.GetType().GetMethod("Letstart").Invoke(count, null);
                yield return null;
                yield return null;
                Assert.IsNull(Label(FirstMinute.GetReady), "GET READY lifts at ENGAGE");
                Assert.IsNotNull(Label(FirstMinute.MoveHint), "the thumb hint stays until the player moves");

                var player = GameObject.FindGameObjectWithTag("player").GetComponent(GameType("PlayerMoverment"));
                player.GetType().GetMethod("DeadPlayer").Invoke(player, new object[] { 10 });
                Assert.AreEqual(1, (int)player.GetType().GetField("countdie").GetValue(player), "a hit never takes the last armor on a first play");
            }
            finally
            {
                editorPreview.SetValue(null, false);
                if (!string.IsNullOrEmpty(saved))
                    progression.Replace(JsonUtility.FromJson<ProgressionState>(saved));
                GameType("TreadShredOverdriveBadge").GetMethod("Hide").Invoke(null, null);
                Time.timeScale = 1f;
            }
        }
    }
}
