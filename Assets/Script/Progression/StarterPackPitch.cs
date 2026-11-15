using System.Collections.Generic;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-037 (R-STARTER-PACK): the Starter Pack, sold like the War Chest as a stack of supply on every main track plus a
    /// special weapon, and in what it does in the game, not in gems (a new player does not know what a gem is): the
    /// armor it adds, as a multiple of the tank's own only when that is true. Every number comes from the rules.
    /// </summary>
    public static class StarterPackPitch
    {
        public const string Limit = OfferLabels.StarterPackLimit;
        public const string TestDriveNote = "ALL STARTER PACK GEAR ON";
        public const string AfterDriveSubtitle = "THAT WAS THE STARTER PACK. KEEP ALL OF IT";

        public static readonly SupplyStack Stack = new SupplyStack(OfferLabels.StarterPackTitle, GemCatalog.StarterPack, "starter_pack",
            TestDriveNote, AfterDriveSubtitle, new[]
            {
                new WarChestItem(SupplyTrack.Armor, 2),
                new WarChestItem(SupplyTrack.Firepower, 1),
                new WarChestItem(SupplyTrack.Speed, 1),
                new WarChestItem(SupplyTrack.Overdrive, 1),
                new WarChestItem(SupplyTrack.Seeker, 1)
            });

        /// <summary>The armor hits the pack's armor supply adds (D-045).</summary>
        public static int ArmorHits => Stack.UnitsOf(SupplyTrack.Armor) * SupplyRules.ArmorHitsPerUnit;

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

        /// <summary>D-037-AC-03: the revive panel leads with the revive it includes and the armor.</summary>
        public static string ReviveHeadline(int armorHits) => "REVIVE FREE + " + Headline(armorHits);

        /// <summary>D-037-AC-03: everything else in the pack, under the revive panel's headline.</summary>
        public static string ReviveAlternative
        {
            get
            {
                var names = new List<string>();
                foreach (var item in Stack.Items)
                {
                    if (item.Track != SupplyTrack.Armor)
                        names.Add(SupplyStack.Name(item.Track));
                }
                return "PLUS " + SupplyStack.Joined(names);
            }
        }

        public static string ReviveButton(string price) => "REVIVE + STARTER PACK" + (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        /// <summary>D-037-AC-04: the gem store's button in place of the smallest pack.</summary>
        public static string StoreButton(int armorHits, string price) =>
            "STARTER PACK: " + Headline(armorHits).Replace(" YOUR", string.Empty) + " + MORE" +
            (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);
    }
}
