using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-050: applies the ask-free start in the game. A mission that starts during it keeps it through its revive, its
/// result screen and NEXT MISSION, so mission 5's own victory screen asks nothing; the next mission plays as for any
/// player. Store capture runs and the editor never get it (the screen tests expect today's offers) unless a PlayMode
/// test turns it on, as with the first minute (D-035-AC-04).
/// </summary>
public static class TreadShredAskFreeStart
{
    // Set by a PlayMode test to play the ask-free start in the editor.
    private static bool editorPreview;
    private static bool missionAskFree;

    private static bool Applies => !TreadShredStoreCaptureMode.IsInstalled && (!Application.isEditor || editorPreview);

    /// <summary>
    /// D-050-AC-01: the player has not yet cleared mission 5 on Recruit. While progress is still loading (the store
    /// starts inside progress's own start-up) it counts as running, so nothing opens early; the next scene load decides.
    /// </summary>
    public static bool InProgress
    {
        get
        {
            if (!Applies)
                return false;
            var progression = TreadShredProgress.Ensure().Progression;
            return progression == null || AskFreeStart.InProgress(progression);
        }
    }

    /// <summary>D-050-AC-02: the mission in play started during the ask-free start.</summary>
    public static bool MissionAskFree => Applies && missionAskFree;

    /// <summary>Called as each mission starts.</summary>
    public static void BeginMission() => missionAskFree = InProgress;
}
