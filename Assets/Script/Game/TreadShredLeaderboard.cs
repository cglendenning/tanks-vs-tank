using System;
using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SocialPlatforms;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

/// <summary>
/// D-032: the platform leaderboard (Game Center on iOS). The platform owns
/// the player's identity and nickname; the game only submits TOTAL SCORE and
/// reads back the player's rank. Sign-in is offered, never required.
/// </summary>
public static class TreadShredLeaderboard
{
    // Created in App Store Connect with score limits 0 to LeaderboardRules.MaxTotalScore.
    public const string GameCenterId = "treadshred.total_score";
    private const string SubmittedKey = "TreadShredLeaderboardSubmitted";

    private static bool submitting;
    private static readonly List<Action<int>> rankRequests = new List<Action<int>>();

    /// <summary>Whether this platform has a Tread Shred leaderboard (Game Center today).</summary>
    public static bool Available => Application.platform == RuntimePlatform.IPhonePlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SignInQuietly()
    {
        if (!Available)
            return;
        // Game Center shows its own sign-in once if needed; play never waits for it.
        Social.localUser.Authenticate(signedIn =>
        {
            Debug.Log("[Leaderboard] Game Center signed in: " + signedIn);
            if (signedIn)
                SubmitIfRaised();
        });
    }

    /// <summary>Submits TOTAL SCORE when it has gone up since the last confirmed submission.</summary>
    public static void SubmitIfRaised()
    {
        if (!Available || !Social.localUser.authenticated || submitting)
            return;
        var total = TreadShredScores.TotalScore();
        if (!LeaderboardRules.ShouldSubmit(total, PlayerPrefs.GetInt(SubmittedKey, 0)))
            return;

        submitting = true;
        Social.ReportScore(total, GameCenterId, accepted =>
        {
            submitting = false;
            if (accepted)
            {
                PlayerPrefs.SetInt(SubmittedKey, total);
                PlayerPrefs.Save();
            }
            else
            {
                // Retried after the next mission or launch, since the saved total did not move.
                Debug.LogWarning("[Leaderboard] score " + total + " was not accepted");
            }
            var waiting = rankRequests.ToArray();
            rankRequests.Clear();
            foreach (var onRank in waiting)
                LoadRank(onRank);
        });
    }

    /// <summary>Calls back with the player's all-time rank, after any submission in flight.</summary>
    public static void RequestRank(Action<int> onRank)
    {
        if (!Available || !Social.localUser.authenticated)
            return;
        if (submitting)
            rankRequests.Add(onRank);
        else
            LoadRank(onRank);
    }

    /// <summary>Opens the platform leaderboard, offering sign-in first when the player is signed out.</summary>
    public static void Open()
    {
        if (!Available)
            return;
        if (Social.localUser.authenticated)
        {
            ShowBoard();
            return;
        }
        Social.localUser.Authenticate(signedIn =>
        {
            if (signedIn)
                ShowBoard();
        });
    }

    private static void ShowBoard()
    {
#if UNITY_IOS
        GameCenterPlatform.ShowLeaderboardUI(GameCenterId, TimeScope.AllTime);
#else
        Social.ShowLeaderboardUI();
#endif
    }

    private static void LoadRank(Action<int> onRank)
    {
        var board = Social.CreateLeaderboard();
        board.id = GameCenterId;
        board.timeScope = TimeScope.AllTime;
        board.userScope = UserScope.Global;
        board.range = new UnityEngine.SocialPlatforms.Range(1, 1);
        board.LoadScores(loaded =>
        {
            var rank = loaded && board.localUserScore != null ? board.localUserScore.rank : 0;
            if (rank > 0)
                onRank(rank);
            else
                Debug.Log("[Leaderboard] no rank reported (loaded=" + loaded + ")");
        });
    }
}
