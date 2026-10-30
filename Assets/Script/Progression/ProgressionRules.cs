namespace TreadShred.Progression
{
    public enum UpgradeTrack
    {
        Armor,
        Firepower,
        Speed
    }

    public enum DifficultyTier
    {
        Recruit,
        Veteran,
        Elite
    }

    public readonly struct TierModifiers
    {
        public TierModifiers(float enemyHealth, float enemySpeed, int coins)
        {
            EnemyHealth = enemyHealth;
            EnemySpeed = enemySpeed;
            Coins = coins;
        }

        public float EnemyHealth { get; }
        public float EnemySpeed { get; }
        public int Coins { get; }
    }

    // Values mirror contracts/rules.json in the specification repository
    // (R-UPGRADE-TRACKS, R-COINS, R-DIFFICULTY-TIERS, R-GEM-MILESTONES, R-REVIVE).
    public static class ProgressionRules
    {
        public const int MissionCount = 40;
        public const int LevelsPerTrack = 10;
        public const int CostStepCoins = 200;

        public const int BaseArmorHits = 3;
        public const int ExtraHitEveryLevels = 2;
        public const float DamageBonusPerLevel = 0.10f;
        public const float SpeedBonusPerLevel = 0.05f;

        public const int CoinsPerWin = 100;
        public const int CoinsPerLoss = 20;
        public const int CoinsPerGem = 20;

        public static readonly int[] MilestoneMissions = { 10, 20, 30, 40 };
        public const int MilestoneMissionGems = 20;

        public static readonly int[] ReviveCostsGems = { 30, 60 };
        public const int MaxRevivesPerMission = 2;
        public const float ReviveOfferSeconds = 8f;

        public static TierModifiers Modifiers(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Veteran:
                    return new TierModifiers(1.5f, 1.2f, 2);
                case DifficultyTier.Elite:
                    return new TierModifiers(2.25f, 1.4f, 3);
                default:
                    return new TierModifiers(1f, 1f, 1);
            }
        }

        public static int TierUnlockGems(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Veteran:
                    return 30;
                case DifficultyTier.Elite:
                    return 40;
                default:
                    return 0;
            }
        }
    }

    public static class UpgradeEffects
    {
        public static int ArmorHits(int level)
        {
            return ProgressionRules.BaseArmorHits + level / ProgressionRules.ExtraHitEveryLevels;
        }

        public static float DamageMultiplier(int level)
        {
            return 1f + ProgressionRules.DamageBonusPerLevel * level;
        }

        public static float SpeedMultiplier(int level)
        {
            return 1f + ProgressionRules.SpeedBonusPerLevel * level;
        }
    }

    public static class ReviveRules
    {
        public static bool CanOffer(int revivesUsed)
        {
            return revivesUsed < ProgressionRules.MaxRevivesPerMission;
        }

        public static int CostGems(int revivesUsed)
        {
            return ProgressionRules.ReviveCostsGems[revivesUsed];
        }
    }
}
