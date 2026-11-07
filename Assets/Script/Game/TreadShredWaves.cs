using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-046-AC-01: on Veteran and Elite, every enemy the map started with (every turret: tanks and one-hit targets alike)
/// comes back at its starting spot once half of
/// the current wave is destroyed (two waves on Veteran, three on Elite). Copies of the map's enemies are taken as the
/// mission starts under an inactive holder, so they never wake; each wave is a fresh set made from them. The mission
/// is won when every wave is destroyed, because each wave adds its enemies to the count the victory waits on. A
/// reinforcement appears only once nothing stands on its spot (an enemy still there, or the tank): two tanks spawned
/// inside each other are thrown apart by physics, and one thrown through a wall leaves the mission unwinnable.
/// </summary>
public sealed class TreadShredWaves : MonoBehaviour
{
    private static TreadShredWaves current;

    private readonly List<GameObject> templates = new List<GameObject>();
    // Reinforcements counted toward the victory but waiting for their starting spot to clear.
    private readonly List<GameObject> waiting = new List<GameObject>();
    private GameObject holder;
    private int wavesLeft;
    private int destroyedThisWave;

    /// <summary>Called as the mission starts; does nothing on Recruit.</summary>
    public static void Begin(ManagerScore score)
    {
        var combat = TierCombat.For(TreadShredProgress.Ensure().SelectedTier);
        if (combat.Waves < 2)
            return;
        current = score.gameObject.AddComponent<TreadShredWaves>();
        current.wavesLeft = combat.Waves - 1;
        current.holder = new GameObject("TreadShredWaveTemplates");
        current.holder.SetActive(false);
        foreach (var enemy in FindObjectsByType<BulletEmnemy>(FindObjectsSortMode.InstanceID))
        {
            // Only enemies the mission counts and the tank can destroy: tanks with health, and targets.
            if (enemy.GetComponent<EmnemyRed>() == null && !enemy.CompareTag("targetMask"))
                continue;
            var template = Instantiate(enemy.gameObject, enemy.transform.position, enemy.transform.rotation, current.holder.transform);
            template.name = enemy.gameObject.name;
            current.templates.Add(template);
        }
        Debug.Log("[Waves] " + current.templates.Count + " enemies, " + combat.Waves + " waves");
        if (current.templates.Count != score.CountEmnemy)
            Debug.LogWarning("[Waves] the map counts " + score.CountEmnemy + " enemies but has " + current.templates.Count);
    }

    /// <summary>The waves still to come.</summary>
    public static int WavesLeft => current != null ? current.wavesLeft : 0;

    /// <summary>An enemy was destroyed; at half of the wave the next one arrives (before the victory count drops).</summary>
    public static void EnemyDestroyed()
    {
        if (current == null)
            return;
        current.destroyedThisWave++;
        if (!TierCombat.NextWaveDue(current.destroyedThisWave, current.templates.Count, current.wavesLeft))
            return;
        current.wavesLeft--;
        current.destroyedThisWave = 0;
        current.waiting.AddRange(current.templates);
        ManagerScore.instance.ReinforcementsArrived(current.templates.Count);
        current.SpawnWhereClear();
        Debug.Log("[Waves] reinforcements: " + current.templates.Count + " enemies, " + current.wavesLeft + " waves left");
    }

    private void Update()
    {
        if (waiting.Count > 0)
            SpawnWhereClear();
    }

    private void SpawnWhereClear()
    {
        for (var i = waiting.Count - 1; i >= 0; i--)
        {
            var template = waiting[i];
            if (Occupied(template))
                continue;
            var enemy = Instantiate(template, template.transform.position, template.transform.rotation);
            enemy.name = template.name;
            waiting.RemoveAt(i);
        }
    }

    // Anything alive on the spot: an enemy (its turret still enabled) or the tank, inside the enemy's own box and a
    // little more.
    private static bool Occupied(GameObject template)
    {
        var center = template.transform.position;
        var half = Vector3.one * 1.5f;
        var box = template.GetComponent<BoxCollider>();
        if (box != null)
        {
            center = template.transform.TransformPoint(box.center);
            half = Vector3.Scale(box.size, template.transform.lossyScale) * 0.6f;
        }
        foreach (var hit in Physics.OverlapBox(center, half, template.transform.rotation))
        {
            if (hit.GetComponentInParent<PlayerMoverment>() != null)
                return true;
            var turret = hit.GetComponentInParent<BulletEmnemy>();
            if (turret != null && turret.enabled)
                return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        if (holder != null)
            Destroy(holder);
        if (current == this)
            current = null;
    }
}
