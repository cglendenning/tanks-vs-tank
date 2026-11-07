using System.Collections.Generic;
using System.Globalization;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>The items sold with a clip of them working (D-034, R-SALES-CLIPS).</summary>
    public enum SaleItem
    {
        Overdrive,
        ArmoredRedeploy,
        Armor,
        Firepower,
        Speed,
        // D-043: the special weapons, rented on cannon-only missions.
        ArcShot,
        TripleShot,
        Seeker
    }

    public readonly struct SalesPitch
    {
        public SalesPitch(string title, string outcome, IReadOnlyList<string> points)
        {
            Title = title;
            Outcome = outcome;
            Points = points;
        }

        public string Title { get; }
        public string Outcome { get; }
        public IReadOnlyList<string> Points { get; }
    }

    /// <summary>
    /// What each sales screen says (D-034-AC-03): the outcome an item buys and only effects it really has, with
    /// numbers read from the rules so the copy cannot drift from the game.
    /// </summary>
    public static class SalesPitches
    {
        public const string ClipFolder = "SalesClips";
        public const string ClipCaption = "SEE IT IN ACTION";
        // Armored Redeploy resumes the SCORE run the lost tank ended (D-032); the free retry starts it over.
        public const string RunContinues = "YOUR SCORE RUN CONTINUES";
        private const string Permanent = "EVERY LEVEL IS YOURS FOR GOOD";

        public static readonly SaleItem[] All =
        {
            SaleItem.Overdrive, SaleItem.ArmoredRedeploy, SaleItem.Armor, SaleItem.Firepower, SaleItem.Speed,
            SaleItem.ArcShot, SaleItem.TripleShot, SaleItem.Seeker
        };
        private static readonly string RentalTerm = "YOURS FOR " + WeaponRental.MissionsWon + " MISSIONS WON";

        public static string ClipName(SaleItem item)
        {
            switch (item)
            {
                case SaleItem.Overdrive: return "overdrive";
                case SaleItem.ArmoredRedeploy: return "armored-redeploy";
                case SaleItem.Armor: return "armor";
                case SaleItem.Firepower: return "firepower";
                case SaleItem.ArcShot: return "arc-shot";
                case SaleItem.TripleShot: return "triple-shot";
                case SaleItem.Seeker: return "seeker";
                default: return "speed";
            }
        }

        /// <summary>The clip's path under Resources, without extension.</summary>
        public static string ClipResource(SaleItem item) => ClipFolder + "/" + ClipName(item);

        public static SaleItem ForOffer(GemOffer offer) =>
            offer == GemOffer.Overdrive ? SaleItem.Overdrive : SaleItem.ArmoredRedeploy;

        public static SaleItem ForWeapon(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.ArcShot: return SaleItem.ArcShot;
                case Weapon.TripleShot: return SaleItem.TripleShot;
                case Weapon.Seeker: return SaleItem.Seeker;
                default: throw new System.ArgumentOutOfRangeException(nameof(weapon), weapon, "The cannon is never sold.");
            }
        }

        public static SaleItem ForTrack(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.Armor: return SaleItem.Armor;
                case UpgradeTrack.Firepower: return SaleItem.Firepower;
                default: return SaleItem.Speed;
            }
        }

        public static SalesPitch For(SaleItem item)
        {
            var top = ProgressionRules.LevelsPerTrack;
            var atTop = " AT LEVEL " + top;
            switch (item)
            {
                case SaleItem.Overdrive:
                    return new SalesPitch("OVERDRIVE", "TWO SHOTS EVERY TAP",
                        new[] { OfferLabels.OverdriveLasts, "DOUBLE THE ROUNDS ON EVERY TARGET" });
                case SaleItem.ArmoredRedeploy:
                    return new SalesPitch("ARMORED REDEPLOY", "GET BACK IN AND FINISH THE JOB",
                        new[] { "REPLAY THIS MISSION", "WITH " + CombatBoosts.RedeployExtraHits + " EXTRA HIT OF ARMOR", RunContinues });
                case SaleItem.Armor:
                    // D-045: armor is bought as supply, with no cap.
                    return new SalesPitch("ARMOR", "SHRUG OFF THE HITS",
                        new[] { ProgressionLabels.SupplyUnit(UpgradeTrack.Armor) + " FOR EVERY " + SupplyRules.UnitGems + " GEMS", OfferLabels.SupplyLasts });
                case SaleItem.Firepower:
                    return new SalesPitch("FIREPOWER", "HIT HARDER. KILL FASTER.",
                        new[] { "UP TO X" + Multiplier(UpgradeEffects.DamageMultiplier(top), "0.0") + " DAMAGE" + atTop, Permanent });
                case SaleItem.ArcShot:
                    return new SalesPitch(WeaponRental.OfferTitle(Weapon.ArcShot), "HIT THEM BEHIND THEIR WALLS",
                        new[] { "SHELLS ARC OVER COVER", RentalTerm });
                case SaleItem.TripleShot:
                    return new SalesPitch(WeaponRental.OfferTitle(Weapon.TripleShot), "THREE SHELLS EVERY TAP",
                        new[] { "A SPREAD THAT IS HARD TO DODGE", RentalTerm });
                case SaleItem.Seeker:
                    return new SalesPitch(WeaponRental.OfferTitle(Weapon.Seeker), "SHELLS THAT HUNT THEM DOWN",
                        new[] { "HOMING SHELLS STEER AROUND WALLS", RentalTerm });
                default:
                    return new SalesPitch("SPEED", "OUTMANEUVER EVERY TANK",
                        new[] { "UP TO X" + Multiplier(UpgradeEffects.SpeedMultiplier(top), "0.00") + " MOVE AND TURRET SPEED" + atTop, Permanent });
            }
        }

        private static string Multiplier(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
