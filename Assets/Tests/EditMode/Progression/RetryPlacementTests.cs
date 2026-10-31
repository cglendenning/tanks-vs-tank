using System;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class RetryPlacementTests
    {
        private static string Method(string source, string signature)
        {
            var body = source.Substring(source.IndexOf(signature, StringComparison.Ordinal));
            return body.Substring(0, body.IndexOf("\n    }", StringComparison.Ordinal));
        }

        [Test(Description = "D-032-AC-06: the victory screen offers no RETRY, and the failure screen's RETRY says SCORE STARTS OVER")]
        public void RetryOnlyOnFailureWithItsCost()
        {
            var results = File.ReadAllText(Path.Combine(Application.dataPath, "Script", "ManagerScore/ManagerScore.cs"));
            StringAssert.Contains("ArrangeRetry(true);", Method(results, "private void BuildVictoryRewardActions()"));
            StringAssert.Contains("ArrangeRetry(false);", Method(results, "private void BuildFailureRewardActions()"));
            var arrange = Method(results, "private void ArrangeRetry(bool won)");
            StringAssert.Contains("retry.gameObject.SetActive(false);", arrange);
            StringAssert.Contains("OfferLabels.RetryCaption", arrange);
            Assert.AreEqual("SCORE STARTS OVER", OfferLabels.RetryCaption);
        }
    }
}
