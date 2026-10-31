using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-032: the platform leaderboard (Game Center on iOS, through the
/// TreadShredGameCenter plugin). The platform owns the player's identity and
/// nickname; LeaderboardSync decides when HIGH SCORE is submitted and how the
/// board is read back; this class only connects it to the plugin. Sign-in is
/// offered when the player taps LEADERBOARD, never forced, and nothing here
/// pauses play.
/// </summary>
public sealed class TreadShredLeaderboard : MonoBehaviour
{
    // Created in App Store Connect by scripts/app-store-connect.py with score limits 0 to ScoreRules.MaxRunScore.
    public const string GameCenterId = "treadshred.best_run";
    // Created in the Play Console by scripts/play-console.py games-leaderboard, with the same limits.
    public const string PlayGamesId = "PENDING_PLAY_GAMES_LEADERBOARD";
#if UNITY_ANDROID
    private const string BoardId = PlayGamesId;
#else
    private const string BoardId = GameCenterId;
#endif
    private const string ReceiverName = "TreadShredGameCenter";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSignIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterIsSignedIn();
    [DllImport("__Internal")] private static extern int TreadShredGameCenterOfferSignIn();
    [DllImport("__Internal")] private static extern void TreadShredGameCenterSubmit(long score, string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterLoadRank(string board);
    [DllImport("__Internal")] private static extern void TreadShredGameCenterLoadTop(string board, int count);
#elif UNITY_ANDROID && !UNITY_EDITOR
    // Google Play Games through Plugins/Android/TreadShredPlayGames.java, which answers with the same messages.
    private static void TreadShredGameCenterSignIn() => TreadShredPlayGames.Start();
    private static int TreadShredGameCenterIsSignedIn() => TreadShredPlayGames.SignedIn ? 1 : 0;
    private static int TreadShredGameCenterOfferSignIn() => TreadShredPlayGames.OfferSignIn() ? 1 : 0;
    private static void TreadShredGameCenterSubmit(long score, string board) => TreadShredPlayGames.Submit(score, board);
    private static void TreadShredGameCenterLoadRank(string board) => TreadShredPlayGames.LoadRank(board);
    private static void TreadShredGameCenterLoadTop(string board, int count) => TreadShredPlayGames.LoadTop(board, count);
#else
    private static void TreadShredGameCenterSignIn() { }
    private static int TreadShredGameCenterIsSignedIn() => 0;
    private static int TreadShredGameCenterOfferSignIn() => 0;
    private static void TreadShredGameCenterSubmit(long score, string board) { }
    private static void TreadShredGameCenterLoadRank(string board) { }
    private static void TreadShredGameCenterLoadTop(string board, int count) { }
#endif

    private static readonly GameCenterPlugin plugin = new GameCenterPlugin();
    private static readonly LeaderboardSync sync = new LeaderboardSync(plugin, () => TreadShredScores.HighScore, After);
    private static TreadShredLeaderboard instance;

    /// <summary>Raised when Game Center finishes signing the player in.</summary>
    public static event Action SignedInNow;

    /// <summary>Whether this platform has a Tread Shred leaderboard: Game Center on iOS, Play Games on Android.</summary>
    public static bool Available =>
        Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android;

    public static bool SignedIn => Available && plugin.SignedIn;

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
        if (Available)
            sync.SubmitIfRaised();
    }

    /// <summary>Calls back with the player's all-time rank, after any submission in flight.</summary>
    public static void RequestRank(Action<int> onRank)
    {
        if (Available)
            sync.RequestRank(onRank);
    }

    /// <summary>Calls back with the top entries and the player's own, after any submission in flight; null when unavailable.</summary>
    public static void RequestTop(Action<LeaderboardPage> onPage)
    {
        if (Available)
            sync.RequestTop(onPage);
        else
            onPage(null);
    }

    /// <summary>Ready when signed in; otherwise offers Apple's sign-in, or says to sign in from Settings.</summary>
    public static LeaderboardOpenResult Open()
    {
        return sync.Open();
    }

    private static void After(float seconds, Action run)
    {
        if (instance != null)
            instance.StartCoroutine(Wait(seconds, run));
    }

    private static System.Collections.IEnumerator Wait(float seconds, Action run)
    {
        yield return new WaitForSecondsRealtime(seconds);
        run();
    }

    // Game Center through the plugin. Answers arrive as the UnitySendMessage callbacks below; reads
    // of the same kind share one native request while it is in flight.
    private sealed class GameCenterPlugin : IGameCenter
    {
        public readonly List<Action<int, long>> AwaitingEntry = new List<Action<int, long>>();
        public readonly List<Action<LeaderboardPage>> AwaitingTop = new List<Action<LeaderboardPage>>();
        public Action<bool> AwaitingSubmit;
        public long Submitting;

        public bool SignedIn => TreadShredGameCenterIsSignedIn() != 0;

        public bool OfferSignIn() => TreadShredGameCenterOfferSignIn() != 0;

        public void Submit(long score, Action<bool> accepted)
        {
            AwaitingSubmit = accepted;
            Submitting = score;
            TreadShredGameCenterSubmit(score, BoardId);
        }

        public void LoadPlayerEntry(Action<int, long> entry)
        {
            AwaitingEntry.Add(entry);
            if (AwaitingEntry.Count == 1)
                TreadShredGameCenterLoadRank(BoardId);
        }

        public void LoadTop(int count, Action<LeaderboardPage> page)
        {
            AwaitingTop.Add(page);
            if (AwaitingTop.Count == 1)
                TreadShredGameCenterLoadTop(BoardId, count);
        }
    }

    // Called by the plugin with "1" (signed in), "0" (not) or "offer" (Apple's sign-in screen is ready).
    private void OnSignIn(string state)
    {
        Debug.Log("[Leaderboard] Game Center sign-in: " + state);
        if (state != "1")
            return;
        sync.SignedIn();
        SignedInNow?.Invoke();
    }

    private void OnSubmitted(string accepted)
    {
        if (accepted != "1")
            Debug.LogWarning("[Leaderboard] score " + plugin.Submitting + " was not accepted");
        var answer = plugin.AwaitingSubmit;
        plugin.AwaitingSubmit = null;
        answer?.Invoke(accepted == "1");
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
        var waiting = plugin.AwaitingTop.ToArray();
        plugin.AwaitingTop.Clear();
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
        var waiting = plugin.AwaitingEntry.ToArray();
        plugin.AwaitingEntry.Clear();
        var parts = (reported ?? string.Empty).Split(':');
        int.TryParse(parts[0], out var rank);
        long score = 0;
        if (parts.Length > 1)
            long.TryParse(parts[1], out score);
        Debug.Log("[Leaderboard] Game Center holds rank " + rank + ", score " + score);
        foreach (var onEntry in waiting)
            onEntry(rank, score);
    }
}
