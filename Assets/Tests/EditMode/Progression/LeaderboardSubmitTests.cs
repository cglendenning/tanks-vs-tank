using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class LeaderboardSubmitTests
    {
        [Test(Description = "D-032-AC-01: scores are submitted against what Game Center holds, so a stale local record never blocks one")]
        public void SubmitsAgainstGameCentersOwnRecord()
        {
            Assert.IsNull(LeaderboardRules.Baseline(null, 0), "nothing is submitted until Game Center reports the player");

            // Game Center holds nothing for the player: any real total goes up, whatever an older build recorded.
            Assert.AreEqual(0, LeaderboardRules.Baseline(0, 0));
            Assert.IsTrue(LeaderboardRules.ShouldSubmit(23400, LeaderboardRules.Baseline(0, 0).Value));

            // A score accepted this session counts even if Game Center's report has not caught up.
            Assert.AreEqual(23400, LeaderboardRules.Baseline(20000, 23400));
            Assert.IsFalse(LeaderboardRules.ShouldSubmit(23400, LeaderboardRules.Baseline(20000, 23400).Value));
            Assert.IsTrue(LeaderboardRules.ShouldSubmit(24000, LeaderboardRules.Baseline(23400, 0).Value));
        }
    }
}
