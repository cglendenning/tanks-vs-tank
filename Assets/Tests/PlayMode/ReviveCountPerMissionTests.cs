using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-020-AC-04 in real missions: the revive limit counts revives in one play of a mission, so RETRY starts a fresh count.
    public sealed class ReviveCountPerMissionTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static IEnumerator WaitForPlay()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // Three one-hit shells destroy a tank with base armor.
        private static IEnumerator DestroyTank()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            var hit = player.GetType().GetMethod("DeadPlayer");
            for (var shell = 0; shell < 3; shell++)
            {
                hit.Invoke(player, new object[] { 1 });
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }

        private static Button ReviveButton() => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith("REVIVE", StringComparison.Ordinal));

        [UnityTest(Description = "D-020-AC-04: two revives in a play of a mission, none for a third destruction, and RETRY starts a fresh count at 30 gems")]
        public IEnumerator RetryStartsAFreshReviveCount()
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true });
                progression.Replace(new ProgressionState { clearedMissions = new[] { 1L, 0L, 0L } });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv2");
                yield return WaitForPlay();

                foreach (var cost in ProgressionRules.ReviveCostsGems)
                {
                    yield return DestroyTank();
                    var revive = ReviveButton();
                    Assert.IsNotNull(revive, "a revive is offered while under the limit");
                    StringAssert.EndsWith("FOR " + cost + " GEMS", revive.GetComponentInChildren<Text>().text);
                    revive.onClick.Invoke();
                    yield return new WaitForSecondsRealtime(1f);
                }
                yield return DestroyTank();
                Assert.IsNull(GameObject.Find("TreadShredRevive"), "no third revive in the same play of the mission");
                yield return new WaitForSecondsRealtime(3f);

                var retry = GameObject.Find("ButtonRetry");
                Assert.IsNotNull(retry, "the failure screen offers RETRY");
                retry.GetComponent<Button>().onClick.Invoke();
                yield return WaitForPlay();
                Assert.AreEqual("Lv2", SceneManager.GetActiveScene().name, "RETRY replays the same mission");

                yield return DestroyTank();
                var fresh = ReviveButton();
                Assert.IsNotNull(fresh, "the retried mission offers a revive again");
                StringAssert.EndsWith("FOR " + ProgressionRules.ReviveCostsGems[0] + " GEMS", fresh.GetComponentInChildren<Text>().text,
                    "at the first revive's price");
            }
            finally
            {
                Time.timeScale = 1f;
                var open = GameObject.Find("TreadShredRevive");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }
    }
}
