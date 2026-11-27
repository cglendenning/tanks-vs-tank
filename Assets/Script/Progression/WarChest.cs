using System;
using System.Collections.Generic;
using System.Globalization;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    /// <summary>One line of a one-time pack (the War Chest, the Starter Pack): a supply track and the units of it the pack holds.</summary>
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
    /// D-047 (R-WAR-CHEST): the War Chest, offered at the player's lowest moment, a lost mission, and bought as often as
    /// they want: every kind of
    /// supply at once (D-045), worth three times its price at the smallest pack's rate, revealed one item at a time.
    /// Every number comes from the supply rules.
    /// </summary>
    public static class WarChest
    {
        public const string Title = "WAR CHEST";
        public const string Subtitle = "ALL OF THIS, ALL AT ONCE";

        /// <summary>How long each item takes to land; the close button waits for the last.</summary>
        public const float RevealSeconds = 0.45f;

        /// <summary>D-047-AC-05: the free test drive: the lost mission again, with everything in the chest on.</summary>
        public const float TestDriveSeconds = 30f;
        public const string TestDriveButton = "TEST DRIVE IT FREE";
        public const string TestDriveNote = "ALL WAR CHEST GEAR ON";
        public const string AfterDriveSubtitle = "THAT WAS THE WAR CHEST. KEEP ALL OF IT";

        /// <summary>D-047-AC-06: the upgrades screen's button, there whenever the chest is on sale.</summary>
        public static string StoreButton(string price) => Title + (string.IsNullOrEmpty(price) ? string.Empty : ": " + price);

        /// <summary>D-047-AC-07: the free War Chest that arrives on result screens, and what it says once taken.</summary>
        public const string FreeButton = "FREE WAR CHEST";
        public const string ArmedLabel = "ARMED: NEXT MISSION";
        public const string FreeNote = "FREE WAR CHEST: ALL GEAR ON";

        /// <summary>D-047-AC-07: the free War Chest arrives after a random 2 to 4 result screens.</summary>
        public const int ArrivalMinScreens = 2;
        public const int ArrivalMaxScreens = 4;

        /// <summary>The drive's countdown, in whole seconds rounded up: TEST DRIVE 0:30.</summary>
        public static string TestDriveClock(float secondsLeft)
        {
            var seconds = Math.Max(0, (int)Math.Ceiling(secondsLeft));
            return "TEST DRIVE " + seconds / 60 + ":" + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public static readonly SupplyStack Stack = new SupplyStack(Title, GemCatalog.WarChest, "war_chest", TestDriveNote,
            AfterDriveSubtitle, new[]
            {
                new WarChestItem(SupplyTrack.Armor, 3),
                new WarChestItem(SupplyTrack.Firepower, 2),
                new WarChestItem(SupplyTrack.Speed, 2),
                new WarChestItem(SupplyTrack.Overdrive, 2),
                new WarChestItem(SupplyTrack.ArcShot, 2),
                new WarChestItem(SupplyTrack.TripleShot, 2),
                new WarChestItem(SupplyTrack.Seeker, 2)
            });

        public static IReadOnlyList<WarChestItem> Items => Stack.Items;

        /// <summary>The supply units in the chest: its gems, one unit per <see cref="SupplyRules.UnitGems"/>.</summary>
        public static int Units => Stack.Units;

        /// <summary>The weapons the chest holds, as WeaponRental bits.</summary>
        public static int WeaponBits => Stack.WeaponBits;

        public static string WeaponsNote => Stack.WeaponsNote;

        public static bool IsWeapon(SupplyTrack track) => SupplyStack.IsWeapon(track);

        public static Weapon WeaponOf(SupplyTrack track) => SupplyStack.WeaponOf(track);

        public static string Line(WarChestItem item) => SupplyStack.Line(item);

        public static string Value(WarChestItem item, string unitPrice) => SupplyStack.Value(item, unitPrice);

        public static string TotalValue(string unitPrice) => Stack.TotalValue(unitPrice);

        public static string YoursFor(string price) => SupplyStack.YoursFor(price);

        public static string BuyButton(string price) => Stack.BuyButton(price);

        /// <summary>D-047-AC-04, D-050-AC-01: a lost mission opens the War Chest once a launch while it is on sale, never
        /// during the ask-free start and never over another War Chest offer.</summary>
        public static bool OpensAfterLoss(bool askFree, bool offeredThisSession, bool onSale, bool alreadyOpen) =>
            !askFree && !offeredThisSession && onSale && !alreadyOpen;

        /// <summary>D-047-AC-08, D-050-AC-01: once the player has seen it, every result screen offers it while it is on
        /// sale, outside the ask-free start.</summary>
        public static bool ShowsOnResultScreen(bool askFree, bool onSale, bool seen) => !askFree && onSale && seen;
    }
}
