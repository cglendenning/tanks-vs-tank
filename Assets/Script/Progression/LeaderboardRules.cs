using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>D-032: when the game submits HIGH SCORE and what the leaderboard screens say.</summary>
    public static class LeaderboardRules
    {
        /// <summary>
        /// The score Game Center already holds for the player, or null until it has reported after
        /// sign-in. What it accepted this session wins over a report that has not caught up yet.
        /// </summary>
        public static int? Baseline(long? gameCenterScore, int acceptedThisSession)
        {
            if (!gameCenterScore.HasValue)
                return null;
            return (int)System.Math.Max(gameCenterScore.Value, acceptedThisSession);
        }

        /// <summary>Submit only a HIGH SCORE that went up and that a run could really produce.</summary>
        public static bool ShouldSubmit(int highScore, int onGameCenter)
        {
            return highScore > onGameCenter && highScore > 0 && highScore <= ScoreRules.MaxRunScore;
        }

        // Shown when Game Center is signed out and iOS only allows signing in from Settings (D-032-AC-03).
        public const string SignInFromSettings = "SIGN IN TO GAME CENTER IN SETTINGS";

        // The in-game leaderboard screen (D-032-AC-02).
        public const string Title = "LEADERBOARD";
        public const string Subtitle = "HIGH SCORE: TOP 25";
        public const string Loading = "LOADING SCORES";
        public const string Empty = "NO SCORES YET. FINISH A MISSION TO RANK";
        public const string Unavailable = "LEADERBOARD UNAVAILABLE. TRY AGAIN SOON";
        public const string Back = "BACK";

        // Game Center queues a score and uploads it a few seconds later, so a board read right
        // after a submission can miss it. Re-read a few times, two seconds apart, until it shows.
        public const int CatchUpReads = 4;
        public const float CatchUpSeconds = 2f;

        /// <summary>True while the board has not yet caught up with a score accepted this session.</summary>
        public static bool BoardBehind(long playerScoreOnBoard, int acceptedThisSession, int readsSoFar)
        {
            return acceptedThisSession > 0 && playerScoreOnBoard < acceptedThisSession && readsSoFar < CatchUpReads;
        }

        /// <summary>D-032-AC-02: the result screen's LEADERBOARD button carries the rank once the platform reports it.</summary>
        public static string ButtonLabel(int rank) => rank > 0 ? Title + " #" + CountText.Format(rank) : Title;

        public static string RankLine(int rank)
        {
            return "LEADERBOARD RANK: #" + CountText.Format(rank);
        }
    }
}
