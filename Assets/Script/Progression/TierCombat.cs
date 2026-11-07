using System;

namespace TreadShred.Progression
{
    /// <summary>D-046-AC-04: the weapons a Veteran or Elite volley cycles through, in order.</summary>
    public enum EnemyWeapon
    {
        Cannon,
        TripleShot,
        ArcShot,
        Seeker
    }

    /// <summary>
    /// D-046: how Veteran and Elite enemies fight harder than Recruit's, not just tougher: the map's enemies come back
    /// in waves, they spot the player from farther, their shells fly faster, Elite's lead their shots, and every
    /// attack is a volley that cycles through every weapon. Recruit fights as it always has.
    /// </summary>
    public readonly struct TierCombat
    {
        public const float VolleyGap = 0.25f;
        public const float TripleSpreadDegrees = 12f;
        public const int WeaponCount = 4;

        public TierCombat(int waves, float sightRange, float shellSpeed, bool leadsShots, int volleyShots)
        {
            Waves = waves;
            SightRange = sightRange;
            ShellSpeed = shellSpeed;
            LeadsShots = leadsShots;
            VolleyShots = volleyShots;
        }

        /// <summary>D-046-AC-01: how many times the map's enemies come at the player, the first included.</summary>
        public int Waves { get; }

        /// <summary>D-046-AC-02: how far enemies spot the player, as a multiple of Recruit's.</summary>
        public float SightRange { get; }

        /// <summary>D-046-AC-02: how fast enemy shells fly, as a multiple of Recruit's.</summary>
        public float ShellSpeed { get; }

        /// <summary>D-046-AC-03: enemies aim where the player's tank will be when the shell arrives.</summary>
        public bool LeadsShots { get; }

        /// <summary>D-046-AC-04: shells in each attack's volley; 0 keeps Recruit's original two-shell attack.</summary>
        public int VolleyShots { get; }

        public static TierCombat For(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Veteran:
                    return new TierCombat(2, 1.5f, 1.3f, false, 3);
                case DifficultyTier.Elite:
                    return new TierCombat(3, 1.5f, 1.3f, true, 4);
                default:
                    return new TierCombat(1, 1f, 1f, false, 0);
            }
        }

        /// <summary>D-046-AC-04: an enemy's shot number n is the next weapon in the cycle.</summary>
        public static EnemyWeapon VolleyWeapon(int shot) => (EnemyWeapon)(((shot % WeaponCount) + WeaponCount) % WeaponCount);

        /// <summary>D-046-AC-01: the next wave comes once half of the current one is destroyed.</summary>
        public static bool NextWaveDue(int destroyedThisWave, int waveSize, int wavesLeft) =>
            wavesLeft > 0 && waveSize > 0 && destroyedThisWave * 2 >= waveSize;

        /// <summary>
        /// D-046-AC-03: where on the ground to aim so a shell at <paramref name="shellSpeed"/> meets a target moving at
        /// (<paramref name="velocityX"/>, <paramref name="velocityZ"/>): the target's position after the shell's flight
        /// time, refined once. Positions are on the ground plane (x, z).
        /// </summary>
        public static (float x, float z) Lead(float fromX, float fromZ, float targetX, float targetZ,
            float velocityX, float velocityZ, float shellSpeed)
        {
            if (shellSpeed <= 0f)
                return (targetX, targetZ);
            var flight = Distance(fromX, fromZ, targetX, targetZ) / shellSpeed;
            flight = Distance(fromX, fromZ, targetX + velocityX * flight, targetZ + velocityZ * flight) / shellSpeed;
            return (targetX + velocityX * flight, targetZ + velocityZ * flight);
        }

        private static float Distance(float ax, float az, float bx, float bz) =>
            (float)Math.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
    }
}
