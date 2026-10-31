using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class PlayConsoleCatalogTests
    {
        [Test(Description = "D-007-AC-02: the Play Console setup script creates and activates exactly the catalog's gem packs")]
        public void PlayConsoleProvisionsTheCatalog()
        {
            var script = File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "scripts/play-console.py"));
            foreach (var pack in GemCatalog.Packs)
                StringAssert.Contains("(\"" + pack.ProductId + "\", " + pack.Gems + ",", script);
            Assert.AreEqual(GemCatalog.Packs.Count, System.Text.RegularExpressions.Regex.Matches(script, "\\(\"treadshred\\.gems").Count,
                "no product the game does not sell");
            StringAssert.Contains("activatePurchaseOptionRequest", script);
        }
    }
}
