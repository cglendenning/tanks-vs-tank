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
        // D-043: special weapons the player has played with (WeaponRental.Bit). rentals is the 1.9.5 save's missions left
        // per weapon, turned into supply when loaded.
        public int usedWeapons;
        public int[] rentals = new int[4];

        // D-045: supply units held per SupplyTrack, the missions won toward the next use, and where the rotation looks next.
        public int[] supply = new int[SupplyRules.TrackCount];
        public int supplyWins;
        public int supplyNext;
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
        public UpgradeReceipt(UpgradeTrack track, int level, int coinsPaid, int gemsSpent, UpgradeQuote quote, bool supply = false)
        {
            Track = track;
            Level = level;
            CoinsPaid = coinsPaid;
            GemsSpent = gemsSpent;
            Quote = quote;
            Supply = supply;
        }

        /// <summary>D-045: the purchase was a supply unit, not a level.</summary>
        public bool Supply { get; }

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
            AdoptRentals(state);
        }

        // D-045: a 1.9.5 save's running rentals (missions left) become weapon supply, one unit per 5 missions begun.
        private static void AdoptRentals(ProgressionState held)
        {
            if (held.supply == null || held.supply.Length != SupplyRules.TrackCount)
                held.supply = new int[SupplyRules.TrackCount];
            if (held.rentals == null)
                return;
            for (var weapon = Weapon.ArcShot; weapon <= Weapon.Seeker && (int)weapon < held.rentals.Length; weapon++)
            {
                var left = held.rentals[(int)weapon];
                if (left > 0)
                    held.supply[(int)SupplyRules.ForWeapon(weapon)] += (left + SupplyRules.MissionsPerUse - 1) / SupplyRules.MissionsPerUse;
            }
            held.rentals = new int[4];
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
            // D-043: the weapons used and a running rental come back with a restored save.
            state.usedWeapons = restored.usedWeapons;
            state.rentals = restored.rentals != null && restored.rentals.Length == 4 ? (int[])restored.rentals.Clone() : new int[4];
            // D-045: and the supply with its clock.
            state.supply = restored.supply != null && restored.supply.Length == SupplyRules.TrackCount
                ? (int[])restored.supply.Clone() : new int[SupplyRules.TrackCount];
            state.supplyWins = restored.supplyWins;
            state.supplyNext = restored.supplyNext;
            AdoptRentals(state);
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

        /// <summary>
        /// D-009-AC-03: the next level that costs fewest coins, the one most likely to be a single tap; ties go to the
        /// first track (armor, firepower, speed). D-045: every track can be bought (a coin level, or a unit of supply
        /// at its coins' worth), so there is always one.
        /// </summary>
        public UpgradeTrack? CheapestNextUpgrade()
        {
            UpgradeTrack? cheapest = null;
            foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
            {
                if (cheapest == null || Quote(track).CostCoins < Quote(cheapest.Value).CostCoins)
                    cheapest = track;
            }
            return cheapest;
        }

        /// <summary>D-045: armor, and a track at the top of its coin ladder, sells supply units instead of levels.</summary>
        public bool BuysSupply(UpgradeTrack track) => ProgressionRules.GemOnly(track) || IsMaxed(track);

        /// <summary>The level the next purchase reaches; 0 when it buys a unit of supply (D-045).</summary>
        public int NextLevel(UpgradeTrack track) => BuysSupply(track) ? 0 : Level(track) + 1;

        public UpgradeQuote Quote(UpgradeTrack track)
        {
            if (BuysSupply(track))
            {
                // D-045: a unit is paid in gems only, at the coin rate the quote is kept in.
                var unit = SupplyRules.UnitGems * ProgressionRules.CoinsPerGem;
                return new UpgradeQuote(unit, unit);
            }
            var cost = (Level(track) + 1) * ProgressionRules.CostStepCoins;
            // D-017-AC-03: coins never buy armor, so its whole price is paid in gems at the coin rate.
            if (ProgressionRules.GemOnly(track))
                return new UpgradeQuote(cost, cost);
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
            var quote = Quote(track);
            if (BuysSupply(track))
            {
                if (!wallet.TrySpend(quote.GemsNeeded))
                    return UpgradeResult.NotEnoughGems;
                SupplyUnits[(int)SupplyOf(track)]++;
                receipt = new UpgradeReceipt(track, Level(track), 0, quote.GemsNeeded, quote, supply: true);
                Commit();
                return UpgradeResult.Upgraded;
            }

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
            if (receipt.Supply)
            {
                var units = SupplyUnits;
                if (units[(int)SupplyOf(receipt.Track)] <= 0)
                    return false;
                units[(int)SupplyOf(receipt.Track)]--;
                wallet.Refund(receipt.GemsSpent);
                Commit();
                return true;
            }
            if (receipt.Level < 1 || Level(receipt.Track) != receipt.Level)
                return false;
            state.upgradeLevels[(int)receipt.Track]--;
            state.coins += receipt.CoinsPaid;
            if (receipt.GemsSpent > 0)
                wallet.Refund(receipt.GemsSpent);
            Commit();
            return true;
        }

        /// <summary>D-043-AC-01: a mission whose tank carries a special weapon makes that weapon rentable.</summary>
        public void WeaponUsed(Weapon weapon)
        {
            if (weapon == Weapon.Cannon || (state.usedWeapons & WeaponRental.Bit(weapon)) != 0)
                return;
            state.usedWeapons |= WeaponRental.Bit(weapon);
            Commit();
        }

        public bool HasUsed(Weapon weapon) => weapon != Weapon.Cannon && (state.usedWeapons & WeaponRental.Bit(weapon)) != 0;

        /// <summary>D-043-AC-06, D-045: the missions won a weapon's supply lasts; 0 when it holds none.</summary>
        public int RentalLeft(Weapon weapon) => weapon == Weapon.Cannon ? 0 : SupplyMissionsLeft(SupplyRules.ForWeapon(weapon));

        // D-045: each weapon's missions left, by Weapon, for the drawer and the starting weapon.
        private int[] Rentals
        {
            get
            {
                var left = new int[4];
                for (var weapon = Weapon.ArcShot; weapon <= Weapon.Seeker; weapon++)
                    left[(int)weapon] = RentalLeft(weapon);
                return left;
            }
        }

        private int[] SupplyUnits => state.supply != null && state.supply.Length == SupplyRules.TrackCount
            ? state.supply : (state.supply = new int[SupplyRules.TrackCount]);

        private static SupplyTrack SupplyOf(UpgradeTrack track) => (SupplyTrack)(int)track;

        /// <summary>D-045: the units a track holds.</summary>
        public int Supply(SupplyTrack track) => SupplyUnits[(int)track];

        /// <summary>D-045: the missions won until a track's last unit is used up, if nothing more is bought.</summary>
        public int SupplyMissionsLeft(SupplyTrack track) =>
            SupplyRules.MissionsLeft(SupplyUnits, state.supplyWins, state.supplyNext, track);

        /// <summary>D-045: the track the next use takes from (null when none is held) and the missions won until then.</summary>
        public SupplyTrack? NextSupplyUse => SupplyRules.NextTrack(SupplyUnits, state.supplyNext);

        public int MissionsToNextSupplyUse => SupplyRules.MissionsToNextUse(SupplyUnits, state.supplyWins, state.supplyNext);

        /// <summary>D-045: one unit paid for (the caller has taken the gems).</summary>
        public void AddSupply(SupplyTrack track)
        {
            SupplyUnits[(int)track]++;
            Commit();
        }

        /// <summary>D-045: a mission won counts toward the next use; every fifth uses one unit up, in rotation.</summary>
        public void SupplyMissionWon()
        {
            var wins = state.supplyWins;
            var next = state.supplyNext;
            SupplyRules.MissionWon(SupplyUnits, ref wins, ref next);
            if (wins == state.supplyWins && next == state.supplyNext)
                return;
            state.supplyWins = wins;
            state.supplyNext = next;
            Commit();
        }

        /// <summary>D-038-AC-02, D-043-AC-02: the weapon drawer for a mission whose own tank carries <paramref name="native"/>.</summary>
        public IReadOnlyList<DrawerEntry> Drawer(Weapon native) => WeaponRental.Drawer(native, state.usedWeapons, Rentals);

        /// <summary>D-038-AC-02, D-043-AC-05: the weapon a mission starts on.</summary>
        public Weapon StartingWeapon(Weapon native) => WeaponRental.Starting(native, Rentals);

        /// <summary>D-043-AC-04, D-045: a paid rental is one unit of the weapon's supply.</summary>
        public void StartRental(Weapon weapon)
        {
            if (!HasUsed(weapon))
                throw new InvalidOperationException(weapon + " has not been used, so it cannot be rented.");
            AddSupply(SupplyRules.ForWeapon(weapon));
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

                // D-019-AC-01: missions unlock strictly in order, so clearing a tier's last mission opens the next tier
                // (a save from before the campaign may lack some earlier clears).
                if (tier < DifficultyTier.Elite && (int)tier == state.highestUnlockedTier
                    && mission == ProgressionRules.MissionCount)
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
