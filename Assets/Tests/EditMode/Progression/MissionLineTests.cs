using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class MissionLineTests
    {
        [Test(Description = "D-032-AC-01: a mission that earned nothing reads THIS MISSION: 0, without an empty breakdown")]
        public void NothingEarnedReadsPlainly()
        {
            Assert.AreEqual("THIS MISSION: 0", ScoreRules.MissionLine(new MissionScore(false, 0, 0, 0, 0)));
            Assert.AreEqual("THIS MISSION: 4,500 (10 KILLS 3,000 + CLEAR 1,500)",
                ScoreRules.MissionLine(new MissionScore(true, 10, 3000, 1500, 9000)), "Elite values, thousands separated");
        }
    }
}
