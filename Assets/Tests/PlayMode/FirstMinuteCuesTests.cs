using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TreadShred.Screens.Tests
{
    /// <summary>The first play's opening cues, and its armor taken as on any mission, played in the editor through the test switch.</summary>
    public sealed class FirstMinuteCuesTests
    {
        private const string ProgressionKey = "TreadShredProgression";

        private static Text Label(string text) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t.text == text && t.gameObject.activeInHierarchy);

        [UnityTest(Description = "D-035-AC-03: GET READY shows over the countdown and lifts at ENGAGE, the thumb hint sits by the joystick, and a hit costs armor as on any mission")]
        public IEnumerator OpeningCuesAndArmorFloor()
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

                Assert.AreEqual(0f, Time.timeScale, "the countdown is running");
                Assert.IsNotNull(Label(FirstMinute.GetReady), "GET READY shows during the countdown");
                var move = Label(FirstMinute.MoveHint);
                Assert.IsNotNull(move, "the thumb hint shows");
                var corners = new Vector3[4];
                move.rectTransform.GetWorldCorners(corners);
                Assert.IsTrue(corners.All(c => c.x >= 0f && c.x <= Screen.width && c.y >= 0f && c.y <= Screen.height), "the thumb hint is on screen");
                Assert.Less(corners.Average(c => c.x), Screen.width / 2f, "it sits on the joystick's side, bottom left");
                Assert.Less(corners.Average(c => c.y), Screen.height / 2f);

                var count = Object.FindFirstObjectByType<Count>();
                count.Letstart();
                yield return null;
                yield return null;
                Assert.IsNull(Label(FirstMinute.GetReady), "GET READY lifts at ENGAGE");
                Assert.IsNotNull(Label(FirstMinute.MoveHint), "the thumb hint stays until the player moves");

                var player = GameObject.FindGameObjectWithTag("player").GetComponent<PlayerMoverment>();
                player.DeadPlayer(10);
                Assert.Less((int)player.countdie, 1, "a hit can take the last armor: the first play can be lost");
            }
            finally
            {
                TreadShredFirstMinute.editorPreview = false;
                if (!string.IsNullOrEmpty(saved))
                    progression.Replace(JsonUtility.FromJson<ProgressionState>(saved));
                TreadShredOverdriveBadge.Hide();
                Time.timeScale = 1f;
            }
        }
    }
}
