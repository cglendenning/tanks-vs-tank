using System;
using System.Globalization;

namespace TreadShred.Progression
{
    /// <summary>D-032-AC-01: how SCORE is earned, its ceiling, and how it is shown.</summary>
    public static class ScoreRules
    {
        public const int KillPoints = 100;
        public const int MaxAccuracyBonus = 300;
        public const int MaxArmorBonus = 1000;
        // Armor bonus is measured against the base three hits, whatever armor upgrades add.
        public const int ArmorBonusHits = 3;

        // Enemies across missions 1-40, read from each Lv scene's ManagerScore.CountEmnemy (2026-10-30).
        // Update with the scenes: the leaderboard's score limit comes from it.
        public const int EnemiesPerPass = 218;
        public const int TierCount = (int)DifficultyTier.Elite + 1;

        /// <summary>The most a run can score: every enemy and full bonuses on every mission of every tier.</summary>
        public static int MaxRunScore =>
            TierCount * (EnemiesPerPass * KillPoints + ProgressionRules.MissionCount * (MaxAccuracyBonus + MaxArmorBonus));

        /// <summary>Kills per shot fired, as a share of 300; Overdrive's extra rounds cannot push it higher.</summary>
        public static int AccuracyBonus(int kills, int shots)
        {
            if (shots <= 0 || kills <= 0)
                return 0;
            return (int)(Math.Min(1f, (float)kills / shots) * MaxAccuracyBonus);
        }

        /// <summary>A third of 1,000 for each of the first three hits the tank did not take.</summary>
        public static int ArmorBonus(int hitsTaken)
        {
            var kept = ArmorBonusHits - Math.Min(Math.Max(hitsTaken, 0), ArmorBonusHits);
            return (int)((float)kept / ArmorBonusHits * MaxArmorBonus);
        }

        public static string ScoreLine(int score) => "SCORE // " + Format(score);

        public static string HighScoreLine(int highScore, bool isNew) =>
            (isNew ? "NEW HIGH SCORE // " : "HIGH SCORE // ") + Format(highScore);

        public static string BonusLine(MissionScore mission) =>
            "ACCURACY +" + Format(mission.AccuracyBonus) + " // ARMOR +" + Format(mission.ArmorBonus);

        private static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }

    /// <summary>What one finished mission added to the run.</summary>
    public readonly struct MissionScore
    {
        public MissionScore(bool won, int killPoints, int accuracyBonus, int armorBonus, int runScore)
        {
            Won = won;
            KillPoints = killPoints;
            AccuracyBonus = accuracyBonus;
            ArmorBonus = armorBonus;
            RunScore = runScore;
        }

        public bool Won { get; }
        public int KillPoints { get; }
        public int AccuracyBonus { get; }
        public int ArmorBonus { get; }

        /// <summary>SCORE when the mission ended: the run's final score if the tank was lost.</summary>
        public int RunScore { get; }
    }

    /// <summary>
    /// D-032-AC-01: SCORE builds across a run of missions and resets when the tank
    /// is lost. A won mission banks its kills plus accuracy and armor bonuses. A
    /// lost mission ends the run with its kills counted; Armored Redeploy resumes
    /// the run from where it stood before that mission. (A revive never ends a mission.)
    /// </summary>
    public sealed class ScoreRun
    {
        private int resumeFrom;

        /// <summary>SCORE carried into the current mission.</summary>
        public int Banked { get; private set; }

        /// <summary>SCORE as it stands mid-mission.</summary>
        public int Live(int kills) => Banked + Math.Max(kills, 0) * ScoreRules.KillPoints;

        public void Start()
        {
            Banked = 0;
            resumeFrom = 0;
        }

        public MissionScore Complete(bool won, int kills, int shots, int hitsTaken)
        {
            var killPoints = Math.Max(kills, 0) * ScoreRules.KillPoints;
            if (!won)
            {
                var final = Banked + killPoints;
                resumeFrom = Banked;
                Banked = 0;
                return new MissionScore(false, killPoints, 0, 0, final);
            }
            var accuracy = ScoreRules.AccuracyBonus(kills, shots);
            var armor = ScoreRules.ArmorBonus(hitsTaken);
            Banked += killPoints + accuracy + armor;
            resumeFrom = Banked;
            return new MissionScore(true, killPoints, accuracy, armor, Banked);
        }

        /// <summary>Armored Redeploy replays the lost mission with the run as it stood before it.</summary>
        public void ResumeAfterRedeploy()
        {
            Banked = resumeFrom;
        }
    }
}
