using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Analytics.Tests
{
    public sealed class AnalyticsLaunchTests
    {
        [Test(Description = "D-021-AC-06: Analytics starts as soon as Firebase is ready at launch, before any game event, so every install records its first open on iOS as on Android")]
        public void AnalyticsStartsAtLaunch()
        {
            var script = File.ReadAllText(Path.Combine(Application.dataPath, "Script", "Game", "TreadShredAnalytics.cs"));
            var initialize = script.IndexOf("[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]", System.StringComparison.Ordinal);
            var start = script.IndexOf("FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);", System.StringComparison.Ordinal);
            var ready = script.IndexOf("dispatcher.MarkReady();", System.StringComparison.Ordinal);
            Assert.Greater(initialize, -1, "Analytics initializes before the first scene loads");
            Assert.Greater(start, initialize, "collection is started inside the launch initializer");
            Assert.Less(start, ready, "before any queued game event is sent");
            StringAssert.DoesNotContain("SetAnalyticsCollectionEnabled(false)", script, "nothing turns it off again");
        }
    }
}
