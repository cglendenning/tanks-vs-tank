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
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-037 on the real revive panel, interstitial and gem store, with the editor's test store offering the Starter Pack.
    // StarterPackWindowSetup ends the window for every other screen test; these reopen it and restore the wallet after.
    public sealed class StarterPackTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const string ProgressionKey = "TreadShredProgression";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Store() => GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);

        private static T Get<T>(object target, string property) => (T)target.GetType().GetProperty(property).GetValue(target);

        private static GemWallet Wallet => Get<GemWallet>(Store(), "Wallet");

        private static PlayerProgression Progression =>
            Get<PlayerProgression>(GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null), "Progression");

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

        private static Button Callout() => ButtonStarting(OfferLabels.StarterPackTitle + "\n");

        // The stack lands item by item; NOT NOW appears once the last has landed.
        private static IEnumerator Revealed()
        {
            for (var waited = 0f; waited < 10f; waited += 0.25f)
            {
                var offer = UnityEngine.Object.FindFirstObjectByType(GameType("TreadShredStarterOffer"));
                if (offer != null && Get<bool>(offer, "Revealed"))
                    yield break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Assert.Fail("the Starter Pack's reveal never finished");
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; scene != "Start" && Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // As the store delivers it: credited, then its gems become its supply before the order is confirmed (D-037-AC-01).
        private static void Deliver(string orderId)
        {
            Get<PurchaseFlow>(Store(), "Purchases").OrderDelivered(GemCatalog.StarterPack.ProductId, orderId, out _);
            Progression.EquipStarterPack();
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

        private static IEnumerator Win()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            Assert.IsNotNull(manager, "the mission has its score manager");
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        // Runs a test body, then closes whatever it left open and puts the wallet and progress back as they were.
        private static IEnumerator Restoring(IEnumerator body)
        {
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var progress = PlayerPrefs.GetString(ProgressionKey, string.Empty);
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
                Progression.Replace(string.IsNullOrEmpty(progress) ? new ProgressionState() : JsonUtility.FromJson<ProgressionState>(progress));
            }
        }

        [UnityTest(Description = "D-037-AC-03: while on sale every revive panel leads with STARTER PACK, REVIVE FREE + the armor it adds, PLUS the rest of the pack, the countdown and REVIVE + STARTER PACK; short of gems the plain revive waits, with gems it sits below, and bought or ended the panel offers the plain revive")]
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
                Assert.IsTrue(texts.Any(t => t.StartsWith("REVIVE FREE + ", StringComparison.Ordinal)), "the revive it includes and the armor it adds");
                Assert.Contains(StarterPackPitch.ReviveAlternative, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("ENDS IN 14:", StringComparison.Ordinal) || t == "ENDS IN 15:00"), "the live countdown");
                Assert.IsNotNull(ButtonStarting("REVIVE + STARTER PACK"), "the main button buys it with the revive included");
                Assert.IsNull(ButtonStarting("REVIVE: "), "short of gems the plain revive would only buy the same pack, so it waits");
                yield return Audit("revive panel led by the Starter Pack", findings);

                Deliver("playmode-starter-1");
                yield return null;
                Assert.AreEqual((0, 1), (Wallet.Balance, Progression.Supply(SupplyTrack.Seeker)), "delivered: its gems are now its supply");
                Assert.IsFalse(Texts().Any(t => t.StartsWith("REVIVE FREE + ", StringComparison.Ordinal)), "bought: the Starter Pack leaves the panel");
                Assert.IsNotNull(ButtonStarting("REVIVE: "), "and the plain revive offer remains");
                yield return CloseRevive();

                Seed(100, 1);
                OfferRevive();
                yield return null;
                Assert.IsNotNull(ButtonStarting("REVIVE + STARTER PACK"));
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

        [UnityTest(Description = "D-037-AC-04: the Starter Pack interstitial opens by itself once (here on reaching the start screen, with the purchase alone); after that the victory screen's STARTER PACK callout opens it, revealing its stack with the free test drive; NOT NOW closes it once revealed, and a delivered purchase closes it and the callout")]
        public IEnumerator InterstitialOpensOverResultsAndStart()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Seed(100, 0.1);
                yield return Load("Start");
                Assert.IsTrue(InterstitialOpen(), "by itself, the first time");
                yield return Revealed();
                Assert.IsNull(ButtonStarting(WarChest.TestDriveButton), "no mission to drive on the start screen");
                Assert.IsNotNull(ButtonStarting("GET THE STARTER PACK"));
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.IsFalse(InterstitialOpen(), "NOT NOW closes it");

                yield return Load("Lv1");
                yield return Win();
                Assert.IsFalse(InterstitialOpen(), "never by itself again");
                Callout().onClick.Invoke();
                yield return null;
                Assert.IsTrue(InterstitialOpen(), "the callout opens it over the victory screen");
                yield return Revealed();
                var texts = Texts();
                Assert.Contains(OfferLabels.StarterPackTitle, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith(OfferLabels.StarterPackLimit + "  |  ENDS IN ", StringComparison.Ordinal)), "ONE TIME ONLY and the countdown");
                foreach (var item in StarterPackPitch.Stack.Items)
                    Assert.Contains(SupplyStack.Line(item), texts);
                Assert.Contains(StarterPackPitch.Stack.WeaponsNote, texts);
                Assert.IsTrue(texts.Any(t => t.StartsWith("TOTAL VALUE ", StringComparison.Ordinal) || t.EndsWith("X THE GEAR FOR THE PRICE", StringComparison.Ordinal)), "what it is worth");
                Assert.IsTrue(texts.Any(t => t.StartsWith("YOURS ", StringComparison.Ordinal)));
                Assert.IsNotNull(ButtonStarting(WarChest.TestDriveButton), "over a mission the free test drive leads");
                Assert.IsNotNull(ButtonStarting("GET THE STARTER PACK"));
                Deliver("playmode-starter-2");
                yield return null;
                yield return null;
                Assert.IsFalse(InterstitialOpen(), "bought: it closes");
                Assert.IsNull(Callout(), "and the callout leaves");
            }
        }

        [UnityTest(Description = "D-037-AC-06: with the Starter Pack on sale the interstitial over the victory and start screens and the gem store showing STARTER PACK: <armor> + MORE pass the screen audit on every screen shape; the revive panel's audits run in its own test")]
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
                yield return Revealed();
                yield return Audit("Starter Pack interstitial over victory", findings);

                Seed(100, 0.1); // a fresh window, so the start screen opens it by itself
                yield return Load("Start");
                Assert.IsTrue(InterstitialOpen());
                yield return Revealed();
                yield return Audit("Starter Pack interstitial over the start screen", findings);
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;

                var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
                var menu = UnityEngine.Object.FindFirstObjectByType(GameType("LoadLv"));
                GameType("TreadShredSettingsPanel").GetMethod("Show").Invoke(null, new object[] { menu, font });
                yield return null;
                Assert.IsNotNull(ButtonStarting(OfferLabels.StarterPackTitle + ": "), "in the smallest pack's slot, sold by what it does");
                Assert.IsNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack waits while the Starter Pack is on sale");
                yield return Audit("gem store with the Starter Pack", findings);

                Deliver("playmode-starter-3");
                yield return null;
                Assert.IsNull(ButtonStarting(OfferLabels.StarterPackTitle), "bought: no longer offered");
                Assert.IsNotNull(ButtonStarting("BUY 100 GEMS"), "the smallest pack returns");
            }
        }

        [UnityTest(Description = "D-037-AC-08: TEST DRIVE IT FREE replays the mission just played with the whole Starter Pack on and nothing kept; when it ends the reveal returns as THAT WAS THE STARTER PACK with the countdown and the purchase alone, and closing it goes on to the next mission after a victory and replays the mission after a loss")]
        public IEnumerator TestDriveTheStarterPack()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Seed(100, 0.1);
                yield return Load("Lv1");
                yield return Win();
                yield return Drive("Lv1");
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                Assert.AreEqual("Lv2", SceneManager.GetActiveScene().name, "after a victory, on to the next mission");

                yield return Load("Lv1");
                var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
                manager.GetType().GetMethod("Failure", Members).Invoke(manager, null);
                yield return new WaitForSecondsRealtime(3.5f);
                Assert.IsFalse(InterstitialOpen(), "once shown, only the callout opens it");
                Callout().onClick.Invoke();
                yield return null;
                yield return Drive("Lv1");
                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                Assert.AreEqual("Lv1", SceneManager.GetActiveScene().name, "after a loss, the mission again");
                Assert.IsTrue(Get<bool>(Store(), "StarterPackOnSale"), "a drive buys nothing");
            }

            // From the result screen's interstitial: the drive, its end, and the reveal that follows.
            IEnumerator Drive(string mission)
            {
                Assert.IsTrue(InterstitialOpen());
                yield return Revealed();
                var armor = Progression.Supply(SupplyTrack.Armor);
                var seeker = Progression.Supply(SupplyTrack.Seeker);
                ButtonStarting(WarChest.TestDriveButton).onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                var drive = GameType("TreadShredTestDrive");
                Assert.IsTrue((bool)drive.GetProperty("Active").GetValue(null), "driving");
                Assert.AreEqual(mission, SceneManager.GetActiveScene().name, "the mission just played");
                Assert.Contains(StarterPackPitch.TestDriveNote, Texts());
                Assert.AreEqual((armor + 2, seeker + 1), (Progression.Supply(SupplyTrack.Armor), Progression.Supply(SupplyTrack.Seeker)), "the whole pack on");
                drive.GetMethod("Finish").Invoke(null, null);
                yield return null;
                Assert.AreEqual((armor, seeker), (Progression.Supply(SupplyTrack.Armor), Progression.Supply(SupplyTrack.Seeker)), "the gear goes with the drive");
                Assert.IsTrue(InterstitialOpen(), "the reveal returns");
                yield return Revealed();
                Assert.IsTrue(Texts().Any(t => t.StartsWith(StarterPackPitch.AfterDriveSubtitle + "  |  ENDS IN ", StringComparison.Ordinal)));
                Assert.IsNull(ButtonStarting(WarChest.TestDriveButton), "the purchase alone");
                Assert.IsNotNull(ButtonStarting("GET THE STARTER PACK"));
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
