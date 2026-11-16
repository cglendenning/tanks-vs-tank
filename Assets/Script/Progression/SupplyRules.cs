using System;

namespace TreadShred.Progression
{
    /// <summary>D-045: what a player buys with gems.</summary>
    public enum SupplyTrack
    {
        Armor,
        Firepower,
        Speed,
        Overdrive,
        ArcShot,
        TripleShot,
        Seeker
    }

    /// <summary>
    /// D-045: everything bought with gems is supply, in units of 100 gems ($0.99) with no cap. Each track that holds any
    /// runs its own clock: every 5 missions won it uses one of its units up, all tracks at once (owner decision
    /// 2026-11-15), so a unit is always 5 missions won of its track, whatever else is held.
    /// </summary>
    public static class SupplyRules
    {
        public const int TrackCount = 7;
        public const int UnitGems = 100;
        public const int MissionsPerUse = 5;
        public const int ArmorHitsPerUnit = 2;
        public const float DamagePerUnit = 0.2f;
        public const float SpeedPerUnit = 0.1f;

        public static SupplyTrack ForWeapon(Weapon weapon)
        {
            if (weapon == Weapon.Cannon)
                throw new ArgumentException("The cannon is never supply.", nameof(weapon));
            return SupplyTrack.ArcShot + ((int)weapon - (int)Weapon.ArcShot);
        }

        public static int Total(int[] units)
        {
            var total = 0;
            foreach (var count in units)
                total += Math.Max(0, count);
            return total;
        }

        /// <summary>
        /// A mission was won: every track that holds any counts it toward its next use and, on its fifth, uses one unit
        /// up. A track that holds none keeps its clock at zero, so a unit bought later lasts its full 5 missions.
        /// </summary>
        public static void MissionWon(int[] units, int[] wins)
        {
            for (var track = 0; track < units.Length; track++)
            {
                if (units[track] <= 0)
                {
                    wins[track] = 0;
                    continue;
                }
                if (++wins[track] < MissionsPerUse)
                    continue;
                wins[track] = 0;
                units[track]--;
            }
        }

        /// <summary>The missions won until <paramref name="track"/>'s last unit is used up if nothing more is bought; 0 when it holds none.</summary>
        public static int MissionsLeft(int[] units, int[] wins, SupplyTrack track) =>
            units[(int)track] <= 0 ? 0 : units[(int)track] * MissionsPerUse - wins[(int)track];
    }
}
