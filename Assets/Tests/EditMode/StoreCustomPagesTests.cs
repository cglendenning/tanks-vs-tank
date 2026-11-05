using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class StoreCustomPagesTests
    {
        [Serializable]
        private sealed class Manifest
        {
            public Page[] pages;
        }

        [Serializable]
        private sealed class Page
        {
            public string name;
            public string[] hooks;
            public string promotionalText;
            public string[] iphone;
            public string[] ipad;
            public string preview;
        }

        private static string Root => Directory.GetParent(Application.dataPath).FullName;

        // The D-030-AC-07 rule the default page's copy follows (StoreValueEquationTests): no price words outside the description.
        private static readonly Regex PriceWords = new Regex(@"\bfree\b|[$£€]\s?\d|\bprice\b|\bdiscount|\bsale\b", RegexOptions.IgnoreCase);

        [Test(Description = "D-042-AC-01: two custom product pages, UNSTOPPABLE for Hooks A to C opening on upgrades and BLOW STUFF UP for Hooks E and F opening on explosions, Hook D on the default page, each of real store captures with promotional text that fits and names no price")]
        public void TwoPagesMatchTheirHooks()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Root, "Store/custom-product-pages.json")));
            CollectionAssert.AreEqual(new[] { "UNSTOPPABLE", "BLOW STUFF UP" }, manifest.pages.Select(p => p.name));
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, manifest.pages[0].hooks);
            CollectionAssert.AreEqual(new[] { "E", "F" }, manifest.pages[1].hooks);
            Assert.IsFalse(manifest.pages.Any(p => p.hooks.Contains("D")), "Hook D keeps the default page");

            StringAssert.EndsWith("5-upgrades-that-last.png", manifest.pages[0].iphone[0], "UNSTOPPABLE opens on the upgrades");
            StringAssert.EndsWith("1-become-unstoppable.png", manifest.pages[0].iphone[1], "then the tank dominating");
            StringAssert.EndsWith("1-blow-stuff-up-iphone.png", manifest.pages[1].iphone[0], "BLOW STUFF UP opens on the explosion chain");
            foreach (var page in manifest.pages)
            {
                Assert.AreEqual(6, page.iphone.Length, page.name);
                Assert.AreEqual(6, page.ipad.Length, page.name);
                CollectionAssert.AreEqual(page.iphone.Select(Path.GetFileName).Select(f => f.Replace("-iphone", "")),
                    page.ipad.Select(Path.GetFileName).Select(f => f.Replace("-ipad", "")), page.name + ": the iPad set follows the iPhone order");
                foreach (var file in page.iphone.Concat(page.ipad).Append(page.preview))
                    Assert.IsTrue(File.Exists(Path.Combine(Root, file)), file);
                Assert.LessOrEqual(page.promotionalText.Length, 170, page.name);
                Assert.IsFalse(PriceWords.IsMatch(page.promotionalText), page.name + " names a price: " + page.promotionalText);
            }
        }
    }
}
