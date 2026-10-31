using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ResultUpgradesTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-017-AC-07: the victory and failure screens open the upgrade screen over themselves, without leaving the mission or the run")]
        public void ResultScreensOpenUpgradesInPlace()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            var offerPanel = results.Substring(results.IndexOf("private void BuildOfferPanel(", System.StringComparison.Ordinal));
            StringAssert.Contains("TreadShredUi.CreateButton(ProgressionLabels.UpgradesButton,", offerPanel,
                "built with the offer panel, which both result screens show");
            StringAssert.Contains("() => TreadShredUpgradePanel.Show(rewardFont));", offerPanel);
            var upgrades = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.DoesNotContain("LoadScene", upgrades, "the upgrade screen is an overlay; the mission stays loaded");
            StringAssert.DoesNotContain("LoadLevel", upgrades);
            StringAssert.Contains("var loadout = TreadShredBoosts.Current.ConsumeForMission(progress.ArmorHits);", Script("Player/PlayerMoverment.cs"),
                "upgrades bought there are read when the next mission starts");
            Assert.AreEqual("UPGRADES", ProgressionLabels.UpgradesButton);
        }
    }
}
