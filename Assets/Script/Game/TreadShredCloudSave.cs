using System;
using System.Runtime.InteropServices;
using TreadShred.Economy;
using TreadShred.Progression;
using TreadShred.Save;
using UnityEngine;

/// <summary>
/// D-025: backs up the wallet and progression to the platform cloud (iCloud
/// key-value storage on iOS) after every change, restores the newer save at
/// launch before the starter grant is considered, and applies a newer save
/// that arrives later as a whole. Without a cloud, play continues on local data.
/// </summary>
public sealed class TreadShredCloudSave : MonoBehaviour
{
    private const string WrittenAtKey = "TreadShredSaveWrittenAt";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern string TreadShredCloudLoad();
    [DllImport("__Internal")] private static extern void TreadShredCloudStore(string json);
    [DllImport("__Internal")] private static extern void TreadShredCloudWatch();
#else
    private static string TreadShredCloudLoad() => string.Empty;
    private static void TreadShredCloudStore(string json) { }
    private static void TreadShredCloudWatch() { }
#endif

    private static bool applying;

    private static bool Available => Application.platform == RuntimePlatform.IPhonePlayer;

    // Runs before the store and progression load their saves (both bootstrap after the scene loads).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreAtLaunch()
    {
        if (!Available)
            return;
        var cloud = Read();
        if (SaveRules.Choose(Local(), cloud) == SaveSource.Cloud)
        {
            WriteLocal(cloud);
            Debug.Log("[CloudSave] restored the iCloud save at launch");
        }
        var host = new GameObject("TreadShredCloudSave");
        DontDestroyOnLoad(host);
        host.AddComponent<TreadShredCloudSave>();
        TreadShredCloudWatch();
    }

    /// <summary>Called by the local stores after every save: stamps it and pushes it to the cloud.</summary>
    public static void LocalSaved()
    {
        if (applying)
            return;
        PlayerPrefs.SetString(WrittenAtKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
        // A save with nothing earned yet (a fresh install's starter gems) never overwrites the cloud.
        var snapshot = Local();
        if (Available && SaveRules.HasProgress(snapshot))
            TreadShredCloudStore(JsonUtility.ToJson(snapshot));
    }

    // Called by the plugin when iCloud delivers a save written elsewhere (another device, or the
    // first sync after a reinstall): a newer save, or any save over a fresh install, replaces local.
    private void OnCloudChanged(string unused)
    {
        var cloud = Read();
        if (SaveRules.Choose(Local(), cloud) != SaveSource.Cloud)
            return;
        Debug.Log("[CloudSave] applying a newer iCloud save");
        applying = true;
        try
        {
            if (TreadShredStore.Instance != null)
                TreadShredStore.Instance.Wallet.Replace(cloud.wallet);
            if (TreadShredProgress.Instance != null)
                TreadShredProgress.Instance.Progression.Replace(cloud.progression);
            WriteLocal(cloud);
        }
        finally
        {
            applying = false;
        }
    }

    private static SaveSnapshot Local()
    {
        long.TryParse(PlayerPrefs.GetString(WrittenAtKey, "0"), out var writtenAt);
        return new SaveSnapshot
        {
            writtenAtTicks = writtenAt,
            wallet = PlayerPrefsJson.Load<WalletState>(PlayerPrefsWalletStore.Key),
            progression = PlayerPrefsJson.Load<ProgressionState>(PlayerPrefsProgressionStore.Key),
        };
    }

    private static SaveSnapshot Read()
    {
        var json = TreadShredCloudLoad();
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonUtility.FromJson<SaveSnapshot>(json);
        }
        catch (ArgumentException error)
        {
            Debug.LogWarning("[CloudSave] unreadable iCloud save ignored: " + error.Message);
            return null;
        }
    }

    // Writes a whole snapshot to the local stores without pushing it back.
    private static void WriteLocal(SaveSnapshot snapshot)
    {
        PlayerPrefsJson.Save(PlayerPrefsWalletStore.Key, snapshot.wallet);
        PlayerPrefsJson.Save(PlayerPrefsProgressionStore.Key, snapshot.progression);
        PlayerPrefs.SetString(WrittenAtKey, snapshot.writtenAtTicks.ToString());
        PlayerPrefs.Save();
    }
}
