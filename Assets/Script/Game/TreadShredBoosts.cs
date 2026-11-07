using TreadShred.Economy;
using UnityEngine;

/// <summary>Combat boosts (D-009) stored in PlayerPrefs.</summary>
public static class TreadShredBoosts
{
    private sealed class PlayerPrefsBoostFlags : IBoostFlags
    {
        public bool IsSet(string key) => PlayerPrefs.GetInt(key, 0) > 0;

        public void Set(string key)
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        public void Clear(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    // D-045: Overdrive is on while the player holds Overdrive supply.
    public static CombatBoosts Current { get; } = new CombatBoosts(new PlayerPrefsBoostFlags(),
        () => TreadShredProgress.Ensure().Progression.Supply(TreadShred.Progression.SupplyTrack.Overdrive) > 0);
}
