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
    // D-043-AC-03 in a mission: the rental offer keeps the game paused, and NOT NOW goes back through the 3-2-1.
    public sealed class RentalPauseScreenTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression =>
            (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static Button Under(string root, string label)
        {
            var parent = GameObject.Find(root);
            return parent == null ? null : parent.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => (b.GetComponentInChildren<Text>()?.text ?? string.Empty) == label);
        }

        private static Button ButtonLabeled(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static bool CountingDown => UnityEngine.Object.FindFirstObjectByType(GameType("Count")) != null;

        private static Weapon InHand()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            return (Weapon)player.GetType().GetProperty("WeaponInHand").GetValue(player);
        }

        [UnityTest(Description = "D-043-AC-03: the arc shot's offer from the drawer keeps the game paused, and NOT NOW plays the 3-2-1 with the weapon button hidden, then returns to the mission unchanged")]
        public IEnumerator NotNowGoesBackThroughTheCountdown()
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true, starterPackBought = true });
                Progression.Replace(new ProgressionState
                {
                    clearedMissions = new[] { (1L << 40) - 1, 0L, 0L }, usedWeapons = WeaponRental.Bit(Weapon.ArcShot), rentals = new int[4]
                });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv26");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);

                Under("TreadShredWeaponToggle", "TRIPLE SHOT").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Under("TreadShredWeaponDrawer", "ARC SHOT").onClick.Invoke();
                yield return new WaitForSecondsRealtime(1f);
                Assert.IsNotNull(GameObject.Find("TreadShredSales"), "the offer is open");
                Assert.AreEqual(0f, Time.timeScale, "the game stays paused on the offer");

                ButtonLabeled(WeaponRental.Decline).onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsNull(GameObject.Find("TreadShredSales"));
                Assert.IsTrue(CountingDown, "NOT NOW plays the 3-2-1");
                Assert.AreEqual(0f, Time.timeScale, "still paused during the countdown");
                Assert.IsNull(GameObject.Find("TreadShredWeaponToggle"), "the weapon button is hidden during the countdown");

                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                Assert.AreEqual(1f, Time.timeScale, "play resumes after the 3-2-1");
                Assert.AreEqual((Weapon.TripleShot, 200), (InHand(), Wallet.Balance), "the mission goes on unchanged");
            }
            finally
            {
                Time.timeScale = 1f;
                var open = GameObject.Find("TreadShredSales");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Progression.Replace(new ProgressionState());
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }
    }
}
