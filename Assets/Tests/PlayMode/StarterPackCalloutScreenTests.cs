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
    // D-037-AC-04 on the real result screen: once the Starter Pack's interstitial has opened by itself, the result screen
    // sells it with a pulsing callout. StarterPackWindowSetup ends the window for every other screen test; these reopen it
    // and restore the wallet and progress after.
    public sealed class StarterPackCalloutScreenTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const string ProgressionKey = "TreadShredProgression";
        private static TreadShredStore Store() => TreadShredStore.Ensure();

        private static GemWallet Wallet => Store().Wallet;

        private static PlayerProgression Progression =>
            TreadShredProgress.Ensure().Progression;

        // A player in the window whose interstitial has already opened by itself, the window opened `minutesAgo` minutes ago.
        private static void SeedShown(double minutesAgo)
        {
            Wallet.Replace(new WalletState
            {
                balance = 100,
                starterGranted = true,
                starterPackWindowStartTicks = DateTime.UtcNow.AddMinutes(-minutesAgo).Ticks
            });
            Wallet.MarkStarterPackShown();
        }

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static Button Callout() => ButtonStarting(OfferLabels.StarterPackTitle + "\n");

        private static bool InterstitialOpen() => GameObject.Find("TreadShredStarterOffer") != null;

        private static IEnumerator WinLv1()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            var manager = UnityEngine.Object.FindFirstObjectByType<ManagerScore>();
            Assert.IsNotNull(manager, "the mission has its score manager");
            manager.CountEmnemy = 1;
            manager.Victory();
            yield return new WaitForSecondsRealtime(3.5f);
        }

        // Runs a test body, then closes whatever it left open, puts the wallet and progress back as they were and starts a
        // fresh run, so the victories here bank no SCORE into later tests (D-020-AC-05).
        private static IEnumerator Restoring(IEnumerator body)
        {
            var wallet = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            var progress = PlayerPrefs.GetString(ProgressionKey, string.Empty);
            try
            {
                while (body.MoveNext())
                    yield return body.Current;
            }
            finally
            {
                Time.timeScale = 1f;
                var open = GameObject.Find("TreadShredStarterOffer");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Wallet.Replace(JsonUtility.FromJson<WalletState>(wallet));
                Progression.Replace(string.IsNullOrEmpty(progress) ? new ProgressionState() : JsonUtility.FromJson<ProgressionState>(progress));
                TreadShredScores.Run.Start();
            }
        }

        [UnityTest(Description = "D-037-AC-04: once the interstitial has opened by itself, the victory screen shows only the STARTER PACK callout with its countdown, and a tap opens the interstitial with the free test drive")]
        public IEnumerator CalloutOpensTheInterstitial()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                SeedShown(0.1);
                yield return WinLv1();
                Assert.IsFalse(InterstitialOpen(), "never by itself again");
                Assert.IsNotNull(Callout(), "the victory screen's callout");
                StringAssert.StartsWith(OfferLabels.StarterPackTitle + "\nENDS IN 14:", Callout().GetComponentInChildren<Text>().text, "with its countdown");
                Callout().onClick.Invoke();
                yield return null;
                Assert.IsTrue(InterstitialOpen(), "a tap opens it");
                for (var waited = 0f; ButtonStarting("NOT NOW") == null && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                Assert.IsNotNull(ButtonStarting(WarChest.TestDriveButton), "with the free test drive of the mission just played");
                Assert.IsNotNull(ButtonStarting("GET THE STARTER PACK"));
            }
        }

        [UnityTest(Description = "D-037-AC-06: the victory screen with the Starter Pack callout passes the screen audit on every supported screen shape")]
        public IEnumerator CalloutPassesTheAudit()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                SeedShown(0.1);
                yield return WinLv1();
                Assert.IsNotNull(Callout());
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "victory screen with the Starter Pack callout"));
                }
            }
        }

        [UnityTest(Description = "D-037-AC-04: the result screen's callout pulses faintly with the window fresh and swells far harder as it ends, and leaves the moment it ends (D-037-AC-07)")]
        public IEnumerator CalloutPulsesHarderNearTheEnd()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                SeedShown(0.1);
                yield return WinLv1();
                var early = 0f;
                yield return MaxSwell(2.2f, swell => early = swell);
                SeedShown(15 - 6.0 / 60);
                var late = 0f;
                yield return MaxSwell(1.2f, swell => late = swell);
                Assert.Less(early, 1.02f, "subtle with the window fresh");
                Assert.Greater(late, early + 0.02f, "far harder with seconds left");
                yield return new WaitForSecondsRealtime(5.5f);
                Assert.IsNull(Callout(), "gone when the window ends");
            }

            IEnumerator MaxSwell(float seconds, Action<float> result)
            {
                var max = 0f;
                for (var waited = 0f; waited < seconds; waited += Time.unscaledDeltaTime)
                {
                    Assert.IsNotNull(Callout(), "the callout stays while on sale");
                    max = Math.Max(max, Callout().transform.localScale.x);
                    yield return null;
                }
                result(max);
            }
        }
    }
}
