using System;

namespace TreadShred.Economy
{
    /// <summary>Persistent on/off flags (PlayerPrefs in the game).</summary>
    public interface IBoostFlags
    {
        bool IsSet(string key);
        void Set(string key);
        void Clear(string key);
    }

    /// <summary>What the next mission starts with.</summary>
    public readonly struct MissionLoadout
    {
        public MissionLoadout(int armorHits, int roundsPerTap)
        {
            ArmorHits = armorHits;
            RoundsPerTap = roundsPerTap;
        }

        public int ArmorHits { get; }
        public int RoundsPerTap { get; }
    }

    /// <summary>
    /// D-009-AC-03: Armored Redeploy gives the reloaded mission one extra hit, used up by the first mission that starts
    /// after it is bought. D-045: Overdrive gives two rounds per tap in every mission while the player holds Overdrive
    /// supply; losing the tank no longer ends it.
    /// </summary>
    public sealed class CombatBoosts
    {
        public const string RedeployKey = "TreadShredArmorCache";
        // A 1.9.5 save's Overdrive switch and its lost-tank marker, turned into one unit of supply when found.
        public const string OverdriveKey = "TreadShredOverdrive";
        public const string OverdriveLostKey = "TreadShredOverdriveLost";
        public const int RedeployExtraHits = 1;
        public const int OverdriveRoundsPerTap = 2;

        private readonly IBoostFlags flags;
        private readonly Func<bool> overdriveSupplied;

        public CombatBoosts(IBoostFlags flags, Func<bool> overdriveSupplied)
        {
            this.flags = flags ?? throw new ArgumentNullException(nameof(flags));
            this.overdriveSupplied = overdriveSupplied ?? throw new ArgumentNullException(nameof(overdriveSupplied));
        }

        public bool OverdriveActive => overdriveSupplied();

        public void ArmRedeploy() => flags.Set(RedeployKey);

        /// <summary>D-045: true once when a 1.9.5 save had Overdrive on (or lost to a redeploy); the flags are cleared.</summary>
        public bool TakeLegacyOverdrive()
        {
            var had = flags.IsSet(OverdriveKey) || flags.IsSet(OverdriveLostKey);
            flags.Clear(OverdriveKey);
            flags.Clear(OverdriveLostKey);
            return had;
        }

        /// <summary>Applies the boosts for the mission that is starting: Armored Redeploy is used up; Overdrive follows supply.</summary>
        public MissionLoadout ConsumeForMission(int baseArmorHits)
        {
            var hits = baseArmorHits;
            if (flags.IsSet(RedeployKey))
            {
                hits += RedeployExtraHits;
                flags.Clear(RedeployKey);
            }
            return new MissionLoadout(hits, OverdriveActive ? OverdriveRoundsPerTap : 1);
        }
    }
}
