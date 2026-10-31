using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    /// <summary>
    /// D-032 end to end: the real ScoreRun and LeaderboardSync against a fake Game Center that
    /// answers late, uploads accepted scores a few seconds afterwards and keeps only the best.
    /// What used to need a phone (stale boards, frozen screens, lost scores) is checked here.
    /// </summary>
    public sealed class LeaderboardScenarioTests
    {
        [Test(Description = "D-032-AC-01: a higher HIGH SCORE is submitted at mission end and the board shows it once Game Center uploads it")]
        public void HigherHighScoreReachesTheBoard()
        {
            var game = new LeaderboardGame().SignIn();
            game.Clock.Advance(1f);

            var mission = game.EndMission(won: true, kills: 3);
            game.Clock.Advance(FakeGameCenter.SettleSeconds);

            CollectionAssert.AreEqual(new[] { (long)mission.RunScore }, game.GameCenter.Submissions);
            Assert.AreEqual(800, game.GameCenter.OnBoard);
        }

        [Test(Description = "D-032-AC-01: submissions are judged against Game Center's own record, so a lower HIGH SCORE never resubmits and a reinstall still submits a higher one")]
        public void GameCenterRecordGovernsSubmission()
        {
            var lower = new LeaderboardGame(onBoard: 5000) { HighScore = 1200 }.SignIn();
            lower.Clock.Advance(1f);
            lower.EndMission(won: true, kills: 4);
            lower.Clock.Advance(FakeGameCenter.SettleSeconds);
            CollectionAssert.IsEmpty(lower.GameCenter.Submissions, "Game Center already holds more");

            var reinstall = new LeaderboardGame(onBoard: 800).SignIn();
            reinstall.Clock.Advance(1f);
            reinstall.EndMission(won: true, kills: 8);
            reinstall.EndMission(won: true, kills: 8);
            reinstall.Clock.Advance(FakeGameCenter.SettleSeconds);
            CollectionAssert.AreEqual(new long[] { 1300, 2600 }, reinstall.GameCenter.Submissions);
            Assert.AreEqual(2600, reinstall.GameCenter.OnBoard);
        }

        [Test(Description = "D-032-AC-01: a mission that ends before Game Center reports sign-in is submitted once it does")]
        public void MissionBeforeSignInIsSubmittedAfter()
        {
            var game = new LeaderboardGame(signedIn: false);
            game.EndMission(won: true, kills: 5);
            game.Clock.Advance(FakeGameCenter.SettleSeconds);
            CollectionAssert.IsEmpty(game.GameCenter.Submissions);

            game.SignIn();
            game.Clock.Advance(FakeGameCenter.SettleSeconds);

            CollectionAssert.AreEqual(new long[] { 1000 }, game.GameCenter.Submissions);
            Assert.AreEqual(1000, game.GameCenter.OnBoard);
        }

        [Test(Description = "D-032-AC-02: the result screen's rank reflects the score just earned, not the board from before the upload")]
        public void ResultRankWaitsForTheUpload()
        {
            var game = new LeaderboardGame(onBoard: 500).SignIn();
            game.Clock.Advance(1f);
            Assert.AreEqual(5, game.GameCenter.RankOf(500));

            game.EndMission(won: true, kills: 8);
            game.EndMission(won: true, kills: 8);
            var rank = game.ResultRank();

            Assert.AreEqual(game.GameCenter.RankOf(2600), rank, "rank for 2,600 (#3), not the stale 500 (#5)");
            Assert.AreEqual(3, rank);
        }

        [Test(Description = "D-032-AC-02: the leaderboard screen opened during a submission shows the new score as the player's entry")]
        public void ScreenDuringSubmissionShowsTheNewScore()
        {
            var game = new LeaderboardGame(onBoard: 500).SignIn();
            game.Clock.Advance(1f);
            game.EndMission(won: true, kills: 9);

            var (answered, page) = game.OpenScreen();

            Assert.IsTrue(answered, "the screen finished loading");
            Assert.IsNotNull(page);
            Assert.AreEqual(1400, page.Player.Value.Score);
            Assert.AreEqual(4, page.Player.Value.Rank, "behind ACE, BRAVO and COBRA");
            CollectionAssert.Contains(page.Top, page.Player.Value);
        }

        [Test(Description = "D-032-AC-02: when Game Center's upload stalls, the screen still finishes loading instead of freezing")]
        public void StalledUploadNeverFreezesTheScreen()
        {
            var game = new LeaderboardGame(onBoard: 500).SignIn();
            game.GameCenter.Upload = 600f;
            game.Clock.Advance(1f);
            game.EndMission(won: true, kills: 9);

            var (answered, page) = game.OpenScreen();

            Assert.IsTrue(answered, "the screen gave up waiting after the catch-up reads");
            Assert.AreEqual(500, page.Player.Value.Score, "it shows what Game Center has");
            Assert.LessOrEqual(game.GameCenter.Loads, 1 + LeaderboardRules.CatchUpReads, "no endless polling");
        }

        [Test(Description = "D-032-AC-02: a board that cannot be loaded reports unavailable rather than hanging")]
        public void UnloadableBoardReportsUnavailable()
        {
            var game = new LeaderboardGame().SignIn();
            game.GameCenter.LoadsFail = true;
            game.Clock.Advance(1f);

            var (answered, page) = game.OpenScreen();

            Assert.IsTrue(answered);
            Assert.IsNull(page, "the screen shows " + LeaderboardRules.Unavailable);
        }

        [Test(Description = "D-032-AC-03: signed out, nothing is submitted or loaded, the screen answers at once and LEADERBOARD offers sign-in")]
        public void SignedOutNeverSubmitsAndOffersSignIn()
        {
            var game = new LeaderboardGame(signedIn: false);
            game.EndMission(won: true, kills: 6);
            var (answered, page) = game.OpenScreen();
            game.Sync.RequestRank(rank => Assert.Fail("no rank while signed out"));
            game.Clock.Advance(10f);

            CollectionAssert.IsEmpty(game.GameCenter.Submissions);
            Assert.AreEqual(0, game.GameCenter.Loads);
            Assert.IsTrue(answered);
            Assert.IsNull(page);
            Assert.IsTrue(game.Clock.Idle, "nothing waits on Game Center");

            Assert.AreEqual(LeaderboardOpenResult.SignInShown, game.Sync.Open());
            game.GameCenter.CanOfferSignIn = false;
            Assert.AreEqual(LeaderboardOpenResult.SignInFromSettings, game.Sync.Open());
            Assert.AreEqual(2, game.GameCenter.SignInOffers);
            game.SignIn();
            Assert.AreEqual(LeaderboardOpenResult.Ready, game.Sync.Open());
        }

        [Test(Description = "D-032-AC-01: a score rejected while offline is submitted at the next mission end, and screens never wait on the failure")]
        public void RejectedScoreIsRetriedNextMission()
        {
            var game = new LeaderboardGame().SignIn();
            game.Clock.Advance(1f);
            game.GameCenter.Online = false;
            game.EndMission(won: true, kills: 4);
            var (answered, _) = game.OpenScreen();
            Assert.IsTrue(answered, "the screen loaded after the failed submission");
            Assert.AreEqual(0, game.GameCenter.OnBoard);
            Assert.AreEqual(1, game.GameCenter.Submissions.Count, "no resubmission loop while offline");

            game.GameCenter.Online = true;
            game.EndMission(won: false, kills: 0);
            game.Clock.Advance(FakeGameCenter.SettleSeconds);

            Assert.AreEqual(900, game.GameCenter.OnBoard);
        }

        [Test(Description = "D-032-AC-01: a HIGH SCORE that rises while a submission is in flight still reaches the board")]
        public void ScoreRisingDuringSubmissionReachesTheBoard()
        {
            var game = new LeaderboardGame().SignIn();
            game.Clock.Advance(1f);
            game.EndMission(won: true, kills: 2);
            game.HighScore = 5000;
            game.Sync.SubmitIfRaised();
            game.Clock.Advance(FakeGameCenter.SettleSeconds);

            CollectionAssert.AreEqual(new long[] { 700, 5000 }, game.GameCenter.Submissions);
            Assert.AreEqual(5000, game.GameCenter.OnBoard);
        }

        [Test(Description = "D-032-AC-04: a HIGH SCORE above what three tiers of missions can produce is never submitted")]
        public void ImpossibleScoreIsNeverSubmitted()
        {
            var game = new LeaderboardGame { HighScore = ScoreRules.MaxRunScore + 1 }.SignIn();
            game.Clock.Advance(FakeGameCenter.SettleSeconds);
            CollectionAssert.IsEmpty(game.GameCenter.Submissions);
        }
    }
}
