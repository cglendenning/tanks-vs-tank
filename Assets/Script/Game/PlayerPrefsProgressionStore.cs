using TreadShred.Progression;

public sealed class PlayerPrefsProgressionStore : IProgressionStore
{
    private const string Key = "TreadShredProgression";

    public ProgressionState Load()
    {
        return PlayerPrefsJson.Load<ProgressionState>(Key);
    }

    public void Save(ProgressionState state)
    {
        PlayerPrefsJson.Save(Key, state);
    }
}
