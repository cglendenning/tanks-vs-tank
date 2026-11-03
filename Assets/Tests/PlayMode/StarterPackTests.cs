using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-037 on the real revive panel and gem store, with the editor's test store offering the Starter Pack.
    public sealed class StarterPackTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private static readonly string StarterHeading = OfferLabels.StarterPackTitle + "  |  " + OfferLabels.StarterPackLimit;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static T Get<T>(object target, string property) => (T)target.GetType().GetProperty(property).GetValue(target);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static IEnumerator UntilOnSale(object store)
        {
            for (var waited = 0f; !Get<bool>(store, "StarterPackOnSale") && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(Get<bool>(store, "StarterPackOnSale"), "the editor's test store offers the Starter Pack's product");
        }

        private static void Deliver(object store, string orderId)
        {
            Get<PurchaseFlow>(store, "Purchases").OrderDelivered(GemCatalog.StarterPack.ProductId, orderId, out _);
        }

        private static void OfferRevive()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            Assert.IsNotNull(player, "the mission has the player's tank");
            Assert.IsTrue((bool)GameType("TreadShredRevivePanel").GetMethod("TryOffer").Invoke(null, new object[] { player }));
        }

        private static IEnumerator CloseRevive()
        {
            UnityEngine.Object.Destroy(GameObject.Find("TreadShredRevive"));
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest(Description = "D-037-AC-03: the first revive panel while the Starter Pack is on sale offers it once beside the revive offer; buying it credits the gems and leaves the revive open, and both panels pass the screen audit")]
        public IEnumerator RevivePanelOffersTheStarterPackOnce()
        {
            var store = Store();
            var wallet = Get<GemWallet>(store, "Wallet");
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var findings = new List<Finding>();
            try
            {
                wallet.Replace(new WalletState { balance = 0, starterGranted = true });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv1");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                yield return UntilOnSale(store);

                OfferRevive();
                yield return null;
                Assert.Contains(StarterHeading, Texts(), "STARTER PACK and ONE TIME ONLY beside the revive offer");
                Assert.IsNotNull(ButtonStarting("BUY 300 GEMS"), "its one-price buy button");
                Assert.IsNotNull(ButtonStarting("REVIVE: "), "a short balance: revive names the store price");
                Assert.Contains(OfferLabels.StarterPackLine, Texts(), "the revive button would buy it, and the line under it says so (D-037-AC-04)");
                Assert.IsTrue(wallet.StarterPackOffered, "recorded as shown");
                yield return Audit("revive panel with the Starter Pack", findings);

                Deliver(store, "playmode-starter-1");
                yield return null;
                Assert.AreEqual(300, wallet.Balance, "the store delivered the Starter Pack");
                Assert.IsFalse(Texts().Contains(StarterHeading), "bought: the row goes");
                Assert.IsNotNull(ButtonStarting("REVIVE FOR 30 GEMS"), "and the revive offer stays, now paid in gems");
                yield return CloseRevive();

                // Shown once: a later panel, the pack still unbought, carries only the revive offer.
                wallet.Replace(new WalletState { balance = 0, starterGranted = true, starterPackOffered = true });
                OfferRevive();
                yield return null;
                Assert.IsFalse(Texts().Contains(StarterHeading), "the Starter Pack row is shown only once");
                Assert.Contains(OfferLabels.StarterPackLine, Texts(), "the revive button still buys it while it is on sale");
                yield return Audit("revive panel after the Starter Pack was shown", findings);
                yield return CloseRevive();
            }
            finally
            {
                Time.timeScale = 1f;
                wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-037-AC-06: with the Starter Pack on sale the gem store, showing STARTER PACK: 300 GEMS in the smallest pack's slot, passes the screen audit on every screen shape, and the smallest pack returns once it is bought; the revive panel's audit runs in its own test")]
        public IEnumerator GemStoreShowsTheStarterPackInTheSmallestSlot()
        {
            var store = Store();
            var wallet = Get<GemWallet>(store, "Wallet");
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var findings = new List<Finding>();
            Component panel = null;
            try
            {
                wallet.Replace(new WalletState { balance = 0, starterGranted = true });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Start");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                yield return UntilOnSale(store);

                var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
                var menu = UnityEngine.Object.FindFirstObjectByType(GameType("LoadLv"));
                panel = (Component)GameType("TreadShredSettingsPanel").GetMethod("Show").Invoke(null, new object[] { menu, font });
                yield return null;
                Assert.IsNotNull(ButtonStarting(OfferLabels.StarterPackTitle + ": 300 GEMS"), "in the smallest pack's slot");
                Assert.IsNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack waits while the Starter Pack is on sale");
                Assert.IsNotNull(ButtonStarting("BUY 550 GEMS"), "the other packs stay");
                yield return Audit("gem store with the Starter Pack", findings);

                Deliver(store, "playmode-starter-2");
                yield return null;
                Assert.IsNull(ButtonStarting(OfferLabels.StarterPackTitle), "bought: no longer offered");
                Assert.IsNotNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack returns");
            }
            finally
            {
                if (panel != null)
                    UnityEngine.Object.Destroy(panel.gameObject);
                wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            yield return null;
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
