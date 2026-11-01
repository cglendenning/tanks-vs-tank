using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-035: applies the first-minute rules in the game. Store capture runs and the editor never get them (the screen
/// tests start from fresh saves and expect the start screen); an owner preview build gets them on every launch.
/// </summary>
public static class TreadShredFirstMinute
{
    public const string InstantStartUsedKey = "TreadShredInstantStartUsed";

    // D-035-AC-04: TankBuildAutomation adds this define only to a build made with TREAD_SHRED_FIRST_MINUTE_PREVIEW=1.
#if TREADSHRED_FIRST_MINUTE_PREVIEW
    private static readonly bool Preview = true;
#else
    private static readonly bool Preview = false;
#endif

    // Set by a PlayMode test to play the first minute in the editor.
    private static bool editorPreview;
    private static bool launchChecked;

    /// <summary>Enemy guns hold fire for the mission in play; set as each mission starts (D-035-AC-03).</summary>
    public static bool EnemiesHoldFire { get; private set; }

    private static bool Applies => !TreadShredStoreCaptureMode.IsInstalled && (!Application.isEditor || editorPreview);

    private static bool MissionOneCleared =>
        TreadShredProgress.Ensure().Progression.IsCleared(DifficultyTier.Recruit, FirstMinute.Mission);

    /// <summary>Asked by the start screen once per launch: true when it should open mission 1 instead (D-035-AC-01).</summary>
    public static bool TakeInstantStart()
    {
        if (launchChecked || !Applies)
            return false;
        launchChecked = true;
        if (!Preview && !FirstMinute.InstantStart(MissionOneCleared, PlayerPrefs.GetInt(InstantStartUsedKey, 0) == 1))
            return false;
        PlayerPrefs.SetInt(InstantStartUsedKey, 1);
        PlayerPrefs.Save();
        TreadShredProgress.Ensure().SelectTier(DifficultyTier.Recruit);
        return true;
    }

    /// <summary>Called as a mission starts: whether it plays as the first minute, with enemy fire set to match.</summary>
    public static bool BeginMission(int mission)
    {
        var firstPlay = Applies && (Preview
            ? mission == FirstMinute.Mission
            : FirstMinute.IsFirstPlay(mission, TreadShredProgress.Ensure().SelectedTier, MissionOneCleared));
        EnemiesHoldFire = firstPlay;
        return firstPlay;
    }
}
