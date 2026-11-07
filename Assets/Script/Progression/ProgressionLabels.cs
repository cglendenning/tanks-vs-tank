using System;
using TreadShred.Economy;
using System.Collections.Generic;

namespace TreadShred.Progression
{
    // Plain command-voice copy for progression screens (D-028).
    public static class ProgressionLabels
    {
        public static string Reward(MissionReward reward)
        {
            var parts = new List<string> { "+" + CountText.Format(reward.Coins) + " COINS" };
            if (reward.Gems > 0)
                parts.Add("+" + CountText.Format(reward.Gems) + " GEMS MILESTONE REWARD");
            if (reward.UnlockedTier.HasValue)
                parts.Add(Tier(reward.UnlockedTier.Value) + " UNLOCKED");
            return string.Join("   ", parts);
        }

        public static string Tier(DifficultyTier tier)
        {
            return tier.ToString().ToUpperInvariant();
        }

        public static string Track(UpgradeTrack track)
        {
            return track.ToString().ToUpperInvariant();
        }

        public static string Effect(UpgradeTrack track, int level)
        {
            switch (track)
            {
                case UpgradeTrack.Armor:
                    return UpgradeEffects.ArmorHits(level) + " HITS";
                case UpgradeTrack.Firepower:
                    return "X" + UpgradeEffects.DamageMultiplier(level).ToString("0.0") + " DAMAGE";
                default:
                    return "X" + UpgradeEffects.SpeedMultiplier(level).ToString("0.00") + " SPEED";
            }
        }

        /// <summary>D-045: what one unit of a track's supply adds.</summary>
        public static string SupplyUnit(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.Armor:
                    return "+" + SupplyRules.ArmorHitsPerUnit + " HITS";
                case UpgradeTrack.Firepower:
                    return "+" + Math.Round(SupplyRules.DamagePerUnit * 100f) + "% DAMAGE";
                default:
                    return "+" + Math.Round(SupplyRules.SpeedPerUnit * 100f) + "% SPEED";
            }
        }

        /// <summary>D-045: the effect of the levels owned and the supply held, and what the next unit adds.</summary>
        public static string SupplyPreview(UpgradeTrack track, int level, int units)
        {
            string now;
            switch (track)
            {
                case UpgradeTrack.Armor:
                    now = UpgradeEffects.ArmorHits(level) + SupplyRules.ArmorHitsPerUnit * units + " HITS";
                    break;
                case UpgradeTrack.Firepower:
                    now = "X" + (UpgradeEffects.DamageMultiplier(level) + SupplyRules.DamagePerUnit * units).ToString("0.0") + " DAMAGE";
                    break;
                default:
                    now = "X" + (UpgradeEffects.SpeedMultiplier(level) + SupplyRules.SpeedPerUnit * units).ToString("0.00") + " SPEED";
                    break;
            }
            return now + "  >  " + SupplyUnit(track);
        }

        /// <summary>D-045: the supply a track holds and the missions won it lasts.</summary>
        public static string SupplyLine(int units, int missionsLeft) =>
            units > 0 ? "SUPPLY " + units + ": " + missionsLeft + " MISSIONS LEFT" : "NO SUPPLY";

        /// <summary>D-017-AC-10: the mission HUD's upgraded tracks, one line each in track order ("ARMOR LEVEL 3"); none with none.</summary>
        public static IReadOnlyList<string> ActiveUpgrades(Func<UpgradeTrack, int> level)
        {
            var lines = new List<string>();
            foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
            {
                if (level(track) > 0)
                    lines.Add(Track(track) + " LEVEL " + level(track));
            }
            return lines;
        }

        /// <summary>D-017-AC-10: armor beyond the shields the HUD draws, as "+N"; empty when there is none.</summary>
        public static string ExtraArmor(int hitsLeft, int shieldsDrawn)
        {
            return hitsLeft > shieldsDrawn ? "+" + (hitsLeft - shieldsDrawn) : string.Empty;
        }

        // Current effect and what the next purchase leads to. Armor gains a hit
        // every second level, so name the level where the next hit arrives.
        public static string Preview(UpgradeTrack track, int level)
        {
            if (level >= ProgressionRules.LevelsPerTrack)
                return Effect(track, level);
            if (track != UpgradeTrack.Armor || UpgradeEffects.ArmorHits(level + 1) > UpgradeEffects.ArmorHits(level))
                return Effect(track, level) + "  >  " + Effect(track, level + 1);
            return Effect(track, level) + "  >  " + Effect(track, level + 2) + " AT LEVEL " + (level + 2);
        }

        public const string Undo = "UNDO UPGRADE";

