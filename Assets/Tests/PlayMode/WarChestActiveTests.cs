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
    // D-047-AC-02 on a real result screen (owner direction 2026-11-27): while a War Chest the player bought is running,
    // nothing offers it; once its gear is used up, WAR CHEST: <price> is back.
    public sealed class WarChestActiveTests
    {
        private const string WalletKey = "TreadShredGemWallet";

        private static GemWallet Wallet => TreadShredStore.Ensure().Wallet;

        private static PlayerProgression Progression => TreadShredProgress.Ensure().Progression;

        private static Button ResultButton() =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name == TreadShredWarChestOffer.ResultButtonName);

        private static IEnumerator LoseMission()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            UnityEngine.Object.FindFirstObjectByType<ManagerScore>().Failure();
            yield return new WaitForSecondsRealtime(2.6f);
        }

        [UnityTest(Description = "D-047-AC-02: while a bought War Chest is running no result screen offers it, and once its gear is used up WAR CHEST: <price> is back")]
        public IEnumerator NotOfferedWhileItsGearLasts()
        {
            var savedWallet = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var savedProgression = JsonUtility.ToJson(Progression.state);
            try
            {
                // After the Starter Pack's window, the War Chest seen before: it is on sale.
                Wallet.Replace(new WalletState
                {
                    balance = 0,
                    starterGranted = true,
                    starterPackBought = true,
                    starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks,
                    warChestSeen = true
                });
                Progression.Replace(new ProgressionState { warChestChecked = true });
                Assert.IsTrue(TreadShredStore.Ensure().WarChestOnSale, "on sale before it is bought");

                Wallet.CreditPurchase(GemCatalog.WarChest.ProductId, "test-order-running");
                Progression.EquipWarChest();
                Assert.IsFalse(TreadShredStore.Ensure().WarChestOnSale, "not on sale while its gear lasts");
                yield return LoseMission();
                Assert.IsNull(GameObject.Find("TreadShredWarChestOffer"), "no offer over a lost mission");
                Assert.IsNull(ResultButton(), "no WAR CHEST button on the result screen");

                Progression.Replace(new ProgressionState { warChestChecked = true });
                Assert.IsTrue(TreadShredStore.Ensure().WarChestOnSale, "back on sale once its gear is used up");
                yield return LoseMission();
                Assert.IsNotNull(ResultButton(), "WAR CHEST: <price> is back on the result screen");
            }
            finally
            {
                if (GameObject.Find("TreadShredWarChestOffer") != null)
                    UnityEngine.Object.Destroy(GameObject.Find("TreadShredWarChestOffer"));
                Time.timeScale = 1f;
                Progression.Replace(JsonUtility.FromJson<ProgressionState>(savedProgression));
                Wallet.Replace(JsonUtility.FromJson<WalletState>(savedWallet));
            }
        }
    }
}
