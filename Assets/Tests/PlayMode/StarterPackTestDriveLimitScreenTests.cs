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
    // D-037-AC-08 on the real result screen: the second free test drive is the last; after it the Starter Pack's
    // interstitial offers the purchase alone. StarterPackWindowSetup ends the window for every other screen test; this
    // reopens it and restores the wallet, progress and run after.
    public sealed class StarterPackTestDriveLimitScreenTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const string ProgressionKey = "TreadShredProgression";
        private static TreadShredStore Store() => TreadShredStore.Ensure();

        private static GemWallet Wallet => Store().Wallet;

        private static PlayerProgression Progression =>
            TreadShredProgress.Ensure().Progression;

        // A player in a fresh window whose interstitial has opened by itself and who has taken `drives` test drives.
        private static void Seed(int drives)
        {
            Wallet.Replace(new WalletState
            {
                balance = 100,
                starterGranted = true,
                starterPackWindowStartTicks = DateTime.UtcNow.AddMinutes(-0.1).Ticks,
                starterPackShown = true,
                starterPackTestDrives = drives
            });
        }

        private static Button ButtonStarting(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

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

        // The callout opens the interstitial; waits until everything has landed.
        private static IEnumerator OpenFromCallout()
        {
            var callout = ButtonStarting(OfferLabels.StarterPackTitle + "\n");
            Assert.IsNotNull(callout, "the result screen's callout");
            callout.onClick.Invoke();
            for (var waited = 0f; ButtonStarting("NOT NOW") == null && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(InterstitialOpen());
        }

        // Runs a test body, then closes whatever it left open and puts the wallet, progress and run back.
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
                if (TreadShredTestDrive.Active)
                    TreadShredTestDrive.Finish();
                var open = GameObject.Find("TreadShredStarterOffer");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Time.timeScale = 1f;
                Wallet.Replace(JsonUtility.FromJson<WalletState>(wallet));
                Progression.Replace(string.IsNullOrEmpty(progress) ? new ProgressionState() : JsonUtility.FromJson<ProgressionState>(progress));
                TreadShredScores.Run.Start();
            }
        }

        [UnityTest(Description = "D-037-AC-08: after one test drive the Starter Pack offers one more, counted as it starts; after the second TEST DRIVE IT FREE is gone and the purchase stands alone")]
        public IEnumerator SecondTestDriveIsTheLast()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Seed(1);
                yield return WinLv1();
                yield return OpenFromCallout();
                var drive = ButtonStarting(WarChest.TestDriveButton);
                Assert.IsNotNull(drive, "one more drive after the first");
                drive.onClick.Invoke();
                yield return new WaitForSecondsRealtime(2f);
                Assert.IsTrue(TreadShredTestDrive.Active, "driving");
                Assert.AreEqual(2, Wallet.StarterPackTestDrives, "counted as it starts");
                TreadShredTestDrive.Finish();
                yield return null;
                Assert.IsTrue(InterstitialOpen(), "the reveal returns after the drive");
                UnityEngine.Object.Destroy(GameObject.Find("TreadShredStarterOffer"));
                yield return null;

                yield return WinLv1();
                yield return OpenFromCallout();
                Assert.IsNull(ButtonStarting(WarChest.TestDriveButton), "two drives taken: no more");
                Assert.IsNotNull(ButtonStarting("GET THE STARTER PACK"), "the purchase stands alone");
            }
        }
    }
}
