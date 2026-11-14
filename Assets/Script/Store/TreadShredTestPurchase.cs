using System;
using TreadShred.Analytics;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>D-021-AC-07: whether a delivered order came from a store's test account (TestPurchases decides).</summary>
public static class TreadShredTestPurchase
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string TreadShredReceiptFileName();
#endif

    public static bool IsTest(string unityReceipt)
    {
#if UNITY_EDITOR
        // The editor's fake store never charges anyone.
        return true;
#elif UNITY_IOS
        return TestPurchases.AppStore(TreadShredReceiptFileName());
#elif UNITY_ANDROID
        return TestPurchases.GooglePlay(InstallerPackage(), PurchaseReceipt.GooglePlay(unityReceipt));
#else
        return false;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // The package that installed this build; unknown counts as the Play Store, so a real purchase is never hidden.
    private static string InstallerPackage()
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packages = activity.Call<AndroidJavaObject>("getPackageManager"))
                return packages.Call<string>("getInstallerPackageName", activity.Call<string>("getPackageName"));
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Store] installer unknown, purchase counted as real: " + error.Message);
            return TestPurchases.PlayStoreInstaller;
        }
    }
#endif
}
