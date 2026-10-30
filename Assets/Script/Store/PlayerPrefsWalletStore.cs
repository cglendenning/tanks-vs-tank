using System;
using TreadShred.Economy;
using UnityEngine;

public sealed class PlayerPrefsWalletStore : IWalletStore
{
    private const string Key = "TreadShredGemWallet";

    public WalletState Load()
    {
        var json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new WalletState();

        try
        {
            return JsonUtility.FromJson<WalletState>(json) ?? new WalletState();
        }
        catch (ArgumentException error)
        {
            Debug.LogError("[Store] Saved gem wallet is unreadable; starting from an empty wallet. " + error.Message);
            return new WalletState();
        }
    }

    public void Save(WalletState state)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }
}
