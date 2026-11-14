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
    // D-047 on a real lost mission, with the editor's test store offering the War Chest's product. WarChestSessionSetup
    // keeps it off every other screen test; this reopens it and puts the wallet back after.
    public sealed class WarChestTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static GemWallet Wallet => (GemWallet)Store().GetType().GetProperty("Wallet").GetValue(Store());

        private static object Manager() => UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));

        private static GameObject Interstitial() => GameObject.Find("TreadShredWarChestOffer");

        private static bool Revealed() =>
            (bool)GameType("TreadShredWarChestOffer").GetProperty("Revealed").GetValue(Interstitial().GetComponent(GameType("TreadShredWarChestOffer")));

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

        private static IEnumerator LoseMission()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            Manager().GetType().GetMethod("Failure", Members).Invoke(Manager(), null);
            yield return new WaitForSecondsRealtime(2.6f);
        }

        [UnityTest(Description = "D-047-AC-04: a lost mission opens the War Chest; each item lands before NOT NOW shows and Back waits for it; then the total, the price and the button, on every screen shape, and only once a session")]
        public IEnumerator LostMissionRevealsTheWarChest()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState
                {
                    balance = 0,
                    starterGranted = true,
                    starterPackBought = true,
                    starterPackWindowStartTicks = DateTime.UtcNow.AddHours(-1).Ticks
                });
                WarChestSessionSetup.Offered(false);
                Assert.IsTrue((bool)Store().GetType().GetProperty("WarChestOnSale").GetValue(Store()),
                    "the editor's test store offers the War Chest's product");

                yield return LoseMission();
                Assert.IsNotNull(Interstitial(), "the War Chest opens over the failure screen");
                Assert.IsFalse(Revealed(), "still landing its items");
                Assert.IsNull(ButtonLabelled("NOT NOW"), "NOT NOW waits for the last item");
                GameType("TreadShredBack").GetMethod("Press").Invoke(null, null);
                yield return null;
                Assert.IsNotNull(Interstitial(), "Back waits for the last item too");

                yield return new WaitForSecondsRealtime(WarChest.RevealSeconds * (WarChest.Items.Count + 6));
                Assert.IsTrue(Revealed());
                var texts = Texts();
                Assert.Contains(WarChest.Title, texts);
                foreach (var item in WarChest.Items)
                    Assert.Contains(WarChest.Line(item), texts);
                Assert.Contains(WarChest.WeaponsNote, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("TOTAL VALUE ", StringComparison.Ordinal)), string.Join(" | ", texts));
                Assert.IsTrue(texts.Any(t => t.StartsWith("YOURS FOR ", StringComparison.Ordinal)), "the price");
                Assert.IsTrue(texts.Any(t => t.StartsWith("GET THE WAR CHEST", StringComparison.Ordinal)), "the button");
                Assert.IsNotNull(ButtonLabelled("NOT NOW"));
                yield return Audit("War Chest over a lost mission", findings);

                ButtonLabelled("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsNull(Interstitial(), "NOT NOW closes it");
                yield return LoseMission();
                Assert.IsNull(Interstitial(), "offered once a session");
            }
            finally
            {
                if (Interstitial() != null)
                    UnityEngine.Object.Destroy(Interstitial());
                WarChestSessionSetup.Offered(true);
                Time.timeScale = 1f;
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
