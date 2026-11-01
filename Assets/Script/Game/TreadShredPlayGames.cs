using UnityEngine;

/// <summary>
/// D-025, D-032: the Android Play Games plugin (Plugins/Android/TreadShredPlayGames.java), started once by the
/// leaderboard after its message receiver exists. Its answers arrive as the same Unity messages the iOS Game
/// Center and iCloud plugins send.
/// </summary>
public static class TreadShredPlayGames
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass plugin;

    private static AndroidJavaClass Plugin
    {
        get
        {
            if (plugin != null)
                return plugin;
            plugin = new AndroidJavaClass("com.tgts.treadshred.TreadShredPlayGames");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                plugin.CallStatic("start", player.GetStatic<AndroidJavaObject>("currentActivity"));
            return plugin;
        }
    }

    public static void Start() => _ = Plugin;
    public static bool SignedIn => Plugin.CallStatic<bool>("isSignedIn");
    public static bool OfferSignIn() => Plugin.CallStatic<bool>("offerSignIn");
    public static void Submit(long score, string board) => Plugin.CallStatic("submit", score, board);
    public static void LoadRank(string board) => Plugin.CallStatic("loadRank", board);
    public static void LoadTop(string board, int count) => Plugin.CallStatic("loadTop", board, count);
    public static void WriteSave(string json) => Plugin.CallStatic("writeSave", json);
    public static bool OpenProfileSettings() => Plugin.CallStatic<bool>("openProfileSettings");
#endif
}
