using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-046-AC-06: on Veteran and Elite a one-hit target takes the tier's enemy health like every other enemy (2 hits on
/// Veteran, 3 on Elite before firepower upgrades, which cut it as they do for every enemy), kept in tenths of a hit as
/// EmnemyRed keeps it. Recruit's targets still go down to one shell.
/// </summary>
public sealed class TreadShredTargetHealth : MonoBehaviour
{
    private int health;

    /// <summary>True when the target survives this hit (only the shell bursts); false when the hit destroys it.</summary>
    public static bool Survives(GameObject target)
    {
        var progress = TreadShredProgress.Ensure();
        var hits = target.GetComponent<TreadShredTargetHealth>();
        if (hits == null)
        {
            hits = target.AddComponent<TreadShredTargetHealth>();
            hits.health = TierCombat.TargetHealth(progress.TierModifiers.EnemyHealth);
        }
        hits.health -= TierCombat.ShellDamage(progress.DamageMultiplier);
        return hits.health > 0;
    }
}
