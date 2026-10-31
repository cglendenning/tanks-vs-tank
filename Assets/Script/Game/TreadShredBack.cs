using System;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-014-AC-03: Android's system Back gesture and button close the panel or dialog opened last,
/// the same as its own BACK, CANCEL or NO THANKS button. Panels register when they open.
/// </summary>
public sealed class TreadShredBack : MonoBehaviour
{
    private static readonly BackStack stack = new BackStack();

    public static void Opened(object panel, Action close) => stack.Opened(panel, close);

    public static void Closed(object panel) => stack.Closed(panel);

    /// <summary>What the system Back does; true when it closed something.</summary>
    public static bool Press() => stack.Back();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Listen()
    {
        var host = new GameObject("TreadShredBack");
        DontDestroyOnLoad(host);
        host.AddComponent<TreadShredBack>();
    }

    private void Update()
    {
        // Android delivers the system Back gesture and button as Escape.
        if (Input.GetKeyDown(KeyCode.Escape))
            Press();
    }
}
