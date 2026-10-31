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
