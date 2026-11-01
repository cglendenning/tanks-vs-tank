using System.Collections.Generic;
using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    // Craig's report: after reinstalling, a failed run showed NEW HIGH SCORE below the 3,800 the leaderboard held.
    // HIGH SCORE lives on the device; the platform's record of the player's best must restore it at sign-in.
    public sealed class HighScoreRestoreTests
    {
        // NUnit runs every test on one instance of this class, so each starts from a fresh device.
        private FakeClock clock;
        private List<int> restored;
        private int highScore;

        [SetUp]
        public void FreshDevice()
        {
            clock = new FakeClock();
            restored = new List<int>();
            highScore = 0;
        }

        private LeaderboardSync SignIn(FakeGameCenter gameCenter)
        {
            var sync = new LeaderboardSync(gameCenter, () => highScore, clock.After, score =>
            {
                restored.Add(score);
                highScore = System.Math.Max(highScore, score);
            });
            sync.SignedIn();
            clock.Advance(FakeGameCenter.SettleSeconds);
            return sync;
        }

        [Test(Description = "D-032-AC-01: after a reinstall, sign-in restores HIGH SCORE from the platform's record, so a lower run is not a NEW HIGH SCORE")]
        public void ReinstallRestoresTheBest()
        {
            var gameCenter = new FakeGameCenter(clock) { OnBoard = 3800 };

            SignIn(gameCenter);

            CollectionAssert.AreEqual(new[] { 3800 }, restored);
            Assert.AreEqual(3800, highScore);
            Assert.IsEmpty(gameCenter.Submissions, "the platform already holds it; nothing is resubmitted");
            Assert.IsFalse(2400 > highScore, "a 2,400 run after the reinstall no longer beats HIGH SCORE");
        }

        [Test(Description = "D-032-AC-01: a device best above the platform's record is kept and submitted, never lowered")]
        public void HigherDeviceBestIsKept()
        {
            highScore = 5200;
            var gameCenter = new FakeGameCenter(clock) { OnBoard = 3800 };

            SignIn(gameCenter);

            Assert.IsEmpty(restored);
            Assert.AreEqual(5200, highScore);
            CollectionAssert.AreEqual(new[] { 5200L }, gameCenter.Submissions);
        }

        [Test(Description = "D-032-AC-01: when the board holds nothing or cannot load, HIGH SCORE is left as it is")]
        public void NothingOnTheBoardRestoresNothing()
        {
            SignIn(new FakeGameCenter(clock) { OnBoard = 0 });
            SignIn(new FakeGameCenter(clock) { OnBoard = 3800, LoadsFail = true });

            Assert.IsEmpty(restored);
            Assert.AreEqual(0, highScore);
        }
    }
}
