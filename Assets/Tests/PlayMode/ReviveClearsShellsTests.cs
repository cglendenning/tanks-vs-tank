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
    // D-020-AC-07: a paid revive clears the enemy shells still in flight.
    public sealed class ReviveClearsShellsTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression =>
            (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static int InFlight => (int)GameType("TreadShredEnemyShells").GetProperty("InFlight").GetValue(null);

        [UnityTest(Description = "D-020-AC-07: shells the enemies fired at the destroyed tank are all gone the moment it is revived, and the revived tank keeps its full armor")]
        public IEnumerator ReviveClearsEnemyShells()
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true, starterPackBought = true });
                Progression.Replace(new ProgressionState { clearedMissions = new[] { (1L << 40) - 1, 0L, 0L } });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv16");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);

                var player = GameObject.FindGameObjectWithTag("player").GetComponent(GameType("PlayerMoverment"));
                var hit = player.GetType().GetMethod("DeadPlayer");
                for (var shell = 0; shell < 3; shell++)
                {
                    hit.Invoke(player, new object[] { 1 });
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                Assert.IsNotNull(GameObject.Find("TreadShredRevive"), "the revive offer holds the game");

                // Every turret that has reloaded fires at the destroyed tank while the offer is up.
                foreach (var turret in UnityEngine.Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None))
                    turret.GetType().GetMethod("BulletPlayer").Invoke(turret, null);
                Assert.Greater(InFlight, 0, "enemy shells are in flight");

                UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .First(b => (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith("REVIVE", StringComparison.Ordinal))
                    .onClick.Invoke();
                yield return null;
                Assert.AreEqual(0, InFlight, "every enemy shell in flight is cleared by the revive");
                var shields = ((System.Collections.Generic.List<GameObject>)player.GetType().GetField("Heart").GetValue(player)).Where(h => h != null);
                Assert.IsTrue(shields.All(h => h.GetComponent<Image>().color.a > 0.9f), "full armor again");
            }
            finally
            {
                Time.timeScale = 1f;
                foreach (var name in new[] { "TreadShredReady", "TreadShredRevive" })
                {
                    var open = GameObject.Find(name);
                    if (open != null)
                        UnityEngine.Object.Destroy(open);
                }
                Progression.Replace(new ProgressionState());
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }
    }
}
