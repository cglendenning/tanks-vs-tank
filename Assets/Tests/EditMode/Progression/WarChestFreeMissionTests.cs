using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class WarChestFreeMissionTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-047-AC-07: the free War Chest arrives after a random 2 to 4 result screens, never on a fixed beat")]
        public void ArrivesOnAVariableRatio()
        {
            var draws = new Queue<int>(new[] { 2, 4, 3 });
            var asked = new List<(int, int)>();
            var scripted = new WarChestArrivals((min, max) =>
            {
                asked.Add((min, max));
                return draws.Count > 0 ? draws.Dequeue() : 3;
            });
            var shown = Enumerable.Range(0, 9).Select(_ => scripted.ScreenShown()).ToArray();
            CollectionAssert.AreEqual(new[] { false, true, false, false, false, true, false, false, true }, shown);
            Assert.IsTrue(asked.All(range => range == (2, 4)), "always drawn from 2 to 4 screens");

            var random = new System.Random(47);
            var schedule = new WarChestArrivals((min, max) => random.Next(min, max + 1));
            var gaps = new List<int>();
            var since = 0;
            for (var screen = 0; screen < 3000; screen++)
            {
                since++;
                if (!schedule.ScreenShown())
                    continue;
                gaps.Add(since);
                since = 0;
            }
            Assert.IsTrue(gaps.All(gap => gap >= 2 && gap <= 4), string.Join(",", gaps.Distinct()));
            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, gaps.Distinct().ToArray(), "no fixed beat");
            Assert.That(gaps.Average(), Is.InRange(2.8, 3.2), "about every third result screen");
        }

        [Test(Description = "D-047-AC-06: the upgrades screen sells the War Chest beside DONE while it is on sale, opening the reveal with the purchase alone")]
        public void UpgradesScreenSellsIt()
        {
            Assert.AreEqual("WAR CHEST: $4.99", WarChest.StoreButton("$4.99"));
            Assert.AreEqual("WAR CHEST", WarChest.StoreButton(null));
            var panel = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("() => TreadShredWarChestOffer.ShowToBuy(font)", panel);
            StringAssert.Contains("warChestButton.gameObject.SetActive(TreadShredStore.Ensure().WarChestOnSale);", panel);
            StringAssert.Contains("Build(root, font, WarChest.Subtitle, false);", Script("Game/TreadShredWarChestOffer.cs"));
        }

        [Test(Description = "D-047-AC-07: taking the free War Chest puts its gear on for the next mission only, which counts as usual, and that mission's result screen opens the reveal")]
        public void FreeMissionCountsThenSells()
        {
            Assert.AreEqual(("FREE WAR CHEST", "ARMED: NEXT MISSION", "FREE WAR CHEST: ALL GEAR ON"),
                (WarChest.FreeButton, WarChest.ArmedLabel, WarChest.FreeNote));
            var results = Script("ManagerScore/ManagerScore.cs");
            var over = results.IndexOf("TreadShredFreeWarChest.MissionOver(", StringComparison.Ordinal);
            var afterLoss = results.IndexOf("TreadShredWarChestOffer.ShowAfterLoss(", StringComparison.Ordinal);
            var arrival = results.IndexOf("TreadShredFreeWarChest.ResultScreen(", StringComparison.Ordinal);
            Assert.IsTrue(over > 0 && over < afterLoss && afterLoss < arrival, "its reveal first, so no other War Chest offer joins it");
            StringAssert.DoesNotContain("TreadShredFreeWarChest.Active", results, "a free mission's result counts as usual");
            var free = Script("Game/TreadShredFreeWarChest.cs");
            StringAssert.Contains("UnityEngine.Object.FindFirstObjectByType<TreadShredWarChestOffer>() != null || !arrivals.ScreenShown()", free);
            StringAssert.Contains("TreadShredProgress.Ensure().Progression.StartTestDrive();", free);
            StringAssert.Contains("TreadShredProgress.Ensure().Progression.EndTestDrive();", free);
            StringAssert.Contains("if (Active)\n        {\n            End();\n            return;\n        }", free, "leaving the mission takes the gear away");
        }
    }
}
