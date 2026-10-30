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
    /// mission one extra hit; Overdrive gives the next mission two rounds per
    /// tap. Each is used up by the first mission that starts after it is bought.
    /// </summary>
    public sealed class CombatBoosts
    {
        public const string RedeployKey = "TreadShredArmorCache";
        public const string OverdriveKey = "TreadShredOverdrive";
        public const int RedeployExtraHits = 1;
        public const int OverdriveRoundsPerTap = 2;

        private readonly IBoostFlags flags;

        public CombatBoosts(IBoostFlags flags)
        {
            this.flags = flags ?? throw new ArgumentNullException(nameof(flags));
        }

        public void ArmRedeploy() => flags.Set(RedeployKey);

        public void ArmOverdrive() => flags.Set(OverdriveKey);

        /// <summary>Applies and clears any armed boosts for the mission that is starting.</summary>
        public MissionLoadout ConsumeForMission(int baseArmorHits)
        {
            var hits = baseArmorHits;
            if (flags.IsSet(RedeployKey))
            {
                hits += RedeployExtraHits;
                flags.Clear(RedeployKey);
            }

            var rounds = 1;
            if (flags.IsSet(OverdriveKey))
            {
                rounds = OverdriveRoundsPerTap;
                flags.Clear(OverdriveKey);
            }
            return new MissionLoadout(hits, rounds);
        }
    }
}
