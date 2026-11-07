using System.Collections.Generic;

namespace TreadShred.Progression
{
    /// <summary>What a weapon is in the drawer (D-038-AC-02, D-043-AC-02): free to use, rented, or locked.</summary>
    public enum DrawerState
    {
        Available,
        Rented,
        Locked
    }

    public readonly struct DrawerEntry
    {
        public DrawerEntry(Weapon weapon, DrawerState state, int missionsLeft)
        {
            Weapon = weapon;
            State = state;
            MissionsLeft = missionsLeft;
        }

        public Weapon Weapon { get; }
        public DrawerState State { get; }
        public int MissionsLeft { get; }
    }

    /// <summary>
    /// D-043 (R-WEAPON-RENTAL): a special weapon the player has used stays rentable on every later mission of every
    /// tier, <see cref="MissionsWon"/> missions won at a time for <see cref="CostGems"/> gems; each weapon has its own
    /// rental, and renting one that is still running adds <see cref="RepurchaseAdds"/> missions.
    /// </summary>
    public static class WeaponRental
    {
        public const int CostGems = 100;
        public const int MissionsWon = 5;
        public const int RepurchaseAdds = 5;
        public const string LockedLabel = "LOCKED";
        public const string Decline = "NOT NOW";

        public static string OfferTitle(Weapon weapon) => WeaponRules.Name(weapon) + " FOR " + MissionsWon + " MISSIONS";

        /// <summary>D-043-AC-06: the rental countdown's label.</summary>
        public static string Left(int missions) => missions + " LEFT";

        /// <summary>The bar's fill: missions left out of a full rental (a stacked rental shows full).</summary>
        public static float BarFill(int missions) => missions <= 0 ? 0f : missions >= MissionsWon ? 1f : (float)missions / MissionsWon;

        public static int Bit(Weapon weapon) => 1 << (int)weapon;

        /// <summary>
        /// D-038-AC-02: the drawer for a mission: the cannon, the mission's own special weapon, then every weapon the
        /// player has used, in the order the game introduces them, each available, rented or locked.
        /// </summary>
        public static IReadOnlyList<DrawerEntry> Drawer(Weapon native, int usedWeapons, IReadOnlyList<int> rentals)
        {
            var entries = new List<DrawerEntry> { new DrawerEntry(Weapon.Cannon, DrawerState.Available, 0) };
            if (native != Weapon.Cannon)
                entries.Add(new DrawerEntry(native, DrawerState.Available, 0));
            for (var weapon = Weapon.ArcShot; weapon <= Weapon.Seeker; weapon++)
            {
                if (weapon == native || (usedWeapons & Bit(weapon)) == 0)
                    continue;
                var left = rentals[(int)weapon];
                entries.Add(new DrawerEntry(weapon, left > 0 ? DrawerState.Rented : DrawerState.Locked, left));
            }
            return entries;
        }

        /// <summary>D-038-AC-02, D-043-AC-05: a special weapon's mission starts on it; a cannon-only mission starts on
        /// the latest-introduced weapon with a rental running, else the cannon.</summary>
        public static Weapon Starting(Weapon native, IReadOnlyList<int> rentals)
        {
            if (native != Weapon.Cannon)
                return native;
            for (var weapon = Weapon.Seeker; weapon > Weapon.Cannon; weapon--)
            {
                if (rentals[(int)weapon] > 0)
                    return weapon;
            }
            return Weapon.Cannon;
        }
    }
}
