using System.Collections.Generic;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-044: erases everything the game saved, here and in the platform cloud, so the game plays like a fresh install
/// by a new player. Attribution bookkeeping (TreadShredAttribution) and the store-capture switch are not game state
/// and stay. The caller returns to the start screen.
/// </summary>
public static class TreadShredGameReset
{
    // The original game's keys: medals, unlocks, kill count, intro and the four settings.
    private static readonly string[] LegacyKeys = { "OpenLv", "CountKill", "intro", "Sound", "Music", "Vibrate", "Camera" };

    /// <summary>D-044-AC-03: every saved game value.</summary>
    public static IEnumerable<string> ErasedKeys()
    {
        yield return PlayerPrefsProgressionStore.Key;
        yield return PlayerPrefsWalletStore.Key;
        yield return TreadShredProgress.TierKey;
        yield return TreadShredProgress.PromotionKey;
        yield return TreadShredCampaign.ClearedKey;
        yield return CombatBoosts.RedeployKey;
        yield return CombatBoosts.OverdriveKey;
        yield return CombatBoosts.OverdriveLostKey;
        yield return TreadShredFirstMinute.InstantStartUsedKey;
        yield return TreadShredFirstMinute.PreviewMissionOneClearedKey;
        yield return TreadShredScores.HighScoreKey;
        yield return TreadShredReviewPrompt.StateKey;
        foreach (var key in LegacyKeys)
            yield return key;
        for (var mission = 1; mission <= Campaign.MissionTotal; mission++)
        {
            yield return "Lv" + mission;
            yield return "Lvs" + mission;
        }
    }

    public static void EraseEverything()
    {
        foreach (var key in ErasedKeys())
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        // D-044-AC-05: the empty save, stamped as a reset, replaces the local and cloud saves.
        TreadShredCloudSave.ResetSave();
        var progress = TreadShredProgress.Ensure();
        progress.Progression.Replace(new ProgressionState());
        progress.SelectTier(DifficultyTier.Recruit);
        var wallet = TreadShredStore.Ensure().Wallet;
        wallet.Replace(new WalletState());
        wallet.GrantStarterIfNeeded();
        TreadShredFirstMinute.ForgetLaunch();
        Debug.Log("[Reset] the game was reset to a fresh install");
    }
}
