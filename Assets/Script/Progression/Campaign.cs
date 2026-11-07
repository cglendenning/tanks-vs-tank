using System;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-019: one campaign of 120 numbered missions. Missions 1-40 are Recruit, 41-80 Veteran and 81-120 Elite; the 40
    /// maps repeat in each tier, so mission n plays map ((n - 1) mod 40) + 1 with its tier's multipliers. Progress is
    /// still kept per tier and map, so saves from before the campaign carry over unchanged.
    /// </summary>
    public static class Campaign
    {
        public const int MissionsPerTier = ProgressionRules.MissionCount;
        public const int TierCount = (int)DifficultyTier.Elite + 1;
        public const int MissionTotal = MissionsPerTier * TierCount;
        public const string Cleared = "ALL 120 MISSIONS CLEARED";
        public const string PromotionLine = "ENEMIES ARE TOUGHER, FASTER AND FIRE MORE OFTEN";

        public static DifficultyTier TierOf(int number) => (DifficultyTier)((Checked(number) - 1) / MissionsPerTier);

        public static int MapOf(int number) => (Checked(number) - 1) % MissionsPerTier + 1;

        public static int Number(DifficultyTier tier, int map)
        {
            if (map < 1 || map > MissionsPerTier)
                throw new ArgumentOutOfRangeException(nameof(map), map, "Unknown map.");
            return (int)tier * MissionsPerTier + map;
        }

        /// <summary>The mission NEXT MISSION leads to, or null after the last one.</summary>
        public static int? Next(int number) => Checked(number) < MissionTotal ? number + 1 : (int?)null;

        /// <summary>Clearing this mission promotes the player into the next tier (40 and 80).</summary>
        public static bool Promotes(int number) => Checked(number) < MissionTotal && number % MissionsPerTier == 0;

        public static string TierName(DifficultyTier tier) => tier.ToString().ToUpperInvariant();

        public static string Header(DifficultyTier tier) =>
            TierName(tier) + " · " + Number(tier, 1) + "–" + Number(tier, MissionsPerTier);

        public static string HudLabel(int number) => "MISSION " + Checked(number) + " · " + TierName(TierOf(number));

        public static string PromotionTitle(DifficultyTier tier) => "PROMOTED TO " + TierName(tier);

        /// <summary>
        /// Missions unlock strictly in order: mission 1 always, any other once the mission before it is cleared and its
        /// tier is open.
        /// </summary>
        public static bool IsUnlocked(int number, Func<int, bool> cleared, Func<DifficultyTier, bool> tierOpen) =>
            Checked(number) == 1 || (tierOpen(TierOf(number)) && cleared(number - 1));

        private static int Checked(int number)
        {
            if (number < 1 || number > MissionTotal)
                throw new ArgumentOutOfRangeException(nameof(number), number, "Missions are numbered 1 to " + MissionTotal + ".");
            return number;
        }
    }
}
