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

        /// <summary>D-018: the upgrade screen's note on where each currency comes from and what it buys.</summary>
        public const string CurrencyNoteTitle = "COINS AND GEMS";

        public static IReadOnlyList<string> CurrencyNote() => new[]
        {
            "COINS: EARNED IN EVERY MISSION, WIN OR LOSE. THEY BUY PERMANENT UPGRADES.",
            "GEMS: BOUGHT IN THE STORE OR EARNED AT MILESTONES. THEY BUY BOOSTS AND REVIVES.",
            "SHORT ON COINS? GEMS COVER THE GAP AT 1 GEM FOR " + ProgressionRules.CoinsPerGem + " COINS."
        };

        /// <summary>
        /// D-017-AC-05: what an upgrade will spend, asked before anything is spent. When gems fall short the
        /// confirm button says it buys a pack, and the store's own sheet follows.
        /// </summary>
        public static GemConfirmation UpgradeConfirmation(UpgradeTrack track, int nextLevel, UpgradeQuote quote, int gemBalance)
        {
            var coins = quote.CostCoins - quote.CoinShortfall;
            var cost = quote.AffordableWithCoins
                ? CountText.Format(quote.CostCoins) + " COINS"
                : (coins > 0 ? CountText.Format(coins) + " COINS + " : string.Empty) + CountText.Format(quote.GemsNeeded) + " GEMS";
            var detail = "LEVEL " + nextLevel + " FOR " + cost;
            var confirm = "UPGRADE";
            if (!quote.AffordableWithCoins && gemBalance < quote.GemsNeeded)
            {
                detail += ". YOU HAVE " + CountText.Format(gemBalance) + " GEMS";
                confirm = "BUY " + CountText.Format(GemCatalog.SmallestPackCovering(quote.GemsNeeded - gemBalance).Gems) + " GEMS";
            }
            return new GemConfirmation("UPGRADE " + Track(track) + "?", detail, confirm, "CANCEL");
        }

        /// <summary>D-017-AC-06: what an undone upgrade gave back.</summary>
        public static string Undone(UpgradeReceipt receipt)
        {
            var returned = new System.Collections.Generic.List<string>();
            if (receipt.CoinsPaid > 0)
                returned.Add(CountText.Format(receipt.CoinsPaid) + " COINS");
            if (receipt.GemsSpent > 0)
                returned.Add(CountText.Format(receipt.GemsSpent) + " GEMS");
            var back = Track(receipt.Track) + " BACK TO LEVEL " + (receipt.Level - 1);
            return returned.Count == 0 ? back : back + ". " + string.Join(" AND ", returned) + " RETURNED";
        }

        public static string UpgradeButton(UpgradeQuote quote, bool maxed)
        {
            if (maxed)
                return "MAXED";
            if (quote.AffordableWithCoins)
                return "UPGRADE FOR " + CountText.Format(quote.CostCoins) + " COINS";
            var coins = quote.CostCoins - quote.CoinShortfall;
            return coins > 0
                ? "UPGRADE FOR " + CountText.Format(coins) + " COINS + " + CountText.Format(quote.GemsNeeded) + " GEMS"
                : "UPGRADE FOR " + CountText.Format(quote.GemsNeeded) + " GEMS";
        }

        public static string Coins(int coins)
        {
            return "YOU HAVE " + CountText.Format(coins) + " COINS";
        }
    }
}
