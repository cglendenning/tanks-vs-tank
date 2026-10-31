using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    public sealed class RetryLabelTests
    {
        [UnityTest(Description = "D-028-AC-01: a mission's free retry reads RETRY; REDEPLOY names only the paid Armored Redeploy")]
        public IEnumerator FreeRetryReadsRetry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return null;
            var labels = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.text).ToList();
            CollectionAssert.DoesNotContain(labels, "REDEPLOY");
            CollectionAssert.Contains(labels, "RETRY");
        }
    }
}
