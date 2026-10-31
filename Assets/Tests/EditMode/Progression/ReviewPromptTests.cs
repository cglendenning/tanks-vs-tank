using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ReviewPromptTests
    {
        // Persists a copy, like PlayerPrefs JSON, so a reload sees only what was saved.
        private sealed class CopyingStore : IReviewPromptStore
        {
            private string saved;
            public ReviewPromptState Load() => saved == null ? null : JsonUtility.FromJson<ReviewPromptState>(saved);
            public void Save(ReviewPromptState state) => saved = JsonUtility.ToJson(state);
        }

        private static readonly DateTime Day1 = new DateTime(2026, 11, 2, 19, 0, 0);
        private static readonly DateTime Utc = new DateTime(2026, 11, 3, 3, 0, 0, DateTimeKind.Utc);

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        // Real play: missions on two days, six wins, twenty minutes in missions.
        private static ReviewPrompt Played(IReviewPromptStore store, int days = 2, int wins = 6, double minutes = 20)
        {
            var prompt = new ReviewPrompt(store);
            for (var day = 0; day < days; day++)
                prompt.MissionPlayed(Day1.AddDays(day));
            prompt.AddActiveTime(minutes * 60);
            for (var win = 0; win < wins; win++)
                prompt.MissionEnded(true);
            return prompt;
        }

        [Test(Description = "D-033-AC-02: a request needs missions on two local days, six missions won and twenty minutes of active mission time")]
        public void RequestNeedsRealPlay()
        {
            Assert.IsTrue(Played(new CopyingStore()).MayRequest(Utc, "1.9.1", false));
            Assert.IsFalse(Played(new CopyingStore(), days: 1).MayRequest(Utc, "1.9.1", false), "first day only");
            Assert.IsFalse(Played(new CopyingStore(), wins: 5).MayRequest(Utc, "1.9.1", false), "five wins");
            Assert.IsFalse(Played(new CopyingStore(), minutes: 19.9).MayRequest(Utc, "1.9.1", false), "under twenty minutes");
            Assert.AreEqual((2, 6, 20), (ReviewRules.MinPlayDays, ReviewRules.MinMissionsWon, ReviewRules.MinActivePlayMinutes), "R-REVIEW-PROMPT");
        }

        [Test(Description = "D-033-AC-02: each local calendar day counts once, and losses are not wins")]
        public void DaysAndWinsAreCountedHonestly()
        {
            var prompt = new ReviewPrompt(new CopyingStore());
            prompt.MissionPlayed(Day1);
            prompt.MissionPlayed(Day1.AddHours(3));
            prompt.MissionEnded(false);
            prompt.MissionEnded(true);
            prompt.AddActiveTime(-5);
            Assert.AreEqual((1, 1, 0.0), (prompt.PlayDays, prompt.MissionsWon, prompt.ActiveMinutes));
            prompt.MissionPlayed(Day1.AddDays(1));
            Assert.AreEqual(2, prompt.PlayDays);
        }

        [Test(Description = "D-033-AC-03: no request while a purchase is in progress, and only a victory screen asks, after the delay")]
        public void AsksOnlyOnAVictoryScreen()
        {
            Assert.IsFalse(Played(new CopyingStore()).MayRequest(Utc, "1.9.1", purchaseInProgress: true));
            var results = Read("Script/ManagerScore/ManagerScore.cs");
            var victory = results.Substring(results.IndexOf("private void BuildVictoryRewardActions()", StringComparison.Ordinal));
            victory = victory.Substring(0, victory.IndexOf("\n    }", StringComparison.Ordinal));
            StringAssert.Contains("TreadShredReviewPrompt.VictoryScreenOpened();", victory);
            Assert.AreEqual(1, results.Split(new[] { "VictoryScreenOpened(" }, StringSplitOptions.None).Length - 1, "never from a loss");
            var bridge = Read("Script/Store/TreadShredReviewPrompt.cs");
            StringAssert.Contains("new WaitForSecondsRealtime(ReviewRules.ResultScreenDelaySeconds)", bridge);
            StringAssert.Contains("if (SceneManager.GetActiveScene().handle != scene)", bridge);
            StringAssert.Contains("TreadShredStore.Ensure().PurchaseInProgress", bridge);
            Assert.AreEqual(1.5f, ReviewRules.ResultScreenDelaySeconds);
        }

        [Test(Description = "D-033-AC-04: one request per app version, 120 days apart, counted once sent, and remembered across launches")]
        public void OncePerVersionAndFarApart()
        {
            var store = new CopyingStore();
            var prompt = Played(store);
            prompt.RequestSent(Utc, "1.9.1");

            var relaunched = new ReviewPrompt(store);
            Assert.IsFalse(relaunched.MayRequest(Utc.AddDays(200), "1.9.1", false), "once per version");
            Assert.IsFalse(relaunched.MayRequest(Utc.AddDays(119), "1.10.0", false), "120 days apart");
            Assert.IsTrue(relaunched.MayRequest(Utc.AddDays(120), "1.10.0", false));
            Assert.AreEqual((1, 120), (ReviewRules.MaxRequestsPerAppVersion, ReviewRules.MinDaysBetweenRequests));
        }

        [Test(Description = "D-033-AC-01: the request goes only to StoreKit or Google Play In-App Review, and the game draws nothing of its own for it")]
        public void OnlyTheNativePrompt()
        {
            var bridge = Read("Script/Store/TreadShredReviewPrompt.cs");
            StringAssert.Contains("UnityEngine.iOS.Device.RequestStoreReview()", bridge);
            StringAssert.Contains("com.google.android.play.core.review.ReviewManagerFactory", bridge);
            StringAssert.Contains("\"launchReviewFlow\"", bridge);
            foreach (var ui in new[] { "TreadShredUi", "UnityEngine.UI", "Text ", "Image", "Button" })
                StringAssert.DoesNotContain(ui, bridge, "no rating stars, question or form of the game's own");
            StringAssert.Contains("com.google.android.play:review:2.0.2", Read("Editor/TreadShredReviewDependencies.xml"));
            StringAssert.Contains("implementation 'com.google.android.play:review:2.0.2'", Read("Plugins/Android/mainTemplate.gradle"));
        }

        [Test(Description = "D-033-AC-05: Settings has RATE TREAD SHRED, which opens the store's review page and never calls the native prompt")]
        public void SettingsOpensTheReviewPage()
        {
            var settings = Read("Script/TreadShredSettingsPanel.cs");
            StringAssert.Contains("ReviewRules.RateButton", settings);
            StringAssert.Contains("TreadShredReviewPrompt.OpenWriteReviewPage", settings);
            StringAssert.DoesNotContain("RequestStoreReview", settings);
            var bridge = Read("Script/Store/TreadShredReviewPrompt.cs");
            StringAssert.Contains("Application.OpenURL(ReviewRules.AndroidStoreUrl);", bridge);
            StringAssert.Contains("Application.OpenURL(ReviewRules.IosWriteReviewUrl);", bridge);
            Assert.AreEqual("RATE TREAD SHRED", ReviewRules.RateButton);
            Assert.AreEqual("https://apps.apple.com/app/id1403763343?action=write-review", ReviewRules.IosWriteReviewUrl);
            Assert.AreEqual("https://play.google.com/store/apps/details?id=com.tgts.tankkvstank", ReviewRules.AndroidStoreUrl);
        }

        [Test(Description = "D-033-AC-06: rating is never rewarded or gated: the review code grants nothing and asks nothing first")]
        public void RatingIsNeverRewardedOrGated()
        {
            var bridge = Read("Script/Store/TreadShredReviewPrompt.cs") + Read("Script/Progression/ReviewPrompt.cs");
            foreach (var reward in new[] { "Wallet", "Credit", "Coins", "Gems", "Reward(", "Confirm", "Enjoying" })
                StringAssert.DoesNotContain(reward, bridge);
        }
    }
}
