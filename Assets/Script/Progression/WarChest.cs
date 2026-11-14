using System;
using System.Collections.Generic;
using System.Globalization;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>One line of the War Chest: a supply track and the units of it the chest holds.</summary>
    public readonly struct WarChestItem
    {
        public WarChestItem(SupplyTrack track, int units)
        {
            Track = track;
            Units = units;
        }

        public SupplyTrack Track { get; }
        public int Units { get; }
    }

    /// <summary>
    /// D-047 (R-WAR-CHEST): the one-time War Chest, sold at the player's lowest moment, a lost mission: every kind of
    /// supply at once (D-045), worth three times its price at the smallest pack's rate, revealed one item at a time.
    /// Every number comes from the supply rules.
    /// </summary>
    public static class WarChest
    {
        public const string Title = "WAR CHEST";
        public const string Subtitle = "ONE TIME ONLY. ALL OF THIS, ALL AT ONCE";
        public const string WeaponsNote = "ARC SHOT, TRIPLE SHOT AND SEEKER UNLOCK NOW";

        /// <summary>How long each item takes to land; the close button waits for the last.</summary>
        public const float RevealSeconds = 0.45f;

        public static readonly IReadOnlyList<WarChestItem> Items = new[]
        {
            new WarChestItem(SupplyTrack.Armor, 3),
            new WarChestItem(SupplyTrack.Firepower, 2),
            new WarChestItem(SupplyTrack.Speed, 2),
            new WarChestItem(SupplyTrack.Overdrive, 2),
            new WarChestItem(SupplyTrack.ArcShot, 2),
            new WarChestItem(SupplyTrack.TripleShot, 2),
            new WarChestItem(SupplyTrack.Seeker, 2)
        };

        /// <summary>The supply units in the chest: its gems, one unit per <see cref="SupplyRules.UnitGems"/>.</summary>
        public static int Units
        {
            get
            {
                var units = 0;
                foreach (var item in Items)
                    units += item.Units;
                return units;
            }
        }

        public static bool IsWeapon(SupplyTrack track) => track >= SupplyTrack.ArcShot;

        public static Weapon WeaponOf(SupplyTrack track) => Weapon.ArcShot + ((int)track - (int)SupplyTrack.ArcShot);

        /// <summary>What an item does in the game: the hits, the bonus, or the missions it lasts at least.</summary>
        public static string Line(WarChestItem item)
        {
            switch (item.Track)
            {
                case SupplyTrack.Armor:
                    return "+" + item.Units * SupplyRules.ArmorHitsPerUnit + " ARMOR HITS";
                case SupplyTrack.Firepower:
                    return "+" + Percent(item.Units * SupplyRules.DamagePerUnit) + "% FIREPOWER";
                case SupplyTrack.Speed:
                    return "+" + Percent(item.Units * SupplyRules.SpeedPerUnit) + "% SPEED";
                case SupplyTrack.Overdrive:
                    return "OVERDRIVE: " + item.Units * SupplyRules.MissionsPerUse + "+ MISSIONS";
                default:
                    return WeaponRules.Name(WeaponOf(item.Track)) + ": " + item.Units * SupplyRules.MissionsPerUse + "+ MISSIONS";
            }
        }

        /// <summary>
        /// An item's worth at the smallest pack's price (<paramref name="unitPrice"/>, one unit of supply); empty when
        /// that price is not in dollars.
        /// </summary>
        public static string Value(WarChestItem item, string unitPrice) =>
            OfferLabels.TryDollars(unitPrice, out var unit) ? Dollars(unit * item.Units) : string.Empty;

        /// <summary>The whole chest's worth, in dollars when the store's prices are, else as a multiple of its price.</summary>
        public static string TotalValue(string unitPrice)
        {
            if (OfferLabels.TryDollars(unitPrice, out var unit))
                return "TOTAL VALUE " + Dollars(unit * Units);
            return Units * GemCatalog.Packs[0].UsdCents / GemCatalog.WarChest.UsdCents + "X THE GEAR FOR THE PRICE";
        }

        public static string YoursFor(string price) => string.IsNullOrEmpty(price) ? "YOURS NOW" : "YOURS FOR " + price;

        public static string BuyButton(string price) =>
            "GET THE WAR CHEST" + (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        private static string Percent(float share) => Math.Round(share * 100f).ToString(CultureInfo.InvariantCulture);

        private static string Dollars(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
