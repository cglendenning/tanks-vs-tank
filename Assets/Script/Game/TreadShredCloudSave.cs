using System;
using System.Runtime.InteropServices;
using TreadShred.Economy;
using TreadShred.Progression;
using TreadShred.Save;
using UnityEngine;

/// <summary>
/// D-025: connects CloudSync (all decisions) to PlayerPrefs for the local save,
/// iCloud key-value storage for the cloud (iOS plugin TreadShredCloud.m), and the
/// live wallet and progression when a newer save arrives during play.
/// </summary>
public sealed class TreadShredCloudSave : MonoBehaviour
{
    private const string WrittenAtKey = "TreadShredSaveWrittenAt";
    private const string SyncedKey = "TreadShredSaveSynced";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern string TreadShredCloudLoad();
    [DllImport("__Internal")] private static extern void TreadShredCloudStore(string json);
    [DllImport("__Internal")] private static extern void TreadShredCloudWatch();
#endif

    private static CloudSync sync;

    private sealed class PlayerPrefsSave : ILocalSave
    {
        public SaveSnapshot Load()
        {
            long.TryParse(PlayerPrefs.GetString(WrittenAtKey, "0"), out var writtenAt);
            return new SaveSnapshot
            {
                writtenAtTicks = writtenAt,
                wallet = PlayerPrefsJson.Load<WalletState>(PlayerPrefsWalletStore.Key),
                progression = PlayerPrefsJson.Load<ProgressionState>(PlayerPrefsProgressionStore.Key),
            };
        }

        public void Write(SaveSnapshot snapshot)
        {
            PlayerPrefsJson.Save(PlayerPrefsWalletStore.Key, snapshot.wallet);
            PlayerPrefsJson.Save(PlayerPrefsProgressionStore.Key, snapshot.progression);
            Stamp(snapshot.writtenAtTicks);
        }

        public void Stamp(long writtenAtTicks)
        {
            PlayerPrefs.SetString(WrittenAtKey, writtenAtTicks.ToString());
            PlayerPrefs.Save();
        }

        public bool Synced => PlayerPrefs.GetInt(SyncedKey, 0) == 1;

        public void MarkSynced()
        {
            if (Synced)
                return;
            PlayerPrefs.SetInt(SyncedKey, 1);
            PlayerPrefs.Save();
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    private sealed class ICloudKeyValueSave : ICloudSave
    {
        public SaveSnapshot Read()
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

        public void Write(SaveSnapshot snapshot)
        {
            TreadShredCloudStore(JsonUtility.ToJson(snapshot));
        }
    }
#endif

    // Runs before the store and progression load their saves (both bootstrap after the scene loads).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreAtLaunch()
    {
#if UNITY_IOS && !UNITY_EDITOR
        sync = new CloudSync(new PlayerPrefsSave(), new ICloudKeyValueSave(), () => DateTime.UtcNow.Ticks);
        if (sync.RestoreAtLaunch())
            Debug.Log("[CloudSave] restored the iCloud save at launch");
        var host = new GameObject("TreadShredCloudSave");
        DontDestroyOnLoad(host);
        host.AddComponent<TreadShredCloudSave>();
        TreadShredCloudWatch();
#endif
    }

    /// <summary>Called by the local stores after every save.</summary>
    public static void LocalSaved()
    {
        sync?.LocalSaved();
    }

    // Called by the plugin when iCloud delivers a save written elsewhere.
    private void OnCloudChanged(string unused)
    {
        if (sync != null && sync.CloudChanged(ApplyInPlay))
            Debug.Log("[CloudSave] applied a newer iCloud save");
    }

    private static void ApplyInPlay(SaveSnapshot snapshot)
    {
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Wallet.Replace(snapshot.wallet);
        if (TreadShredProgress.Instance != null)
            TreadShredProgress.Instance.Progression.Replace(snapshot.progression);
    }
}
