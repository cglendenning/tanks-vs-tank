using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-037-AC-03/04: the Starter Pack sold in what it does in the game, not in gems (a new player does not know what a
    /// gem is): the armor it adds, as a multiple of the tank's own only when that is true, and the revives, Overdrive
    /// missions or firepower levels it buys instead. Every number comes from the rules.
    /// </summary>
    public static class StarterPackPitch
    {
        private static int Gems => GemCatalog.StarterPack.Gems;

        /// <summary>The supply units the pack's gems buy (D-045).</summary>
        public static int Units => Gems / SupplyRules.UnitGems;

        public static int ArmorHits => Units * SupplyRules.ArmorHitsPerUnit;

        public static int Revives => Gems / ProgressionRules.ReviveCostsGems[0];

        public static int OverdriveMissions => Gems / GemCatalog.Cost(GemOffer.Overdrive) * SupplyRules.MissionsPerUse;

        /// <summary>How many of the regular smallest pack the Starter Pack is worth.</summary>
        public static int ValueMultiple => Gems / GemCatalog.Packs[0].Gems;

        /// <summary>TRIPLE YOUR ARMOR only when the hits truly triple the tank's own; DOUBLE when they at least double it.</summary>
        public static string Headline(int armorHits)
        {
            var total = armorHits + ArmorHits;
            if (total >= 3 * armorHits)
                return "TRIPLE YOUR ARMOR";
            if (total >= 2 * armorHits)
                return "DOUBLE YOUR ARMOR";
            return "+" + ArmorHits + " ARMOR HITS";
        }

        public static string Alternatives => "OR " + Revives + " REVIVES · OR " + OverdriveMissions + " MISSIONS OF OVERDRIVE";

        /// <summary>The firepower levels the gems buy from the tank's current level (coins first, D-018), or, at the top, its supply.</summary>
        public static string Firepower(int level)
        {
            var coins = Gems * ProgressionRules.CoinsPerGem;
            var levels = 0;
            while (level + levels < ProgressionRules.LevelsPerTrack && coins >= (level + levels + 1) * ProgressionRules.CostStepCoins)
            {
                coins -= (level + levels + 1) * ProgressionRules.CostStepCoins;
                levels++;
            }
            if (levels > 0)
                return "OR " + levels + (levels == 1 ? " FIREPOWER UPGRADE" : " FIREPOWER UPGRADES");
            return "OR +" + Math.Round(Units * SupplyRules.DamagePerUnit * 100f) + "% FIREPOWER";
        }

        /// <summary>The pack's worth against the regular pack's price; in plain multiples when the store's prices are not in dollars.</summary>
        public static string ValueLine(string regularPrice, string starterPrice)
        {
            if (Dollars(regularPrice, out var regular) && !string.IsNullOrEmpty(starterPrice))
                return "$" + (regular * ValueMultiple).ToString("0.00", CultureInfo.InvariantCulture) + " OF GEAR FOR " + starterPrice +
                    ". YOU PICK WHAT.";
            return ValueMultiple + "X THE GEAR OF A REGULAR PACK. YOU PICK WHAT.";
        }

        public static string GemsNote => Gems + " GEMS: SPEND THEM ON ANY OF THESE";

        public static string BuyButton(string price) => string.IsNullOrEmpty(price) ? "GET IT" : "GET IT FOR " + price;

        /// <summary>D-037-AC-03: the revive panel leads with the revive it includes and the armor.</summary>
        public static string ReviveHeadline(int armorHits) => "REVIVE FREE + " + Headline(armorHits);

        public static string ReviveAlternative => "OR " + Revives + " MORE REVIVES";

        public static string ReviveButton(string price) => "REVIVE + STARTER PACK" + (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        /// <summary>D-037-AC-04: the gem store's button in place of the smallest pack.</summary>
        public static string StoreButton(int armorHits, string price) =>
            "STARTER PACK: " + Headline(armorHits).Replace(" YOUR", string.Empty) + " + MORE" +
            (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        private static bool Dollars(string price, out decimal amount)
        {
            amount = 0m;
            var match = price == null ? null : Regex.Match(price, @"^\$(\d+\.\d{2})$");
            return match != null && match.Success &&
                decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
