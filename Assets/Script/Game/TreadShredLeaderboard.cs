using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TreadShred.Progression;
using UnityEngine;

public enum LeaderboardOpenResult
{
    Opened,
    SignInShown,
    SignInFromSettings
}

/// <summary>
/// D-032: the platform leaderboard (Game Center on iOS, through the
/// TreadShredGameCenter plugin). The platform owns the player's identity and
/// nickname; the game only submits TOTAL SCORE and reads back the player's
/// rank. Sign-in is offered when the player taps LEADERBOARD, never forced,
/// and nothing here pauses play.
/// </summary>
public sealed class TreadShredLeaderboard : MonoBehaviour
{
    // Created in App Store Connect by scripts/app-store-connect.py with score limits 0 to LeaderboardRules.MaxTotalScore.
    public const string GameCenterId = "treadshred.total_score";
    private const string SubmittedKey = "TreadShredLeaderboardSubmitted";
    private const string ReceiverName = "TreadShredGameCenter";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSignIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterIsSignedIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterOfferSignIn();
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSubmit(long score, string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterLoadRank(string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterShowBoard(string board);
#else
    private static void TreadShredGameCenterSignIn() { }
    private static int TreadShredGameCenterIsSignedIn() => 0;
    private static int TreadShredGameCenterOfferSignIn() => 0;
    private static void TreadShredGameCenterSubmit(long score, string board) { }
    private static void TreadShredGameCenterLoadRank(string board) { }
    private static void TreadShredGameCenterShowBoard(string board) { }
#endif

    private static bool submitting;
    private static int submittingTotal;
    private static readonly List<Action<int>> afterSubmit = new List<Action<int>>();
    private static readonly List<Action<int>> awaitingRank = new List<Action<int>>();

    /// <summary>Whether this platform has a Tread Shred leaderboard (Game Center today).</summary>
    public static bool Available => Application.platform == RuntimePlatform.IPhonePlayer;

    private static bool SignedIn => TreadShredGameCenterIsSignedIn() != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SignInQuietly()
    {
        if (!Available)
            return;
        var host = new GameObject(ReceiverName);
        DontDestroyOnLoad(host);
        host.AddComponent<TreadShredLeaderboard>();
        TreadShredGameCenterSignIn();
    }

    /// <summary>Submits TOTAL SCORE when it has gone up since the last accepted submission.</summary>
    public static void SubmitIfRaised()
    {
        if (!Available || !SignedIn || submitting)
            return;
        var total = TreadShredScores.TotalScore();
        if (!LeaderboardRules.ShouldSubmit(total, PlayerPrefs.GetInt(SubmittedKey, 0)))
            return;
        submitting = true;
        submittingTotal = total;
        TreadShredGameCenterSubmit(total, GameCenterId);
    }

    /// <summary>Calls back with the player's all-time rank, after any submission in flight.</summary>
    public static void RequestRank(Action<int> onRank)
    {
        if (!Available || !SignedIn)
            return;
        if (submitting)
            afterSubmit.Add(onRank);
        else
            LoadRank(onRank);
    }

    /// <summary>Opens the leaderboard, or offers sign-in when the player is signed out.</summary>
    public static LeaderboardOpenResult Open()
    {
        if (SignedIn)
        {
            TreadShredGameCenterShowBoard(GameCenterId);
            return LeaderboardOpenResult.Opened;
        }
        return TreadShredGameCenterOfferSignIn() != 0
            ? LeaderboardOpenResult.SignInShown
            : LeaderboardOpenResult.SignInFromSettings;
    }

    private static void LoadRank(Action<int> onRank)
    {
        awaitingRank.Add(onRank);
        if (awaitingRank.Count == 1)
            TreadShredGameCenterLoadRank(GameCenterId);
    }

    // Called by the plugin with "1" (signed in), "0" (not) or "offer" (Apple's sign-in screen is ready).
    private void OnSignIn(string state)
    {
        Debug.Log("[Leaderboard] Game Center sign-in: " + state);
        if (state == "1")
            SubmitIfRaised();
    }

    private void OnSubmitted(string accepted)
    {
        submitting = false;
        if (accepted == "1")
        {
            PlayerPrefs.SetInt(SubmittedKey, submittingTotal);
            PlayerPrefs.Save();
        }
        else
        {
            // Retried after the next mission or launch, since the saved total did not move.
            Debug.LogWarning("[Leaderboard] score " + submittingTotal + " was not accepted");
        }
        var waiting = afterSubmit.ToArray();
        afterSubmit.Clear();
        foreach (var onRank in waiting)
            LoadRank(onRank);
    }

    private void OnRank(string reported)
    {
        var waiting = awaitingRank.ToArray();
        awaitingRank.Clear();
        if (!int.TryParse(reported, out var rank) || rank <= 0)
        {
            Debug.Log("[Leaderboard] no rank reported");
            return;
        }
        foreach (var onRank in waiting)
            onRank(rank);
    }
}
