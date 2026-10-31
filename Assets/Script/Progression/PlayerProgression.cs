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

    /// <summary>What one upgrade took: the level it bought, the coins it removed and the gems it spent.</summary>
    public readonly struct UpgradeReceipt
    {
        public UpgradeReceipt(UpgradeTrack track, int level, int coinsPaid, int gemsSpent, UpgradeQuote quote)
        {
            Track = track;
            Level = level;
            CoinsPaid = coinsPaid;
            GemsSpent = gemsSpent;
            Quote = quote;
        }

        public UpgradeTrack Track { get; }
        public int Level { get; }
        public int CoinsPaid { get; }
        public int GemsSpent { get; }
        public UpgradeQuote Quote { get; }
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

        /// <summary>D-025-AC-03: takes a restored save as a whole, replacing what is held now.</summary>
        public void Replace(ProgressionState restored)
        {
            if (restored == null)
                throw new ArgumentNullException(nameof(restored));
            state.coins = restored.coins;
            state.highestUnlockedTier = restored.highestUnlockedTier;
            state.upgradeLevels = restored.upgradeLevels != null && restored.upgradeLevels.Length == 3
                ? (int[])restored.upgradeLevels.Clone() : new int[3];
            state.clearedMissions = restored.clearedMissions != null && restored.clearedMissions.Length == 3
                ? (long[])restored.clearedMissions.Clone() : new long[3];
            state.claimedMilestones = new List<string>(restored.claimedMilestones ?? new List<string>());
            Commit();
        }

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
        public UpgradeResult TryUpgrade(UpgradeTrack track) => TryUpgrade(track, out _);

        /// <summary>Buys the next level and says exactly what it took, so it can be given back.</summary>
        public UpgradeResult TryUpgrade(UpgradeTrack track, out UpgradeReceipt receipt)
        {
            receipt = default;
            if (IsMaxed(track))
                return UpgradeResult.AlreadyMaxed;

            var quote = Quote(track);
            var coinsBefore = state.coins;
            var gemsSpent = 0;
            if (!quote.AffordableWithCoins)
            {
                if (!wallet.TrySpend(quote.GemsNeeded))
                    return UpgradeResult.NotEnoughGems;
                gemsSpent = quote.GemsNeeded;
                state.coins += quote.GemsNeeded * ProgressionRules.CoinsPerGem;
            }

            state.coins -= quote.CostCoins;
            state.upgradeLevels[(int)track]++;
            receipt = new UpgradeReceipt(track, Level(track), coinsBefore - state.coins, gemsSpent, quote);
            Commit();
            return UpgradeResult.Upgraded;
        }

        /// <summary>
        /// Gives back an upgrade (D-017-AC-06): the track drops to the level before it, and the coins and gems
        /// it took return. Refused when the track is no longer at the level that upgrade bought.
        /// </summary>
        public bool Refund(UpgradeReceipt receipt)
        {
            if (receipt.Level < 1 || Level(receipt.Track) != receipt.Level)
                return false;
            state.upgradeLevels[(int)receipt.Track]--;
            state.coins += receipt.CoinsPaid;
            if (receipt.GemsSpent > 0)
                wallet.Refund(receipt.GemsSpent);
            Commit();
            return true;
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
