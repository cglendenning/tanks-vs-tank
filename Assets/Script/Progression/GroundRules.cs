using System;

namespace TreadShred.Progression
{
    /// <summary>D-039: the arena ground changes every five missions (R-GROUNDS).</summary>
    public static class GroundRules
    {
        public const int MissionsPerGround = 5;
        public const int Grounds = 8;

        // R-GROUNDS tiling across the arena plane, so each ground's detail reads at the arena camera's distance.
        public const float TilingX = 6f;
        public const float TilingY = 5f;

        /// <summary>The ground (1 to 8) a mission is fought on: missions 1 to 5 on the first, 36 to 40 on the eighth.</summary>
        public static int GroundFor(int mission) => Math.Min(Grounds, Math.Max(1, (mission - 1) / MissionsPerGround + 1));

        public static string ResourcePath(int ground) => "Ground/ground-" + ground;
    }
}
