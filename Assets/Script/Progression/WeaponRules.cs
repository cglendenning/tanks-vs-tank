using System;

namespace TreadShred.Progression
{
    /// <summary>D-038: the weapons a mission's tank can carry, in the order of PlayerMoverment.PlayerType.</summary>
    public enum Weapon
    {
        Cannon,
        ArcShot,
        TripleShot,
        Seeker
    }

    /// <summary>
    /// D-038: the weapon in hand is named, and a mission whose tank carries a special weapon lets the player switch
    /// between it and the cannon (R-WEAPONS).
    /// </summary>
    public static class WeaponRules
    {
        public const string SwitchHint = "TAP TO SWITCH";
        public const float SwitchHintSeconds = 6f;

        public static string Name(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.Cannon:
                    return "CANNON";
                case Weapon.ArcShot:
                    return "ARC SHOT";
                case Weapon.TripleShot:
                    return "TRIPLE SHOT";
                case Weapon.Seeker:
                    return "SEEKER";
                default:
                    throw new ArgumentOutOfRangeException(nameof(weapon), weapon, "Unknown weapon.");
            }
        }

        /// <summary>D-038-AC-01/02: only a mission with a special weapon shows the toggle.</summary>
        public static bool CanSwitch(Weapon missionWeapon) => missionWeapon != Weapon.Cannon;

        /// <summary>D-038-AC-03: the weapon in hand after a tap on the other side of the toggle.</summary>
        public static Weapon Switch(Weapon missionWeapon, Weapon inHand) =>
            !CanSwitch(missionWeapon) ? Weapon.Cannon : inHand == Weapon.Cannon ? missionWeapon : Weapon.Cannon;
    }
}
