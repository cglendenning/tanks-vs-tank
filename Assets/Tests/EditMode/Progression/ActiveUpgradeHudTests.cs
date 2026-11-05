using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class ActiveUpgradeHudTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-017-AC-10: the mission HUD names each upgraded track and its level, one line each in track order, and nothing when no track is upgraded")]
        public void UpgradeLinesNameEachUpgradedTrack()
        {
            CollectionAssert.IsEmpty(ProgressionLabels.ActiveUpgrades(track => 0));
            CollectionAssert.AreEqual(new[] { "ARMOR LEVEL 3", "SPEED LEVEL 1" },
                ProgressionLabels.ActiveUpgrades(track => track == UpgradeTrack.Armor ? 3 : track == UpgradeTrack.Speed ? 1 : 0));
            CollectionAssert.AreEqual(new[] { "ARMOR LEVEL 10", "FIREPOWER LEVEL 2", "SPEED LEVEL 5" },
                ProgressionLabels.ActiveUpgrades(track => track == UpgradeTrack.Armor ? 10 : track == UpgradeTrack.Firepower ? 2 : 5));
        }

        [Test(Description = "D-017-AC-10: armor beyond the three drawn shields shows as +N and counts down to nothing as hits use it")]
        public void ExtraArmorCountsDown()
        {
            Assert.AreEqual("+2", ProgressionLabels.ExtraArmor(UpgradeEffects.ArmorHits(4), 3), "armor level 4 is five hits");
            Assert.AreEqual("+1", ProgressionLabels.ExtraArmor(4, 3));
            Assert.AreEqual(string.Empty, ProgressionLabels.ExtraArmor(3, 3));
            Assert.AreEqual(string.Empty, ProgressionLabels.ExtraArmor(0, 3));
        }

        [Test(Description = "D-017-AC-10: every mission shows the upgrade line and the extra armor, which updates on each hit and on a revive")]
        public void TheMissionHudShowsThem()
        {
            var player = Script("Player/PlayerMoverment.cs");
            StringAssert.Contains("TreadShredOverdriveBadge.ShowUpgrades(hudFont, TreadShred.Progression.ProgressionLabels.ActiveUpgrades(progress.Progression.Level));", player);
            StringAssert.Contains("extraArmor = TreadShredExtraArmor.Attach(Heart, hudFont);", player);
            StringAssert.Contains("TreadShredFirstMinute.FirstPlay);\n        ShowExtraArmor();", player, "after every hit");
            StringAssert.Contains("countdie = missionArmorHits;\n        ShowExtraArmor();", player, "after a revive");
            var badge = Script("Game/TreadShredOverdriveBadge.cs");
            StringAssert.Contains("if (upgrades != null)\n            upgrades.SetActive(visible);", badge, "hidden with the Overdrive tag while paused");
            StringAssert.Contains("Clear(ref badge);\n        Clear(ref upgrades);", badge, "gone with it when the mission ends");
        }
    }
}
