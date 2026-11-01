namespace TreadShred.Progression
{
    /// <summary>
    /// D-035: a first-time player's first minute. The first launch opens straight into mission 1, and until mission 1
    /// is first cleared on Recruit it plays as a guaranteed power moment: free Overdrive for that mission, enemies
    /// holding fire, and a fire hint above the nearest enemy until the first shot.
    /// </summary>
    public static class FirstMinute
    {
        public const int Mission = 1;
        public const int MissionOneEnemies = 5;
        public const string FireHint = "TAP HERE TO FIRE";

        /// <summary>The launch opens in mission 1: mission 1 never cleared, and this install's instant start not yet used.</summary>
        public static bool InstantStart(bool missionOneCleared, bool instantStartUsed) => !missionOneCleared && !instantStartUsed;

        /// <summary>The mission plays as the first minute: mission 1 on Recruit, before mission 1 is first cleared.</summary>
        public static bool IsFirstPlay(int mission, DifficultyTier tier, bool missionOneCleared) =>
            mission == Mission && tier == DifficultyTier.Recruit && !missionOneCleared;
    }
}
