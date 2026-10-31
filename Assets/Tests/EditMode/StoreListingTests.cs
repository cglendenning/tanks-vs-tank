using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class StoreListingTests
    {
        private static string Listing() =>
            File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Store/listing-en-US.md"));

        private static string Field(string listing, string label) =>
            listing.Split(new[] { "**" + label + ":** " }, System.StringSplitOptions.None)[1].Split('\n')[0].Trim();

        [Test(Description = "D-030-AC-02: the listing copy fits each store's field limits and says the game has no ads")]
        public void ListingFitsStoreLimits()
        {
            var listing = Listing();
            Assert.LessOrEqual(Field(listing, "App Store subtitle (30)").Length, 30);
            Assert.LessOrEqual(Field(listing, "Google Play short description (80)").Length, 80);
            Assert.LessOrEqual(Field(listing, "App Store promotional text (170)").Length, 170);
            Assert.LessOrEqual(Field(listing, "App Store keywords (100)").Length, 100);
            var description = listing.Split(new[] { "## Description\n" }, System.StringSplitOptions.None)[1].Trim();
            Assert.LessOrEqual(description.Length, 4000);
            StringAssert.Contains("No ads", description);
            StringAssert.Contains("Optional in-app purchases", description, "purchases are disclosed (D-030-AC-03)");
            StringAssert.DoesNotContain("watch", description.ToLowerInvariant(), "no rewarded ads are promised");
        }
    }
}
