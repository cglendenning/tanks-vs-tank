using System;
using System.Collections;
using System.Collections.Generic;
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
    // D-009-AC-03 in a real mission: Overdrive ends only when the tank is lost, so a revive bought when it goes down keeps it.
    public sealed class ReviveKeepsOverdriveTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static CombatBoosts Boosts => (CombatBoosts)GameType("TreadShredBoosts").GetProperty("Current").GetValue(null);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        [UnityTest(Description = "D-009-AC-03: a revive keeps Overdrive: the tank destroyed with Overdrive on is revived for gems, and Overdrive and OVERDRIVE ACTIVE stay for the rest of the mission")]
        public IEnumerator ReviveKeepsOverdrive()
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            try
            {
                Wallet.Replace(new WalletState { balance = 100, starterGranted = true });
                progression.Replace(new ProgressionState { clearedMissions = new[] { 1L, 0L, 0L } });
                Boosts.ArmOverdrive();
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv2");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                Assert.Contains(OfferLabels.OverdriveActive, Texts(), "Overdrive armed for this mission");

                var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
                var hit = player.GetType().GetMethod("DeadPlayer");
                for (var shell = 0; shell < 3; shell++)
                {
                    hit.Invoke(player, new object[] { 1 });
                    yield return new WaitForSecondsRealtime(0.8f);
                }
                Assert.IsNotNull(GameObject.Find("TreadShredRevive"), "the third shell destroyed the tank: the revive offer is up");

                var revive = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .First(b => (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith("REVIVE", StringComparison.Ordinal));
                revive.onClick.Invoke();
                yield return new WaitForSecondsRealtime(1f);

                Assert.IsNull(GameObject.Find("TreadShredRevive"), "revived: the panel is gone");
                Assert.AreEqual(100 - ProgressionRules.ReviveCostsGems[0], Wallet.Balance, "the revive was paid in gems");
                Assert.IsTrue(Boosts.OverdriveActive, "a revive keeps Overdrive");
                Assert.Contains(OfferLabels.OverdriveActive, Texts(), "and the HUD still says so");
                var failure = (GameObject)GameType("ManagerScore").GetField("FailureObj").GetValue(GameType("ManagerScore").GetField("instance").GetValue(null));
                Assert.IsFalse(failure != null && failure.activeInHierarchy, "the mission goes on: no failure screen");
            }
            finally
            {
                Time.timeScale = 1f;
                var open = GameObject.Find("TreadShredRevive");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                // Leave no Overdrive armed for the tests after this one.
                Boosts.TankLost();
                Boosts.ConsumeForMission(0);
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }
    }
}
