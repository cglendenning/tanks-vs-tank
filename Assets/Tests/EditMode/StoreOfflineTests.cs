using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class StoreOfflineTests
    {
        private static readonly string Root = Directory.GetParent(Application.dataPath).FullName;

        [Test(Description = "D-030-AC-08: the listing says missions play offline (App Store keyword and the shared description), and no game code gates play on a connection")]
        public void ListingSaysMissionsPlayOffline()
        {
            var listing = File.ReadAllText(Path.Combine(Root, "Store/listing-en-US.md"));
            var keywords = listing.Split(new[] { "**App Store keywords (100):** " }, System.StringSplitOptions.None)[1].Split('\n')[0].Trim();
            CollectionAssert.Contains(keywords.Split(','), "offline");
            Assert.LessOrEqual(keywords.Length, 100);
            StringAssert.Contains("• Missions play offline: no connection needed to fight", listing);

            var scripts = Directory.GetFiles(Path.Combine(Application.dataPath, "Script"), "*.cs", SearchOption.AllDirectories);
            var gates = scripts.Where(f => File.ReadAllText(f).Contains("internetReachability")).ToList();
            Assert.IsEmpty(gates, "no script checks for a connection before play");
        }
    }
}
