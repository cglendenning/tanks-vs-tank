namespace TreadShred.Progression
{
    /// <summary>
    /// D-043 (R-WEAPON-RENTAL): a special weapon the player has used can be rented on cannon-only missions for
    /// <see cref="MissionsWon"/> missions won, for <see cref="CostGems"/> gems.
    /// </summary>
    public static class WeaponRental
    {
        public const int CostGems = 100;
        public const int MissionsWon = 5;
        public const string LockedLabel = "LOCKED";
        public const string Decline = "NOT NOW";

        public static string OfferTitle(Weapon weapon) => WeaponRules.Name(weapon) + " FOR " + MissionsWon + " MISSIONS";

        public static string MissionsLeft(int left) => left == 1 ? "1 MISSION LEFT" : left + " MISSIONS LEFT";

        /// <summary>The weapon a cannon-only mission offers: the rented one while a rental runs, else the most recently
        /// introduced weapon the player has used (weapons are introduced in enum order), else none.</summary>
        public static Weapon? Offered(int usedWeapons, Weapon? rented)
        {
            if (rented is Weapon active)
                return active;
            for (var weapon = Weapon.Seeker; weapon > Weapon.Cannon; weapon--)
            {
                if ((usedWeapons & Bit(weapon)) != 0)
                    return weapon;
            }
            return null;
        }

        public static int Bit(Weapon weapon) => 1 << (int)weapon;
    }
}
