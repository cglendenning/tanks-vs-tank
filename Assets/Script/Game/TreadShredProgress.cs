using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the player's coins, upgrades and selected difficulty tier for the
/// running game, and exposes the combat multipliers they imply.
/// </summary>
public sealed class TreadShredProgress : MonoBehaviour
{
    internal const string TierKey = "TreadShredTier";
    // D-019-AC-05: the tier a player has just been promoted into, until the promotion has played.
    internal const string PromotionKey = "TreadShredPromotionPending";

    public static TreadShredProgress Instance { get; private set; }

    public PlayerProgression Progression { get; private set; }
    public DifficultyTier SelectedTier { get; private set; }

    // The reward from the mission that just ended, shown on its result screen.
    public MissionReward? LastReward { get; private set; }

    /// <summary>D-043-AC-05: the weapon the mission's own tank carries; only cannon-only missions use up a rental.</summary>
    public Weapon NativeWeapon { get; set; }

    public TierModifiers TierModifiers => ProgressionRules.Modifiers(SelectedTier);
    public float DamageMultiplier => UpgradeEffects.DamageMultiplier(Progression.Level(UpgradeTrack.Firepower));
    public float SpeedMultiplier => UpgradeEffects.SpeedMultiplier(Progression.Level(UpgradeTrack.Speed));
    public int ArmorHits => UpgradeEffects.ArmorHits(Progression.Level(UpgradeTrack.Armor));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    public static TreadShredProgress Ensure()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject("Tread Shred Progress");
        return host.AddComponent<TreadShredProgress>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Progression = new PlayerProgression(new PlayerPrefsProgressionStore(), TreadShredStore.Ensure().Wallet);
        var saved = (DifficultyTier)Mathf.Clamp(PlayerPrefs.GetInt(TierKey, 0), 0, (int)DifficultyTier.Elite);
        SelectedTier = Progression.IsUnlocked(saved) ? saved : DifficultyTier.Recruit;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SelectTier(DifficultyTier tier)
    {
        if (!Progression.IsUnlocked(tier))
            return;

        SelectedTier = tier;
        PlayerPrefs.SetInt(TierKey, (int)tier);
        PlayerPrefs.Save();
    }

    // Mission number from the active scene ("Lv12" is mission 12), 0 outside a mission.
    public static int CurrentMission()
    {
        var name = SceneManager.GetActiveScene().name;
        return name.StartsWith("Lv") && int.TryParse(name.Substring(2), out var mission) ? mission : 0;
    }

    /// <summary>D-019: the campaign number (1-120) of the mission being played, 0 outside a mission.</summary>
    public int CurrentNumber()
    {
        var map = CurrentMission();
        return map < 1 || map > ProgressionRules.MissionCount ? 0 : Campaign.Number(SelectedTier, map);
    }

    /// <summary>D-019-AC-05: the tier whose promotion is still to play, if any.</summary>
    public DifficultyTier? PendingPromotion =>
        PlayerPrefs.HasKey(PromotionKey) ? (DifficultyTier)PlayerPrefs.GetInt(PromotionKey) : (DifficultyTier?)null;

    public void PromotionPlayed()
    {
        PlayerPrefs.DeleteKey(PromotionKey);
        PlayerPrefs.Save();
    }

    public MissionReward? CompleteCurrentMission(bool won)
    {
        var mission = CurrentMission();
        if (mission < 1 || mission > ProgressionRules.MissionCount)
        {
            LastReward = null;
            return null;
        }

        LastReward = Progression.CompleteMission(SelectedTier, mission, won);
        if (won)
            Progression.RentalMissionWon(NativeWeapon);
        if (LastReward.Value.UnlockedTier is DifficultyTier promoted)
        {
            PlayerPrefs.SetInt(PromotionKey, (int)promoted);
            PlayerPrefs.Save();
        }
        return LastReward;
    }
}
