using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class MissionListTests
    {
        [Test(Description = "D-041-AC-01: the mission list opens on the first unlocked mission not yet cleared at the selected tier, or on mission 1 when there is none")]
        public void OpensOnTheNextMission()
        {
            Assert.AreEqual(1, MissionList.NextToPlay(40, m => m == 1, m => false), "a fresh save");
            Assert.AreEqual(13, MissionList.NextToPlay(40, m => m <= 13, m => m <= 12), "twelve cleared: mission 13 is next");
            Assert.AreEqual(4, MissionList.NextToPlay(40, m => true, m => m <= 3), "a replay tier: every row open, three cleared at this tier");
            Assert.AreEqual(1, MissionList.NextToPlay(40, m => true, m => true), "every mission cleared at this tier");
            Assert.AreEqual(1, MissionList.NextToPlay(40, m => m <= 3, m => m <= 3), "no unlocked mission left to clear");
        }

        [Test(Description = "D-041-AC-01: the list scrolls in whole rows to put the selected row in the middle of the view, held at the top and bottom ends, and stays at the top when every row fits")]
        public void ScrollsTheSelectedRowIntoView()
        {
            // The real list: 20 rows a column, 148.6 apart, 3266.4 tall behind a 780.5-tall view that shows five.
            const float pitch = 148.6f, content = 3266.4f, view = 780.5f, travel = content - view;
            Assert.AreEqual(1f, MissionList.ScrollPosition(0, pitch, content, view), 1e-4, "mission 1 or 21: the top");
            Assert.AreEqual(1f, MissionList.ScrollPosition(2, pitch, content, view), 1e-4, "mission 3 is already in the middle at the top");
            Assert.AreEqual(1f - 3 * pitch / travel, MissionList.ScrollPosition(5, pitch, content, view), 1e-4,
                "mission 6 or 26: three rows down, the middle of five");
            Assert.AreEqual(1f - 16 * pitch / travel, MissionList.ScrollPosition(19, pitch, content, view), 1e-4,
                "mission 20 or 40: the last whole-row step");
            Assert.AreEqual(1f, MissionList.ScrollPosition(3, pitch, 700f, view), "nothing to scroll");
        }
    }
}
