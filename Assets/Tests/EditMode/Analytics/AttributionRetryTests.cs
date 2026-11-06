using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class AttributionRetryTests
    {
        [Test(Description = "D-021-AC-05: the game keeps asking Tenjin until it credits a network, since organic or no answer may still become AppLovin")]
        public void SettledOnlyByANetwork()
        {
            Assert.IsFalse(Attribution.Settled(Attribution.FromTenjin("", "", "")), "Tenjin has not credited the install yet");
            Assert.IsFalse(Attribution.Settled(Attribution.FromTenjin("organic", "", "")), "the click may not be matched yet");
            Assert.IsTrue(Attribution.Settled(Attribution.FromTenjin("applovin", "TS Android Test 1", "1")));
        }

        [Test(Description = "D-021-AC-05: an install credited minutes after the first launch is still tagged in that session: the game asks again after launch and on every return")]
        public void AsksAgainAfterLaunchAndOnReturn()
        {
            var delays = Attribution.AskAgainAfterSeconds;
            Assert.That(delays, Is.Ordered.Ascending);
            Assert.LessOrEqual(delays.First(), 30f, "the first retry comes while a new player is still in the first mission");
            Assert.GreaterOrEqual(delays.Sum(), 600f, "retries cover the first ten minutes of play");

            var game = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/Script/Game/TreadShredAttribution.cs"));
            StringAssert.Contains("StartCoroutine(AskAgain())", game);
            StringAssert.Contains("Run(\"resume\", Platform.Resume);\n        if (!settled)\n            Run(\"attribution\", Platform.RequestAttribution);", game);
            StringAssert.Contains("settled = Attribution.Settled(acquisition);", game);
            StringAssert.Contains("public static void RequestAttribution() => Plugin.CallStatic(\"requestAttribution\");", game);
        }
    }
}
