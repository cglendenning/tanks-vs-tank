using TreadShred.Economy;
using System;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-032-AC-01: SCORE is earned only during play and shown the moment it is
    /// earned: 100 per enemy destroyed and 500 for clearing a mission, doubled on
    /// Veteran and tripled on Elite. Nothing is added after a mission ends.
    /// </summary>
    public static class ScoreRules
    {
        public const int KillPoints = 100;
        public const int ClearPoints = 500;

        // Enemies across missions 1-40, read from each Lv scene's ManagerScore.CountEmnemy (2026-11-01: mission 1
        // has five, D-035). Update with the scenes: the leaderboard's score limit comes from it.
        public const int EnemiesPerPass = 222;
        public const int TierCount = (int)DifficultyTier.Elite + 1;

        public static int TierMultiplier(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Veteran:
                    return 2;
                case DifficultyTier.Elite:
                    return 3;
                default:
                    return 1;
            }
        }

        public static int KillValue(DifficultyTier tier) => KillPoints * TierMultiplier(tier);

        public static int ClearValue(DifficultyTier tier) => ClearPoints * TierMultiplier(tier);

        /// <summary>The most a run can score: every enemy and every mission clear on every tier.</summary>
        public static int MaxRunScore
        {
            get
            {
                var perPass = EnemiesPerPass * KillPoints + ProgressionRules.MissionCount * ClearPoints;
                var total = 0;
                for (var tier = 0; tier < TierCount; tier++)
                    total += perPass * TierMultiplier((DifficultyTier)tier);
                return total;
            }
        }

        public static string ScoreLine(int score) => "SCORE: " + CountText.Format(score);

        public static string HighScoreLine(int highScore, bool isNew) =>
            (isNew ? "NEW HIGH SCORE: " : "HIGH SCORE: ") + CountText.Format(highScore);

        public static string KillCallout(int points) => "TARGET DESTROYED +" + CountText.Format(points);

        public static string ClearCallout(int points) => "MISSION CLEAR +" + CountText.Format(points);

    }

    /// <summary>What one finished mission added to the run.</summary>
    public readonly struct MissionScore
    {
        public MissionScore(bool won, int kills, int killPoints, int clearPoints, int runScore)
        {
            Won = won;
            Kills = kills;
            KillPoints = killPoints;
            ClearPoints = clearPoints;
            RunScore = runScore;
        }

        public bool Won { get; }
        public int Kills { get; }
        public int KillPoints { get; }
        public int ClearPoints { get; }
        public int MissionPoints => KillPoints + ClearPoints;

        /// <summary>SCORE when the mission ended: the run's final score if the tank was lost.</summary>
        public int RunScore { get; }
    }

    /// <summary>
    /// D-032-AC-01: SCORE builds across a run of missions and resets when the tank
    /// is lost. A won mission banks its kills and its clear; a lost mission ends the
    /// run with its kills counted, and Armored Redeploy resumes the run from where it
    /// stood before that mission. (A revive never ends a mission.)
    /// </summary>
    public sealed class ScoreRun
    {
        private int resumeFrom;

        /// <summary>SCORE carried into the current mission.</summary>
        public int Banked { get; private set; }

        /// <summary>SCORE as it stands mid-mission.</summary>
        public int Live(int kills, DifficultyTier tier) => Banked + Math.Max(kills, 0) * ScoreRules.KillValue(tier);

        public void Start()
        {
            Banked = 0;
            resumeFrom = 0;
        }

        public MissionScore Complete(bool won, int kills, DifficultyTier tier)
        {
            kills = Math.Max(kills, 0);
            var killPoints = kills * ScoreRules.KillValue(tier);
            if (!won)
            {
                var final = Banked + killPoints;
                resumeFrom = Banked;
                Banked = 0;
                return new MissionScore(false, kills, killPoints, 0, final);
            }
            var clear = ScoreRules.ClearValue(tier);
            Banked += killPoints + clear;
            resumeFrom = Banked;
            return new MissionScore(true, kills, killPoints, clear, Banked);
        }

        /// <summary>D-009-AC-05: the SCORE an Armored Redeploy would carry back into the lost mission.</summary>
        public int Resumable => resumeFrom;

        /// <summary>Armored Redeploy replays the lost mission with the run as it stood before it.</summary>
        public void ResumeAfterRedeploy()
        {
            Banked = resumeFrom;
        }
    }
}
