using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-028: swaps legacy decorative labels in the loaded scene for command-voice copy.
/// </summary>
public static class TreadShredCopyFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    public static void Apply()
    {
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            text.text = CommandCopy.Replace(text.text);
    }
}
