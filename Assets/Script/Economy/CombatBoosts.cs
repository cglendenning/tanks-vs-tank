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
    /// D-009-AC-03: result-screen boosts. Armored Redeploy gives the reloaded
    /// mission one extra hit, used up by the first mission that starts after it
    /// is bought. Overdrive gives two rounds per tap from the next mission until
    /// the player's tank is lost; an Armored Redeploy bought then keeps it.
    /// </summary>
    public sealed class CombatBoosts
    {
        public const string RedeployKey = "TreadShredArmorCache";
        public const string OverdriveKey = "TreadShredOverdrive";
        // Set while the lost tank's Overdrive can still come back with an Armored Redeploy.
        public const string OverdriveLostKey = "TreadShredOverdriveLost";
        public const int RedeployExtraHits = 1;
        public const int OverdriveRoundsPerTap = 2;

        private readonly IBoostFlags flags;

        public CombatBoosts(IBoostFlags flags)
        {
            this.flags = flags ?? throw new ArgumentNullException(nameof(flags));
        }

        public bool OverdriveActive => flags.IsSet(OverdriveKey);

        public void ArmRedeploy()
        {
            flags.Set(RedeployKey);
            // The redeploy continues the run the lost tank ended, Overdrive included.
            if (!flags.IsSet(OverdriveLostKey))
                return;
            flags.Clear(OverdriveLostKey);
            flags.Set(OverdriveKey);
        }

        public void ArmOverdrive() => flags.Set(OverdriveKey);

        /// <summary>The player's tank was lost: Overdrive ends unless an Armored Redeploy is bought next.</summary>
        public void TankLost()
        {
            if (!flags.IsSet(OverdriveKey))
                return;
            flags.Clear(OverdriveKey);
            flags.Set(OverdriveLostKey);
        }

        /// <summary>
        /// Applies the armed boosts for the mission that is starting: Armored Redeploy is used up, Overdrive
        /// stays until the tank is lost, and a lost Overdrive not resumed by a redeploy is gone for good.
        /// </summary>
        public MissionLoadout ConsumeForMission(int baseArmorHits)
        {
            var hits = baseArmorHits;
            if (flags.IsSet(RedeployKey))
            {
                hits += RedeployExtraHits;
                flags.Clear(RedeployKey);
            }

            flags.Clear(OverdriveLostKey);
            return new MissionLoadout(hits, flags.IsSet(OverdriveKey) ? OverdriveRoundsPerTap : 1);
        }
    }
}
