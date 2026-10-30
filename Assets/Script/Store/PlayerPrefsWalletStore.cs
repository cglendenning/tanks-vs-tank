using TreadShred.Economy;

public sealed class PlayerPrefsWalletStore : IWalletStore
{
    private const string Key = "TreadShredGemWallet";

    public WalletState Load()
    {
        return PlayerPrefsJson.Load<WalletState>(Key);
    }

    public void Save(WalletState state)
    {
        PlayerPrefsJson.Save(Key, state);
    }
}
