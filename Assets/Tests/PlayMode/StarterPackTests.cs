using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-037 on the real revive panel, interstitial and gem store, with the editor's test store offering the Starter Pack.
    // StarterPackWindowSetup ends the window for every other screen test; these reopen it and restore the wallet after.
    public sealed class StarterPackTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static T Get<T>(object target, string property) => (T)target.GetType().GetProperty(property).GetValue(target);

        private static GemWallet Wallet => Get<GemWallet>(Store(), "Wallet");

        // The wallet as a player in the window has it: gems as given, the window opened `minutesAgo` minutes ago.
        private static void Seed(int gems, double minutesAgo) => Wallet.Replace(new WalletState
        {
            balance = gems,
            starterGranted = true,
            starterPackWindowStartTicks = DateTime.UtcNow.AddMinutes(-minutesAgo).Ticks
        });

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

        private static bool InterstitialOpen() => GameObject.Find("TreadShredStarterOffer") != null;

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; scene != "Start" && Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static void Deliver(string orderId) =>
            Get<PurchaseFlow>(Store(), "Purchases").OrderDelivered(GemCatalog.StarterPack.ProductId, orderId, out _);

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

        private static IEnumerator Win()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            Assert.IsNotNull(manager, "the mission has its score manager");
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        // Runs a test body, then closes whatever it left open and puts the wallet back as it was.
        private static IEnumerator Restoring(IEnumerator body)
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                while (body.MoveNext())
                    yield return body.Current;
            }
            finally
            {
                Time.timeScale = 1f;
                foreach (var name in new[] { "TreadShredRevive", "TreadShredStarterOffer", "TreadShredSettings" })
                {
                    var open = GameObject.Find(name);
                    if (open != null)
                        UnityEngine.Object.Destroy(open);
                }
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }

        [UnityTest(Description = "D-037-AC-03: while on sale every revive panel leads with STARTER PACK, 300 GEMS + THIS REVIVE FREE, the countdown and REVIVE + 300 GEMS; short of gems the plain revive waits, with gems it sits below, and bought or ended the panel offers the plain revive")]
        public IEnumerator RevivePanelLeadsWithTheStarterPack()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                Seed(0, 0.1);
                yield return Load("Lv1");
                Assert.IsTrue(Get<bool>(Store(), "StarterPackOnSale"), "the editor's test store offers the Starter Pack's product");
                // A fresh run (SCORE 0), so the plain revive reads as at the start of a run whatever earlier tests banked (D-020-AC-05).
                var run = GameType("TreadShredScores").GetProperty("Run").GetValue(null);
                run.GetType().GetMethod("Start").Invoke(run, null);

                OfferRevive();
                yield return null;
                var texts = Texts();
                Assert.Contains(OfferLabels.StarterPackTitle, texts);
                Assert.Contains(OfferLabels.StarterPackReviveHeadline, texts);
                Assert.Contains(OfferLabels.StarterPackValue, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("ENDS IN 14:", StringComparison.Ordinal) || t == "ENDS IN 15:00"), "the live countdown");
                Assert.IsNotNull(ButtonStarting("REVIVE + 300 GEMS"), "the main button buys it with the revive included");
                Assert.IsNull(ButtonStarting("REVIVE: "), "short of gems the plain revive would only buy the same pack, so it waits");
                yield return Audit("revive panel led by the Starter Pack", findings);

                Deliver("playmode-starter-1");
                yield return null;
                Assert.AreEqual(300, Wallet.Balance, "the store delivered the Starter Pack");
                Assert.IsFalse(Texts().Contains(OfferLabels.StarterPackReviveHeadline), "bought: the Starter Pack leaves the panel");
                Assert.IsNotNull(ButtonStarting("REVIVE FOR 30 GEMS"), "and the revive offer remains, paid in gems");
                yield return CloseRevive();

                Seed(100, 1);
                OfferRevive();
                yield return null;
                Assert.IsNotNull(ButtonStarting("REVIVE + 300 GEMS"));
                Assert.IsNotNull(ButtonStarting("REVIVE FOR 30 GEMS"), "gems cover the revive: the plain revive sits below");
                yield return Audit("revive panel led by the Starter Pack, gems covering the revive", findings);
                yield return CloseRevive();

                Seed(0, 16);
                OfferRevive();
                yield return null;
                Assert.IsFalse(Texts().Contains(OfferLabels.StarterPackTitle), "window over: the plain revive panel");
                Assert.IsNotNull(ButtonStarting("REVIVE: "));
                yield return CloseRevive();
            }
        }

        [UnityTest(Description = "D-037-AC-04: while on sale the Starter Pack interstitial opens over the victory screen and on reaching the start screen, NOT NOW closes it, and a delivered purchase closes it")]
        public IEnumerator InterstitialOpensOverResultsAndStart()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Seed(100, 0.1);
                yield return Load("Lv1");
                yield return Win();
                Assert.IsTrue(InterstitialOpen(), "over the victory screen");
                var texts = Texts();
                Assert.Contains(OfferLabels.StarterPackTitle, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith(OfferLabels.StarterPackLimit + "  |  ENDS IN ", StringComparison.Ordinal)), "ONE TIME ONLY and the countdown");
                Assert.IsTrue(texts.Any(t => t.StartsWith("300 GEMS FOR ", StringComparison.Ordinal)), "its price");
                Assert.Contains(OfferLabels.StarterPackValue, texts);
                Assert.Contains(OfferLabels.StarterPackBuys, texts);
                Assert.IsNotNull(ButtonStarting("BUY 300 GEMS"));
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsFalse(InterstitialOpen(), "NOT NOW closes it");

                yield return Load("Start");
                Assert.IsTrue(InterstitialOpen(), "on reaching the start screen");
                Deliver("playmode-starter-2");
                yield return null;
                yield return null;
                Assert.IsFalse(InterstitialOpen(), "bought: it closes");
            }
        }

        [UnityTest(Description = "D-037-AC-06: with the Starter Pack on sale the interstitial over the victory and start screens and the gem store showing STARTER PACK: 300 GEMS pass the screen audit on every screen shape; the revive panel's audits run in its own test")]
        public IEnumerator StarterPackScreensPassTheAudit()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                Seed(100, 0.1);
                yield return Load("Lv1");
                yield return Win();
                Assert.IsTrue(InterstitialOpen());
                yield return Audit("Starter Pack interstitial over victory", findings);

                yield return Load("Start");
                Assert.IsTrue(InterstitialOpen());
                yield return Audit("Starter Pack interstitial over the start screen", findings);
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;

                var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
                var menu = UnityEngine.Object.FindFirstObjectByType(GameType("LoadLv"));
                GameType("TreadShredSettingsPanel").GetMethod("Show").Invoke(null, new object[] { menu, font });
                yield return null;
                Assert.IsNotNull(ButtonStarting(OfferLabels.StarterPackTitle + ": 300 GEMS"), "in the smallest pack's slot");
                Assert.IsNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack waits while the Starter Pack is on sale");
                yield return Audit("gem store with the Starter Pack", findings);

                Deliver("playmode-starter-3");
                yield return null;
                Assert.IsNull(ButtonStarting(OfferLabels.StarterPackTitle), "bought: no longer offered");
                Assert.IsNotNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack returns");
            }
        }

        [UnityTest(Description = "D-037-AC-07: when the countdown reaches zero the open interstitial closes and the open revive panel drops the Starter Pack for the plain revive, at once")]
        public IEnumerator CountdownEndsTheOfferOnOpenScreens()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Seed(0, 15 - 8.0 / 60);
                yield return Load("Start");
                Assert.IsTrue(InterstitialOpen(), "a few seconds left");
                yield return new WaitForSecondsRealtime(7f);
                Assert.IsFalse(InterstitialOpen(), "closed when the countdown ended");
                Assert.IsFalse(Get<bool>(Store(), "StarterPackOnSale"));

                yield return Load("Lv1");
                Seed(0, 15 - 2.0 / 60);
                OfferRevive();
                yield return null;
                Assert.IsTrue(Texts().Contains(OfferLabels.StarterPackTitle), "on sale for two more seconds");
                yield return new WaitForSecondsRealtime(3f);
                Assert.IsFalse(Texts().Contains(OfferLabels.StarterPackTitle), "the Starter Pack left the open panel");
                Assert.IsNotNull(ButtonStarting("REVIVE: "), "and the plain revive took its place");
                yield return CloseRevive();
            }
        }
    }
}
