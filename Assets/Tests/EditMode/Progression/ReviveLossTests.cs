using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ReviveLossTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-020-AC-05: with a SCORE to lose the revive offer names it, revives AND KEEPS IT, and the way out reads END RUN: SCORE BACK TO 0; at 0 it reads as before")]
        public void ReviveNamesTheRunAtStake()
        {
            Assert.AreEqual("YOUR 12,400 SCORE RUN ENDS HERE", OfferLabels.ReviveHeadline(12400));
            Assert.AreEqual("REVIVE AND KEEP IT", OfferLabels.ReviveAction(12400));
            Assert.AreEqual("END RUN: SCORE BACK TO 0", OfferLabels.ReviveDecline(12400));
            Assert.AreEqual("REVIVE AND KEEP IT FOR 30 GEMS", OfferLabels.ActionButton(OfferLabels.ReviveAction(12400), new OfferPlan(30, null), null),
                "the D-009-AC-01 button format carries the new action");

            Assert.AreEqual("REVIVE NOW WITH FULL ARMOR", OfferLabels.ReviveHeadline(0));
            Assert.AreEqual("REVIVE", OfferLabels.ReviveAction(0));
            Assert.AreEqual("NO THANKS", OfferLabels.ReviveDecline(0), "nothing to lose, nothing to warn about");
        }

        [Test(Description = "D-020-AC-05: the revive panel takes the run's live SCORE when the tank goes down and labels its headline, button and way out from it in both layouts")]
        public void RevivePanelUsesTheLiveScore()
        {
            var panel = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("panel.runScore = ManagerScore.instance != null ? ManagerScore.instance.LiveRunScore : 0;", panel);
            Assert.AreEqual(2, Count(panel, "OfferLabels.ReviveHeadline(runScore)"), "the plain and Starter Pack layouts");
            Assert.AreEqual(2, Count(panel, "OfferLabels.ReviveDecline(runScore)"));
            StringAssert.Contains("OfferLabels.ActionButton(OfferLabels.ReviveAction(runScore), plan, price)", panel);
            StringAssert.DoesNotContain("\"NO THANKS\"", panel);
        }

        [Test(Description = "D-020-AC-01: the revive offer stays up for 12 seconds (R-REVIVE offer_seconds)")]
        public void OfferLastsTwelveSeconds()
        {
            Assert.AreEqual(12f, ProgressionRules.ReviveOfferSeconds);
        }

        [Test(Description = "D-009-AC-05: after a lost mission Armored Redeploy names the SCORE it keeps and RETRY says END RUN: SCORE BACK TO 0; a run with nothing banked reads as before")]
        public void FailureScreenNamesTheScoreKept()
        {
            var run = new ScoreRun();
            run.Start();
            run.Complete(true, 3, DifficultyTier.Recruit);
            var kept = run.Banked;
            run.Complete(false, 2, DifficultyTier.Recruit);
            Assert.AreEqual(kept, run.Resumable, "a redeploy carries back the SCORE banked before the lost mission");
            Assert.AreEqual(0, run.Banked);

            Assert.AreEqual("1 EXTRA HIT. KEEP YOUR 800 SCORE", OfferLabels.RedeployKeeps(800));
            Assert.AreEqual("END RUN: SCORE BACK TO 0", OfferLabels.RetryCaptionFor(800));
            Assert.AreEqual("SCORE STARTS OVER", OfferLabels.RetryCaptionFor(0), "D-032-AC-06 with nothing to keep");

            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("OfferLabels.RedeployKeeps(TreadShredScores.Run.Resumable)", results);
            StringAssert.Contains("OfferLabels.RetryCaptionFor(TreadShredScores.Run.Resumable)", results);
        }

        private static int Count(string text, string part)
        {
            var count = 0;
            for (var at = text.IndexOf(part, System.StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + part.Length, System.StringComparison.Ordinal))
                count++;
            return count;
        }
    }
}
