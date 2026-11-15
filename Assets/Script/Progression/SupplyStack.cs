using System;
using System.Collections.Generic;
using System.Globalization;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>
    /// A one-time pack sold as everything at once (the War Chest, D-047; the Starter Pack, D-037): the supply it equips,
    /// item by item, what each item does and its worth at the smallest pack's price. Every number comes from the supply
    /// rules; the pack's gems are exactly its units.
    /// </summary>
    public sealed class SupplyStack
    {
        public SupplyStack(string title, GemPack pack, string offer, string testDriveNote, string afterDriveSubtitle,
            IReadOnlyList<WarChestItem> items)
        {
            Title = title;
            Pack = pack;
            Offer = offer;
            TestDriveNote = testDriveNote;
            AfterDriveSubtitle = afterDriveSubtitle;
            Items = items;
        }

        public string Title { get; }
        public GemPack Pack { get; }

        /// <summary>The offer's name in analytics (offer_shown, offer_accepted).</summary>
        public string Offer { get; }

        public string TestDriveNote { get; }
        public string AfterDriveSubtitle { get; }
        public IReadOnlyList<WarChestItem> Items { get; }

        /// <summary>The supply units in the stack: its gems, one unit per <see cref="SupplyRules.UnitGems"/>.</summary>
        public int Units
        {
            get
            {
                var units = 0;
                foreach (var item in Items)
                    units += item.Units;
                return units;
            }
        }

        /// <summary>The weapons the stack holds, as WeaponRental bits.</summary>
        public int WeaponBits
        {
            get
            {
                var bits = 0;
                foreach (var item in Items)
                {
                    if (IsWeapon(item.Track))
                        bits |= WeaponRental.Bit(WeaponOf(item.Track));
                }
                return bits;
            }
        }

        /// <summary>The units of a track the stack holds.</summary>
        public int UnitsOf(SupplyTrack track)
        {
            var units = 0;
            foreach (var item in Items)
            {
                if (item.Track == track)
                    units += item.Units;
            }
            return units;
        }

        /// <summary>ARC SHOT, TRIPLE SHOT AND SEEKER UNLOCK NOW; SEEKER UNLOCKS NOW.</summary>
        public string WeaponsNote
        {
            get
            {
                var names = new List<string>();
                foreach (var item in Items)
                {
                    if (IsWeapon(item.Track))
                        names.Add(Name(item.Track));
                }
                return Joined(names) + (names.Count == 1 ? " UNLOCKS NOW" : " UNLOCK NOW");
            }
        }

        public static bool IsWeapon(SupplyTrack track) => track >= SupplyTrack.ArcShot;

        public static Weapon WeaponOf(SupplyTrack track) => Weapon.ArcShot + ((int)track - (int)SupplyTrack.ArcShot);

        /// <summary>A track as the player knows it: ARMOR, FIREPOWER, SPEED, OVERDRIVE or the weapon's name.</summary>
        public static string Name(SupplyTrack track)
        {
            switch (track)
            {
                case SupplyTrack.Armor:
                    return "ARMOR";
                case SupplyTrack.Firepower:
                    return "FIREPOWER";
                case SupplyTrack.Speed:
                    return "SPEED";
                case SupplyTrack.Overdrive:
                    return "OVERDRIVE";
                default:
                    return WeaponRules.Name(WeaponOf(track));
            }
        }

        /// <summary>A, A AND B, A, B AND C.</summary>
        public static string Joined(IReadOnlyList<string> names)
        {
            if (names.Count < 2)
                return names.Count == 0 ? string.Empty : names[0];
            var head = new string[names.Count - 1];
            for (var index = 0; index < head.Length; index++)
                head[index] = names[index];
            return string.Join(", ", head) + " AND " + names[names.Count - 1];
        }

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

        /// <summary>The whole stack's worth, in dollars when the store's prices are, else as a multiple of its price.</summary>
        public string TotalValue(string unitPrice)
        {
            if (OfferLabels.TryDollars(unitPrice, out var unit))
                return "TOTAL VALUE " + Dollars(unit * Units);
            return Units * GemCatalog.Packs[0].UsdCents / Pack.UsdCents + "X THE GEAR FOR THE PRICE";
        }

        public static string YoursFor(string price) => string.IsNullOrEmpty(price) ? "YOURS NOW" : "YOURS FOR " + price;

        public string BuyButton(string price) => "GET THE " + Title + (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        private static string Percent(float share) => Math.Round(share * 100f).ToString(CultureInfo.InvariantCulture);

        private static string Dollars(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
