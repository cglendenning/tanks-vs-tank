namespace TreadShred.Progression
{
    /// <summary>
    /// D-050: a new player's first five missions ask for nothing. Until mission 5 is first cleared on Recruit, no revive
    /// offer, result-screen card, NEXT MISSION offer, Starter Pack or War Chest opens; the UPGRADES button stays. It is
    /// read from progress alone, so a player whose restored progress is past mission 5 never gets it and a reset (a
    /// fresh start) brings it back. After it, every screen, replays of missions 1 to 5 included, is as for any player.
    /// </summary>
    public static class AskFreeStart
    {
        /// <summary>The last mission of the ask-free start.</summary>
        public const int LastMission = 5;

        /// <summary>The start is still ask-free: mission 5 has never been cleared on Recruit.</summary>
        public static bool InProgress(PlayerProgression progression) =>
            !progression.IsCleared(DifficultyTier.Recruit, LastMission);
    }
}
