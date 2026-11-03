using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    // P-6: the store pages present the value equation, in the art and in the first lines of copy.
    public sealed class StoreValueEquationTests
    {
        private static readonly string Root = Directory.GetParent(Application.dataPath).FullName;

        private static readonly string[] ValueEquationOrder =
        {
            "1-become-unstoppable.png", "2-in-battle-in-seconds.png", "3-win-your-first-battle.png",
            "4-no-ads-no-sign-up.png", "5-upgrades-that-last.png", "6-climb-the-leaderboard.png",
        };

        // The Play listing's art folder, as play-console.py publishes it; the App Store sets sit beside it.
        private static string ListingMedia() =>
            Regex.Match(File.ReadAllText(Path.Combine(Root, "scripts/play-console.py")), "MEDIA = ROOT / \"([^\"]+)/google-play\"").Groups[1].Value;

        private static string Field(string label)
        {
            var listing = File.ReadAllText(Path.Combine(Root, "Store/listing-en-US.md"));
            return listing.Split(new[] { "**" + label + ":** " }, System.StringSplitOptions.None)[1].Split('\n')[0].Trim();
        }

        [Test(Description = "D-030-AC-05: every published screenshot set runs in value-equation order, dream outcome first")]
        public void ScreenshotsFollowTheValueEquation()
        {
            var media = ListingMedia();
            Assert.IsNotEmpty(media, "play-console.py names the listing's art folder");
            foreach (var set in new[] { "google-play/phone", "google-play/tablet", "app-store/iphone", "app-store/ipad" })
            {
                var files = Directory.GetFiles(Path.Combine(Root, media, set), "*.png").Select(Path.GetFileName).OrderBy(n => n).ToArray();
                CollectionAssert.AreEqual(ValueEquationOrder, files, set);
            }
        }

        [Test(Description = "D-030-AC-06: the first lines a browser reads state the offer: in battle in seconds, a first win within a minute, no ads, no sign-up")]
        public void FirstLinesStateTheOffer()
        {
            StringAssert.Contains("in seconds", Field("App Store subtitle (30)").ToLowerInvariant());
            StringAssert.Contains("no ads", Field("App Store subtitle (30)").ToLowerInvariant());
            var promo = Field("App Store promotional text (170)").ToLowerInvariant();
            foreach (var claim in new[] { "in battle within seconds", "first win under a minute", "no ads", "no sign-up" })
                StringAssert.Contains(claim, promo);
            var play = Field("Google Play short description (80)").ToLowerInvariant();
            StringAssert.Contains("in battle in seconds", play);
            StringAssert.Contains("no ads", play);
        }

        // App Review treats "free" as a price reference anywhere but the description (Guideline 2.3.7, 1.9.3 rejection).
        private static readonly Regex PriceWords = new Regex(@"\bfree\b|[$\u00a3\u20ac]\s?\d|\bprice\b|\bdiscount|\bsale\b", RegexOptions.IgnoreCase);

        [Test(Description = "D-030-AC-07: no screenshot caption, subtitle, promotional text or keyword names a price or says free")]
        public void NoPriceOutsideTheDescription()
        {
            foreach (var label in new[] { "App Store subtitle (30)", "App Store promotional text (170)", "App Store keywords (100)", "Google Play short description (80)" })
                Assert.IsFalse(PriceWords.IsMatch(Field(label)), label + ": " + Field(label));
            foreach (var set in new[] { "google-play/phone", "google-play/tablet", "app-store/iphone", "app-store/ipad" })
                foreach (var shot in Directory.GetFiles(Path.Combine(Root, ListingMedia(), set), "*.png").Select(Path.GetFileNameWithoutExtension))
                    Assert.IsFalse(PriceWords.IsMatch(shot.Replace('-', ' ')), set + "/" + shot);
        }
    }
}
