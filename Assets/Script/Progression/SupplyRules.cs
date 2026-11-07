using System;

namespace TreadShred.Progression
{
    /// <summary>D-045: what a player buys with gems, in the order the rotation uses it up.</summary>
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
    /// D-045: everything bought with gems is supply, in units of 100 gems ($0.99) with no cap. Every 5 missions won
    /// while any is held, exactly one unit is used up, from the next track in rotation (armor, firepower, speed,
    /// Overdrive, then the special weapons) that holds any, so a player never runs out of one while holding plenty
    /// of another.
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

        /// <summary>The track the next use takes from, looking from <paramref name="next"/>; null when none is held.</summary>
        public static SupplyTrack? NextTrack(int[] units, int next)
        {
            for (var step = 0; step < TrackCount; step++)
            {
                var track = (next + step) % TrackCount;
                if (units[track] > 0)
                    return (SupplyTrack)track;
            }
            return null;
        }

        /// <summary>A mission was won: counts toward the next use and, on the fifth, uses one unit up.</summary>
        public static void MissionWon(int[] units, ref int wins, ref int next)
        {
            var track = NextTrack(units, next);
            if (track == null)
            {
                wins = 0;
                return;
            }
            if (++wins < MissionsPerUse)
                return;
            wins = 0;
            units[(int)track.Value]--;
            next = ((int)track.Value + 1) % TrackCount;
        }

        /// <summary>
        /// The missions won until <paramref name="track"/>'s last unit is used up if nothing more is bought: the
        /// rotation played forward. 0 when the track holds none.
        /// </summary>
        public static int MissionsLeft(int[] units, int wins, int next, SupplyTrack track)
        {
            if (units[(int)track] <= 0)
                return 0;
            var held = (int[])units.Clone();
            var missions = 0;
            while (held[(int)track] > 0)
            {
                missions++;
                MissionWon(held, ref wins, ref next);
            }
            return missions;
        }

        /// <summary>D-045: the missions won until the next unit is used up; 0 when none is held.</summary>
        public static int MissionsToNextUse(int[] units, int wins, int next) =>
            NextTrack(units, next) == null ? 0 : MissionsPerUse - wins;
    }
}
