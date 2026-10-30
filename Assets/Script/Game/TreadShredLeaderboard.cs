using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TreadShred.Progression;
using UnityEngine;

public enum LeaderboardOpenResult
{
    // Signed in: show the in-game leaderboard screen.
    Ready,
    SignInShown,
    SignInFromSettings
}

/// <summary>
/// D-032: the platform leaderboard (Game Center on iOS, through the
/// TreadShredGameCenter plugin). The platform owns the player's identity and
/// nickname; the game submits HIGH SCORE and reads back the top entries and
/// the player's rank for its own leaderboard screen. Sign-in is offered when
/// the player taps LEADERBOARD, never forced, and nothing here pauses play.
/// </summary>
public sealed class TreadShredLeaderboard : MonoBehaviour
{
    // Created in App Store Connect by scripts/app-store-connect.py with score limits 0 to ScoreRules.MaxRunScore.
    public const string GameCenterId = "treadshred.high_score";
    private const string ReceiverName = "TreadShredGameCenter";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSignIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterIsSignedIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterOfferSignIn();
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSubmit(long score, string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterLoadRank(string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterLoadTop(string board, int count);
#else
    private static void TreadShredGameCenterSignIn() { }
    private static int TreadShredGameCenterIsSignedIn() => 0;
    private static int TreadShredGameCenterOfferSignIn() => 0;
    private static void TreadShredGameCenterSubmit(long score, string board) { }
    private static void TreadShredGameCenterLoadRank(string board) { }
    private static void TreadShredGameCenterLoadTop(string board, int count) { }
#endif

    private static bool submitting;
    private static int submittingTotal;
    // Game Center's own record of the player's score, and what it accepted this session. Submitting
    // against these (not a local "last submitted" marker) means a stale marker can never block a score.
    private static long? gameCenterScore;
    private static int acceptedThisSession;
    private static TreadShredLeaderboard instance;
    private static readonly List<Action<int>> afterSubmit = new List<Action<int>>();
    private static readonly List<Action<int, long>> awaitingRank = new List<Action<int, long>>();
    private static readonly List<Action<LeaderboardPage>> afterSubmitTop = new List<Action<LeaderboardPage>>();
    private static readonly List<Action<LeaderboardPage>> awaitingTop = new List<Action<LeaderboardPage>>();

    /// <summary>Raised when Game Center finishes signing the player in.</summary>
    public static event Action SignedInNow;

    /// <summary>Whether this platform has a Tread Shred leaderboard (Game Center today).</summary>
    public static bool Available => Application.platform == RuntimePlatform.IPhonePlayer;

    public static bool SignedIn => TreadShredGameCenterIsSignedIn() != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SignInQuietly()
    {
        if (!Available)
            return;
        var host = new GameObject(ReceiverName);
        DontDestroyOnLoad(host);
        instance = host.AddComponent<TreadShredLeaderboard>();
        TreadShredGameCenterSignIn();
    }

    /// <summary>Submits HIGH SCORE when it beats what Game Center holds for the player.</summary>
    public static void SubmitIfRaised()
    {
        if (!Available || !SignedIn || submitting)
            return;
        // Wait until Game Center has reported the player's entry after sign-in.
        var onGameCenter = LeaderboardRules.Baseline(gameCenterScore, acceptedThisSession);
        if (!onGameCenter.HasValue)
            return;
        var highScore = TreadShredScores.HighScore;
        if (!LeaderboardRules.ShouldSubmit(highScore, onGameCenter.Value))
            return;
        submitting = true;
        submittingTotal = highScore;
        TreadShredGameCenterSubmit(highScore, GameCenterId);
    }

    /// <summary>Calls back with the player's all-time rank, after any submission in flight.</summary>
    public static void RequestRank(Action<int> onRank)
    {
        if (!Available || !SignedIn)
            return;
        if (submitting)
            afterSubmit.Add(onRank);
        else
            CatchUpRank(onRank, 0);
    }

    /// <summary>Calls back with the top entries and the player's own, after any submission in flight; null when unavailable.</summary>
    public static void RequestTop(Action<LeaderboardPage> onPage)
    {
        if (!Available || !SignedIn)
        {
            onPage(null);
            return;
        }
        if (submitting)
            afterSubmitTop.Add(onPage);
        else
            CatchUpTop(onPage, 0);
    }

    /// <summary>Ready when signed in; otherwise offers Apple's sign-in, or says to sign in from Settings.</summary>
    public static LeaderboardOpenResult Open()
    {
        if (SignedIn)
            return LeaderboardOpenResult.Ready;
        return TreadShredGameCenterOfferSignIn() != 0
            ? LeaderboardOpenResult.SignInShown
            : LeaderboardOpenResult.SignInFromSettings;
    }

    // Re-reads until the board shows the score accepted this session (Game Center uploads it a few seconds later).
    private static void CatchUpTop(Action<LeaderboardPage> onPage, int reads)
    {
        LoadTop(page =>
        {
            var onBoard = page != null && page.Player.HasValue ? page.Player.Value.Score : 0;
            if (page != null && LeaderboardRules.BoardBehind(onBoard, acceptedThisSession, reads + 1))
                RetryLater(() => CatchUpTop(onPage, reads + 1));
            else
                onPage(page);
        });
    }

    private static void CatchUpRank(Action<int> onRank, int reads)
    {
        LoadRank((rank, score) =>
        {
            if (LeaderboardRules.BoardBehind(score, acceptedThisSession, reads + 1))
                RetryLater(() => CatchUpRank(onRank, reads + 1));
            else if (rank > 0)
                onRank(rank);
        });
    }

    private static void RetryLater(Action read)
    {
        if (instance != null)
            instance.StartCoroutine(After(LeaderboardRules.CatchUpSeconds, read));
    }

    private static System.Collections.IEnumerator After(float seconds, Action read)
    {
        yield return new WaitForSecondsRealtime(seconds);
        read();
    }

    private static void LoadTop(Action<LeaderboardPage> onPage)
    {
        awaitingTop.Add(onPage);
        if (awaitingTop.Count == 1)
            TreadShredGameCenterLoadTop(GameCenterId, LeaderboardPage.TopCount);
    }

    private static void LoadRank(Action<int, long> onRank)
    {
        if (onRank != null)
            awaitingRank.Add(onRank);
        if (awaitingRank.Count <= 1)
            TreadShredGameCenterLoadRank(GameCenterId);
    }

    // Called by the plugin with "1" (signed in), "0" (not) or "offer" (Apple's sign-in screen is ready).
    private void OnSignIn(string state)
    {
        Debug.Log("[Leaderboard] Game Center sign-in: " + state);
        if (state != "1")
            return;
        // Learn the player's score on Game Center first; OnRank then submits if the game's total is higher.
        LoadRank(null);
        SignedInNow?.Invoke();
    }

    private void OnSubmitted(string accepted)
    {
        submitting = false;
        if (accepted == "1")
        {
            acceptedThisSession = Math.Max(acceptedThisSession, submittingTotal);
        }
        else
        {
            // Retried after the next mission or launch, since the saved total did not move.
            Debug.LogWarning("[Leaderboard] score " + submittingTotal + " was not accepted");
        }
        var waiting = afterSubmit.ToArray();
        afterSubmit.Clear();
        foreach (var onRank in waiting)
            CatchUpRank(onRank, 0);
        var waitingTop = afterSubmitTop.ToArray();
        afterSubmitTop.Clear();
        foreach (var onPage in waitingTop)
            CatchUpTop(onPage, 0);
    }

    [Serializable]
    private sealed class RowJson
    {
        public int rank;
        public string name;
        public long score;
        public bool isPlayer;
    }

    [Serializable]
    private sealed class TopJson
    {
        public bool ok;
        public RowJson[] entries;
        public RowJson player;
    }

    private void OnTop(string json)
    {
        var waiting = awaitingTop.ToArray();
        awaitingTop.Clear();
        LeaderboardPage page = null;
        try
        {
            var top = JsonUtility.FromJson<TopJson>(json);
            if (top != null && top.ok)
            {
                var rows = new List<LeaderboardEntry>();
                foreach (var row in top.entries ?? new RowJson[0])
                    rows.Add(new LeaderboardEntry(row.rank, row.name, row.score, row.isPlayer));
                LeaderboardEntry? player = null;
                if (top.player != null && top.player.rank > 0)
                    player = new LeaderboardEntry(top.player.rank, top.player.name, top.player.score, true);
                page = new LeaderboardPage(rows, player);
            }
        }
        catch (ArgumentException error)
        {
            Debug.LogWarning("[Leaderboard] unreadable leaderboard page: " + error.Message);
        }
        foreach (var onPage in waiting)
            onPage(page);
    }

    // Called by the plugin with "rank:score" for the player's own entry ("0:0" when there is none).
    private void OnRank(string reported)
    {
        var waiting = awaitingRank.ToArray();
        awaitingRank.Clear();
        var parts = (reported ?? string.Empty).Split(':');
        int.TryParse(parts[0], out var rank);
        long score = 0;
        if (parts.Length > 1)
            long.TryParse(parts[1], out score);
        gameCenterScore = score;
        Debug.Log("[Leaderboard] Game Center holds rank " + rank + ", score " + score);
        SubmitIfRaised();
        foreach (var onRank in waiting)
            onRank(rank, score);
    }
}
