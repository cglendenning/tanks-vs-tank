using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-020-AC-07: every shell an enemy fires, so a paid revive can clear the ones still in flight and the revived tank is
/// never hit by fire aimed at the destroyed one. The player's own shots are never listed.
/// </summary>
public static class TreadShredEnemyShells
{
    private static readonly List<GameObject> fired = new List<GameObject>();

    /// <summary>The shells still in flight.</summary>
    public static int InFlight
    {
        get
        {
            fired.RemoveAll(shell => shell == null);
            return fired.Count;
        }
    }

    public static void Fired(GameObject shell)
    {
        fired.RemoveAll(existing => existing == null);
        fired.Add(shell);
    }

    /// <summary>Destroys every enemy shell still in flight; returns how many.</summary>
    public static int Clear()
    {
        var cleared = 0;
        foreach (var shell in fired)
        {
            if (shell == null)
                continue;
            Object.Destroy(shell);
            cleared++;
        }
        fired.Clear();
        return cleared;
    }
}