        /// <summary>The same undo on the victory screen's upgrade card, where the row is narrow (D-036-AC-03).</summary>
        public const string UndoShort = "UNDO";
        public const string UpgradesButton = "UPGRADES";

        /// <summary>D-018: the upgrade screen's note on where each currency comes from and what it buys.</summary>
        public const string CurrencyNoteTitle = "COINS AND GEMS";

        /// <summary>D-036-AC-04: what each currency is for, how an upgrade spends them, and what a priced button does.</summary>
        public static IReadOnlyList<string> CurrencyNote() => new[]
        {
            "COINS: YOU EARN THEM IN EVERY MISSION, WIN OR LOSE. THEY BUY FIREPOWER AND SPEED.",
            "GEMS: BUY THEM OR EARN THEM AT MILESTONES. THEY BUY ARMOR, OVERDRIVE, REVIVES AND WEAPONS.",
            "FIREPOWER AND SPEED USE YOUR COINS FIRST, THEN GEMS FOR THE REST: 1 GEM = " + ProgressionRules.CoinsPerGem + " COINS.",
            "A BUTTON SHOWING A PRICE BUYS IT AND APPLIES IT IN ONE TAP."
        };

        /// <summary>
        /// D-017-AC-05, D-036-AC-03: the one confirmation an upgrade can need, before it spends gems the player
        /// already holds, naming the coins and gems it takes. When gems fall short the store's own sheet is the only
        /// confirmation (D-036-AC-02), so there is no dialog to describe.
        /// </summary>
        public static GemConfirmation UpgradeConfirmation(UpgradeTrack track, int nextLevel, UpgradeQuote quote, int gemBalance)
        {
            if (!quote.AffordableWithCoins && gemBalance < quote.GemsNeeded)
                throw new ArgumentException("Gems fall short: the store's purchase sheet is the only confirmation (D-036-AC-02).",
                    nameof(gemBalance));
            var coins = quote.CostCoins - quote.CoinShortfall;
            var cost = quote.AffordableWithCoins
                ? CountText.Format(quote.CostCoins) + " COINS"
                : (coins > 0 ? CountText.Format(coins) + " COINS + " : string.Empty) + CountText.Format(quote.GemsNeeded) + " GEMS";
            // D-045: nextLevel 0 is a supply unit.
            var what = nextLevel > 0 ? "LEVEL " + nextLevel : SupplyUnit(track) + " SUPPLY";
            return new GemConfirmation("UPGRADE " + Track(track) + "?", what + " FOR " + cost, "UPGRADE", "CANCEL");
        }

        /// <summary>D-017-AC-06: what an undone upgrade gave back.</summary>
        public static string Undone(UpgradeReceipt receipt)
        {
            var returned = new System.Collections.Generic.List<string>();
            if (receipt.CoinsPaid > 0)
                returned.Add(CountText.Format(receipt.CoinsPaid) + " COINS");
            if (receipt.GemsSpent > 0)
                returned.Add(CountText.Format(receipt.GemsSpent) + " GEMS");
            // D-045: an undone supply unit goes back; a level drops to the one before it.
            var back = receipt.Supply ? Track(receipt.Track) + " SUPPLY UNDONE" : Track(receipt.Track) + " BACK TO LEVEL " + (receipt.Level - 1);
            return returned.Count == 0 ? back : back + ". " + string.Join(" AND ", returned) + " RETURNED";
        }

        /// <summary>
        /// D-036-AC-01: the upgrade's one price. Coins when the player's coins cover it; the gems it spends when the
        /// player's gems cover the rest (the coins it also takes are named in the confirmation); otherwise the store
        /// price of the gem pack that buys it, or the pack's gems until the store reports a price.
        /// </summary>
        public static string UpgradeButton(UpgradeQuote quote, bool maxed, int gemBalance = int.MaxValue,
            Func<GemPack, string> priceFor = null, bool starterOnSale = false)
        {
            if (maxed)
                return "MAXED";
            if (quote.AffordableWithCoins)
                return "UPGRADE FOR " + CountText.Format(quote.CostCoins) + " COINS";
            if (gemBalance >= quote.GemsNeeded)
                return "UPGRADE FOR " + CountText.Format(quote.GemsNeeded) + " GEMS";
            // D-037-AC-04: names the price of the pack the tap will buy, the Starter Pack while it is on sale.
            var pack = GemCatalog.SmallestPackCovering(quote.GemsNeeded - gemBalance, starterOnSale);
            var price = priceFor?.Invoke(pack);
            return "UPGRADE: " + (string.IsNullOrEmpty(price) ? "BUY " + CountText.Format(pack.Gems) + " GEMS" : price);
        }

        public static string Coins(int coins)
        {
            return "YOU HAVE " + CountText.Format(coins) + " COINS";
        }
    }
}
