using System;
using System.Collections.Generic;
using TreadShred.Economy;

namespace TreadShred.Progression
{
    // Public fields so Unity's JsonUtility can persist it.
    [Serializable]
    public sealed class ProgressionState
    {
        public int coins;
        public int[] upgradeLevels = new int[3];
        public int highestUnlockedTier;
        // One bit per mission (bit 0 = mission 1) for each tier.
        public long[] clearedMissions = new long[3];
        public List<string> claimedMilestones = new List<string>();
    }

    public interface IProgressionStore
    {
        ProgressionState Load();
        void Save(ProgressionState state);
    }

    public enum UpgradeResult
    {
        Upgraded,
        AlreadyMaxed,
        NotEnoughGems
    }

    public readonly struct UpgradeQuote
    {
        public UpgradeQuote(int costCoins, int coinShortfall)
        {
            CostCoins = costCoins;
            CoinShortfall = coinShortfall;
        }

        public int CostCoins { get; }
        public int CoinShortfall { get; }
        public int GemsNeeded => (CoinShortfall + ProgressionRules.CoinsPerGem - 1) / ProgressionRules.CoinsPerGem;
        public bool AffordableWithCoins => CoinShortfall == 0;
    }

    public readonly struct MissionReward
    {
        public MissionReward(int coins, int gems, DifficultyTier? unlockedTier)
        {
            Coins = coins;
            Gems = gems;
            UnlockedTier = unlockedTier;
        }

        public int Coins { get; }
        public int Gems { get; }
        public DifficultyTier? UnlockedTier { get; }
    }

    /// <summary>
    /// Coins, permanent upgrades, difficulty tiers and milestone gems.
    /// </summary>
    public sealed class PlayerProgression
    {
        private readonly IProgressionStore store;
        private readonly GemWallet wallet;
        private readonly ProgressionState state;

        public PlayerProgression(IProgressionStore store, GemWallet wallet)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            state = store.Load() ?? new ProgressionState();
            if (state.upgradeLevels == null || state.upgradeLevels.Length != 3)
                state.upgradeLevels = new int[3];
            if (state.clearedMissions == null || state.clearedMissions.Length != 3)
                state.clearedMissions = new long[3];
            if (state.claimedMilestones == null)
                state.claimedMilestones = new List<string>();
        }

        public event Action Changed;

        public int Coins => state.coins;

        public int Level(UpgradeTrack track)
        {
            return state.upgradeLevels[(int)track];
        }

        public bool IsMaxed(UpgradeTrack track)
        {
            return Level(track) >= ProgressionRules.LevelsPerTrack;
        }

        public UpgradeQuote Quote(UpgradeTrack track)
        {
            if (IsMaxed(track))
                return new UpgradeQuote(0, 0);
            var cost = (Level(track) + 1) * ProgressionRules.CostStepCoins;
            return new UpgradeQuote(cost, Math.Max(0, cost - state.coins));
        }

        /// <summary>
        /// Buys the next level with coins, covering any coin shortfall with gems.
        /// </summary>
        public UpgradeResult TryUpgrade(UpgradeTrack track)
        {
            if (IsMaxed(track))
                return UpgradeResult.AlreadyMaxed;

            var quote = Quote(track);
            if (!quote.AffordableWithCoins)
            {
                if (!wallet.TrySpend(quote.GemsNeeded))
                    return UpgradeResult.NotEnoughGems;
                state.coins += quote.GemsNeeded * ProgressionRules.CoinsPerGem;
            }

            state.coins -= quote.CostCoins;
            state.upgradeLevels[(int)track]++;
            Commit();
            return UpgradeResult.Upgraded;
        }

        public bool IsUnlocked(DifficultyTier tier)
        {
            return (int)tier <= state.highestUnlockedTier;
        }

        public bool IsCleared(DifficultyTier tier, int mission)
        {
            return (state.clearedMissions[(int)tier] & MissionBit(mission)) != 0;
        }

        public int ClearedCount(DifficultyTier tier)
        {
            var count = 0;
            for (var mission = 1; mission <= ProgressionRules.MissionCount; mission++)
            {
                if (IsCleared(tier, mission))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Pays coins for a finished mission, records the clear, unlocks the
        /// next tier when every mission is cleared, and pays milestone gems once.
        /// </summary>
        public MissionReward CompleteMission(DifficultyTier tier, int mission, bool won)
        {
            if (mission < 1 || mission > ProgressionRules.MissionCount)
                throw new ArgumentOutOfRangeException(nameof(mission), mission, "Unknown mission.");
            if (!IsUnlocked(tier))
                throw new InvalidOperationException(tier + " is not unlocked.");

            var coins = (won ? ProgressionRules.CoinsPerWin : ProgressionRules.CoinsPerLoss) * ProgressionRules.Modifiers(tier).Coins;
            state.coins += coins;

            var gems = 0;
            DifficultyTier? unlocked = null;
            if (won)
            {
                state.clearedMissions[(int)tier] |= MissionBit(mission);
                if (tier == DifficultyTier.Recruit && Array.IndexOf(ProgressionRules.MilestoneMissions, mission) >= 0
                    && Claim("mission-" + mission))
                    gems += ProgressionRules.MilestoneMissionGems;

                if (tier < DifficultyTier.Elite && (int)tier == state.highestUnlockedTier
                    && ClearedCount(tier) == ProgressionRules.MissionCount)
                {
                    var next = tier + 1;
                    state.highestUnlockedTier = (int)next;
                    unlocked = next;
                    if (Claim("tier-" + next))
                        gems += ProgressionRules.TierUnlockGems(next);
                }
            }

            Commit();
            if (gems > 0)
                wallet.CreditReward(gems);
            return new MissionReward(coins, gems, unlocked);
        }

        private bool Claim(string milestone)
        {
            if (state.claimedMilestones.Contains(milestone))
                return false;
            state.claimedMilestones.Add(milestone);
            return true;
        }

        private static long MissionBit(int mission)
        {
            return 1L << (mission - 1);
        }

        private void Commit()
        {
            store.Save(state);
            Changed?.Invoke();
        }
    }
}
