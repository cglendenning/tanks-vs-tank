using System.Collections.Generic;
using System.Globalization;

namespace TreadShred.Progression
{
    /// <summary>D-032: what the platform leaderboards rank and when the game submits.</summary>
    public static class LeaderboardRules
    {
        // A mission's best score is accuracy (up to 300) plus armor kept (up to 1000).
        public const int MaxMissionScore = 1300;

        // Every mission is replayed on each tier (D-019), and each tier's best counts.
        public const int TierCount = (int)DifficultyTier.Elite + 1;

        // The ceiling configured as the score limit in App Store Connect and the Play Console (D-032-AC-04).
        public static int MaxTotalScore => TierCount * ProgressionRules.MissionCount * MaxMissionScore;

        /// <summary>TOTAL SCORE: the sum of the best score of every mission on every tier played.</summary>
        public static int TotalScore(IEnumerable<int> bestMissionScores)
        {
            var total = 0;
            foreach (var score in bestMissionScores)
                total += score;
            return total;
        }

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

        /// <summary>Submit only a total that went up and that the game could really produce.</summary>
        public static bool ShouldSubmit(int total, int lastSubmitted)
        {
            return total > lastSubmitted && total > 0 && total <= MaxTotalScore;
        }

        /// <summary>
        /// Where a mission's best score is saved. Recruit keeps the original key so
        /// existing scores carry over; harder tiers keep their own bests.
        /// </summary>
        public static string BestScoreKey(DifficultyTier tier, int mission)
        {
            return tier == DifficultyTier.Recruit
                ? "Lvs" + mission
                : "Lvs" + mission + "_" + tier.ToString().ToLowerInvariant();
        }

        // Shown when Game Center is signed out and iOS only allows signing in from Settings (D-032-AC-03).
        public const string SignInFromSettings = "SIGN IN TO GAME CENTER IN SETTINGS";

        // The in-game leaderboard screen (D-032-AC-02).
        public const string Title = "LEADERBOARD";
        public const string Subtitle = "TOTAL SCORE // TOP 25";
        public const string Loading = "LOADING SCORES";
        public const string Empty = "NO SCORES YET // FINISH A MISSION TO RANK";
        public const string Unavailable = "LEADERBOARD UNAVAILABLE // TRY AGAIN SOON";
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

        // A mission's rating (accuracy bonus plus armor kept): what it adds to TOTAL SCORE when it beats the best.
        public static string MissionLine(int missionScore)
        {
            return "MISSION SCORE // " + missionScore.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string TotalLine(int total)
        {
            return "TOTAL SCORE // " + total.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string RankLine(int rank)
        {
            return "LEADERBOARD RANK // #" + rank.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
