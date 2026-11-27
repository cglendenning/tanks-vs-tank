using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-047-AC-08 on real result screens: once the player has seen the War Chest, WAR CHEST: <price> stays one tap away.
    public sealed class WarChestResultButtonTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Store().GetType().GetProperty("Wallet").GetValue(Store());

        private static object Manager() => UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));

        private static GameObject Interstitial() => GameObject.Find("TreadShredWarChestOffer");

        private static string ResultButtonName => (string)GameType("TreadShredWarChestOffer").GetField("ResultButtonName").GetValue(null);

        private static Button ResultButton() =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name == ResultButtonName);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonLabelled(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator EndMission(bool won)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            var manager = Manager();
            if (won)
            {
                manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
                manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
                yield return new WaitForSecondsRealtime(3.5f);
            }
            else
            {
                manager.GetType().GetMethod("Failure", Members).Invoke(manager, null);
                yield return new WaitForSecondsRealtime(2.6f);
            }
        }

        [UnityTest(Description = "D-047-AC-08: once seen, WAR CHEST: <price> is on every result screen, opens the reveal with the purchase alone and closes back to the result screen, on every screen shape")]
        public IEnumerator SeenWarChestStaysOnEveryResultScreen()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                // After the Starter Pack's window, already bought once: still on sale, never yet seen.
                Wallet.Replace(new WalletState
                {
                    balance = 0,
                    starterGranted = true,
                    starterPackBought = true,
                    starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks,
                    warChestBought = true
                });
                Assert.IsTrue((bool)Store().GetType().GetProperty("WarChestOnSale").GetValue(Store()), "on sale though bought (D-047-AC-02)");

                yield return EndMission(false);
                Assert.IsNull(Interstitial(), "the session's War Chest offer has been made (WarChestSessionSetup)");
                Assert.IsNull(ResultButton(), "not before the player has seen it");

                Wallet.MarkWarChestSeen();
                foreach (var won in new[] { false, true })
                {
                    yield return EndMission(won);
                    var button = ResultButton();
                    Assert.IsNotNull(button, won ? "on a victory" : "on a failure");
                    StringAssert.StartsWith("WAR CHEST: ", button.GetComponentInChildren<Text>().text);
                    yield return Audit(won ? "War Chest button after a victory" : "War Chest button after a loss", findings);
                }

                ResultButton().onClick.Invoke();
                yield return new WaitForSecondsRealtime(WarChest.RevealSeconds * (WarChest.Items.Count + 6));
                Assert.IsNotNull(Interstitial(), "the reveal opens");
                var texts = Texts();
                Assert.Contains(WarChest.Subtitle, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)), "the purchase");
                Assert.IsNull(ButtonLabelled(WarChest.TestDriveButton), "the purchase alone, no test drive");
                ButtonLabelled("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsNull(Interstitial(), "NOT NOW closes it");
                Assert.IsNotNull(ResultButton(), "back on the result screen");
            }
            finally
            {
                if (Interstitial() != null)
                    UnityEngine.Object.Destroy(Interstitial());
                Time.timeScale = 1f;
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
