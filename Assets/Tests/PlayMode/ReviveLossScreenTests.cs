using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-020-AC-05 and D-009-AC-05 on the real revive panel and failure screen, with a SCORE run at stake.
    public sealed class ReviveLossScreenTests
    {
        private const string WalletKey = "TreadShredGemWallet";
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static GemWallet Wallet()
        {
            var store = GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);
            return (GemWallet)store.GetType().GetProperty("Wallet").GetValue(store);
        }

        private static object Manager() => UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));

        // The run as it stands in the game: the score so far, and what a won mission banks.
        private static object Run() => GameType("TreadShredScores").GetProperty("Run").GetValue(null);

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // A run that has already won a mission: 3 kills and the clear banked, then `kills` more in this one.
        private static void RunAtStake(int kills)
        {
            var tier = Enum.ToObject(GameType("TreadShred.Progression.DifficultyTier"), 0);
            var run = Run();
            run.GetType().GetMethod("Start").Invoke(run, null);
            run.GetType().GetMethod("Complete").Invoke(run, new[] { (object)true, 3, tier });
            Manager().GetType().GetField("CurDieEmnemy", Members).SetValue(Manager(), kills);
        }

        private static void OfferRevive()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            Assert.IsTrue((bool)GameType("TreadShredRevivePanel").GetMethod("TryOffer").Invoke(null, new object[] { player }));
        }

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        // Runs a test body, then closes the revive panel, starts a fresh run and puts the wallet back as it was.
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
                var open = GameObject.Find("TreadShredRevive");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Run().GetType().GetMethod("Start").Invoke(Run(), null);
                Wallet().Replace(JsonUtility.FromJson<WalletState>(saved));
            }
        }

        private static readonly Regex LossLine = new Regex(@"^YOUR [\d,]+ SCORE RUN ENDS HERE$");

        [UnityTest(Description = "D-020-AC-05: with a run at stake the revive panel names its SCORE, offers REVIVE AND KEEP IT and END RUN: SCORE BACK TO 0, on every screen shape")]
        public IEnumerator RevivePanelNamesTheRun()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                yield return Load("Lv1");
                RunAtStake(2);
                OfferRevive();
                yield return null;
                var texts = Texts();
                Assert.IsTrue(texts.Any(LossLine.IsMatch), "the loss line: " + string.Join(" | ", texts));
                Assert.IsTrue(texts.Any(t => t.StartsWith("REVIVE AND KEEP IT", StringComparison.Ordinal)), string.Join(" | ", texts));
                CollectionAssert.Contains(texts, "END RUN: SCORE BACK TO 0");
                CollectionAssert.DoesNotContain(texts, "NO THANKS");
                yield return Audit("REVIVE, RUN AT STAKE", findings);
            }
        }

        [UnityTest(Description = "D-020-AC-05: while the Starter Pack leads the revive panel, the plain revive below it still names the run at stake, on every screen shape")]
        public IEnumerator StarterLayoutNamesTheRun()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                Wallet().Replace(new WalletState
                {
                    balance = 0,
                    starterGranted = true,
                    starterPackWindowStartTicks = DateTime.UtcNow.AddMinutes(-0.1).Ticks
                });
                yield return Load("Lv1");
                RunAtStake(2);
                OfferRevive();
                yield return null;
                var texts = Texts();
                CollectionAssert.Contains(texts, OfferLabels.StarterPackTitle, "the Starter Pack leads");
                Assert.IsTrue(texts.Any(LossLine.IsMatch), string.Join(" | ", texts));
                CollectionAssert.Contains(texts, "END RUN: SCORE BACK TO 0");
                yield return Audit("REVIVE WITH STARTER PACK, RUN AT STAKE", findings);
            }
        }

        [UnityTest(Description = "D-009-AC-05: after a lost mission in a run, Armored Redeploy names the SCORE it keeps and RETRY reads END RUN: SCORE BACK TO 0, on every screen shape")]
        public IEnumerator FailureScreenNamesTheScoreKept()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                yield return Load("Lv1");
                RunAtStake(1);
                Manager().GetType().GetMethod("Failure", Members).Invoke(Manager(), null);
                yield return new WaitForSecondsRealtime(3.5f);
                var texts = Texts();
                Assert.IsTrue(texts.Any(t => Regex.IsMatch(t, @"^1 EXTRA HIT\. KEEP YOUR [\d,]+ SCORE$")), string.Join(" | ", texts));
                CollectionAssert.Contains(texts, "END RUN: SCORE BACK TO 0");
                yield return Audit("FAILURE, RUN AT STAKE", findings);
            }
        }
    }
}
