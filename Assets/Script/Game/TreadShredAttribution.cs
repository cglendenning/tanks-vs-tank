using System;
using TreadShred.Analytics;
using TreadShred.Economy;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// D-023: install attribution for AppLovin campaigns. On Android, Tenjin's SDK (Plugins/Android/TreadShredTenjin.java)
/// reports installs and verified purchases, and Tenjin forwards them to AppLovin. On iOS, purchases set SKAdNetwork
/// conversion values (Plugins/iOS/TreadShredAdAttribution.m); Apple reports the install and its value to AppLovin and
/// sends a copy to Tenjin. Tenjin's iOS SDK is not used: its server is a declared tracking domain, which iOS blocks
/// without the tracking prompt D-005 forbids. Every failure is logged and none reaches gameplay.
/// </summary>
public sealed class TreadShredAttribution : MonoBehaviour
{
    private const string ReceiverName = "TreadShredAttribution";

    private bool paused;
    private bool settled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var receiver = new GameObject(ReceiverName);
        DontDestroyOnLoad(receiver);
        receiver.AddComponent<TreadShredAttribution>();
    }

    private void Start()
    {
        Run("start", Platform.Start);
        StartCoroutine(AskAgain());
    }

    // D-021-AC-05: the launch's own question is usually asked before Tenjin has credited the install.
    private System.Collections.IEnumerator AskAgain()
    {
        foreach (var delay in Attribution.AskAgainAfterSeconds)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (settled)
                yield break;
            Run("attribution", Platform.RequestAttribution);
        }
    }

    // Tenjin counts a session each time the player comes back to the game.
    private void OnApplicationPause(bool pausing)
    {
        if (pausing)
        {
            paused = true;
            return;
        }
        if (!paused)
            return;
        paused = false;
        Run("resume", Platform.Resume);
        if (!settled)
            Run("attribution", Platform.RequestAttribution);
    }

    // Sent by TreadShredTenjin.java as "ad_network<TAB>campaign_name<TAB>campaign_id".
    private void OnAttribution(string answer)
    {
        var parts = (answer ?? string.Empty).Split('\t');
        var acquisition = Attribution.FromTenjin(Part(parts, 0), Part(parts, 1), Part(parts, 2));
        if (acquisition == null)
        {
            Debug.Log("[Attribution] Tenjin has not attributed this install yet");
            return;
        }
        Debug.Log("[Attribution] " + acquisition.Value.Source + " / " + acquisition.Value.Campaign);
        settled = Attribution.Settled(acquisition);
        TreadShredAnalytics.SetUserProperty(GameEvents.AcquisitionSource, acquisition.Value.Source);
        TreadShredAnalytics.SetUserProperty(GameEvents.AcquisitionCampaign, acquisition.Value.Campaign);
    }

    /// <summary>Called by the store once a credited gem pack order is confirmed, so it is reported exactly once.</summary>
    public static void PurchaseConfirmed(string productId, double price, string currency, string receipt)
    {
        Run("purchase " + productId, () => Platform.Purchase(productId, price, currency, receipt));
    }

    private static string Part(string[] parts, int index) => index < parts.Length ? parts[index] : string.Empty;

    private static void Run(string step, Action action)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Attribution] " + step + " failed: " + error.Message);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static class Platform
    {
        private static AndroidJavaClass plugin;

        private static AndroidJavaClass Plugin =>
            plugin ?? (plugin = new AndroidJavaClass("com.tgts.treadshred.TreadShredTenjin"));

        public static void Start()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                Plugin.CallStatic("start", player.GetStatic<AndroidJavaObject>("currentActivity"));
            RequestAttribution();
        }

        public static void Resume() => Plugin.CallStatic("connect");

        public static void RequestAttribution() => Plugin.CallStatic("requestAttribution");

        public static void Purchase(string productId, double price, string currency, string receipt)
        {
            var purchase = PurchaseReceipt.GooglePlay(receipt);
            if (purchase == null)
            {
                Debug.LogWarning("[Attribution] " + productId + " has no Google Play purchase data; Tenjin cannot verify it");
                return;
            }
            Plugin.CallStatic("transaction", productId, currency, price, purchase.Value.PurchaseData, purchase.Value.Signature);
        }
    }
#elif UNITY_IOS && !UNITY_EDITOR
    private static class Platform
    {
        private const string PurchasesKey = "attribution_purchases_usd_cents";
        private const string RegisteredKey = "attribution_skan_registered";

        [DllImport("__Internal")]
        private static extern void TreadShredUpdateConversionValue(int fineValue, string coarseValue);

        // The first conversion value on a fresh install starts Apple's attribution for it.
        public static void Start()
        {
            if (PlayerPrefs.GetInt(RegisteredKey, 0) == 1)
                return;
            TreadShredUpdateConversionValue(0, SkanConversion.Low);
            PlayerPrefs.SetInt(RegisteredKey, 1);
            PlayerPrefs.Save();
        }

        public static void Resume()
        {
        }

        public static void RequestAttribution()
        {
        }

        // Conversion values use each pack's reference US price, so every storefront's purchases decode alike.
        public static void Purchase(string productId, double price, string currency, string receipt)
        {
            var pack = GemCatalog.FindPack(productId);
            if (pack == null)
            {
                Debug.LogWarning("[Attribution] unknown product " + productId + "; conversion value unchanged");
                return;
            }
            var total = PlayerPrefs.GetInt(PurchasesKey, 0) + pack.UsdCents;
            PlayerPrefs.SetInt(PurchasesKey, total);
            PlayerPrefs.Save();
            TreadShredUpdateConversionValue(SkanConversion.FineValue(total), SkanConversion.CoarseValue(total));
        }
    }
#else
    private static class Platform
    {
        public static void Start()
        {
        }

        public static void Resume()
        {
        }

        public static void RequestAttribution()
        {
        }

        public static void Purchase(string productId, double price, string currency, string receipt)
        {
        }
    }
#endif
}
