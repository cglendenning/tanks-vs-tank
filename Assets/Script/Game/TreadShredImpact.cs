using System.Collections;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-027: heavy combat feedback. Camera shake on hits and kills, a kill
/// freeze-frame, a debris burst and a TARGET DESTROYED callout.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class TreadShredImpact : MonoBehaviour
{
    private const float ShakeAmplitude = 0.35f;
    private const int DebrisPieces = 10;

    private static TreadShredImpact instance;

    private float shakeUntil;
    private float shakeDuration;
    private Vector3 appliedOffset;
    private Transform shakenCamera;
    private RectTransform calloutOverlay;
    private Text callout;
    private Text clearCallout;
    private Coroutine calloutRoutine;
    private Coroutine clearRoutine;
    private bool frozen;

    private static TreadShredImpact Ensure()
    {
        if (instance == null)
            instance = new GameObject("Tread Shred Impact").AddComponent<TreadShredImpact>();
        return instance;
    }

    public static void HitTaken()
    {
        Ensure().Shake(ImpactRules.ShakeOnHitTaken);
        // Kill paths already pulse through Rung; hits taken did not (D-027-AC-03).
        if (ManagerScore.instance != null)
            ManagerScore.instance.Rung(HapticPulse.Hit);
    }

    public static void Kill(Vector3 position)
    {
        var impact = Ensure();
        impact.Shake(ImpactRules.ShakeOnKill);
        impact.SpawnDebris(position);
        // The kill's points appear with it (D-032-AC-01).
        // A shell still in flight can land after the mission ends; the result screen stays clean (D-013-AC-04).
        if (!missionOver)
            impact.ShowCallout(ScoreRules.KillCallout(ScoreRules.KillValue(TreadShredProgress.Ensure().SelectedTier)));
        if (!impact.frozen)
            impact.StartCoroutine(impact.FreezeFrame());
    }

    private void Shake(float duration)
    {
        shakeDuration = duration;
        shakeUntil = Time.unscaledTime + duration;
    }

    // Runs after camera follow; removes last frame's offset before applying a
    // new one so a fixed camera never drifts.
    private void LateUpdate()
    {
        var camera = Camera.main != null ? Camera.main.transform : null;
        if (shakenCamera != null && shakenCamera == camera)
            shakenCamera.position -= appliedOffset;
        appliedOffset = Vector3.zero;
        shakenCamera = camera;
        if (camera == null || Time.unscaledTime >= shakeUntil)
            return;

        var strength = ShakeAmplitude * (shakeUntil - Time.unscaledTime) / shakeDuration;
        appliedOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * strength;
        camera.position += appliedOffset;
    }

    private IEnumerator FreezeFrame()
    {
        frozen = true;
        var previous = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(ImpactRules.KillFreezeFrame);
        // A revive offer or pause may have taken over time while frozen.
        if (Time.timeScale == 0f)
            Time.timeScale = previous;
        frozen = false;
    }

    private void SpawnDebris(Vector3 position)
    {
        for (var i = 0; i < DebrisPieces; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.position = position + Random.insideUnitSphere * 0.4f + Vector3.up * 0.5f;
            piece.transform.localScale = Vector3.one * Random.Range(0.12f, 0.3f);
            piece.transform.rotation = Random.rotation;
            var renderer = piece.GetComponent<Renderer>();
            renderer.material.color = Color.Lerp(new Color(0.08f, 0.08f, 0.08f), new Color(1f, 0.36f, 0.08f), Random.value * 0.4f);
            var body = piece.AddComponent<Rigidbody>();
            body.AddExplosionForce(9f, position, 3f, 1.5f, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);
            Destroy(piece, 2.5f);
        }
    }

    private static bool missionOver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetEachMission()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => missionOver = false;
    }

    /// <summary>The result screen is opening: clear any callout so nothing sits over its text (D-013-AC-04).</summary>
    public static void MissionOver()
    {
        missionOver = true;
        var impact = Ensure();
        foreach (var line in new[] { impact.callout, impact.clearCallout })
        {
            if (line != null)
                line.color = new Color(line.color.r, line.color.g, line.color.b, 0f);
        }
        if (impact.calloutRoutine != null)
            impact.StopCoroutine(impact.calloutRoutine);
        if (impact.clearRoutine != null)
            impact.StopCoroutine(impact.clearRoutine);
    }

    /// <summary>D-032-AC-01: the clear points appear the moment the last enemy falls.</summary>
    public static void MissionClear(int points)
    {
        var impact = Ensure();
        impact.clearCallout = impact.CalloutLine(impact.clearCallout, 190f, 64);
        if (impact.clearRoutine != null)
            impact.StopCoroutine(impact.clearRoutine);
        impact.clearRoutine = impact.StartCoroutine(impact.FadeCallout(impact.clearCallout, ScoreRules.ClearCallout(points)));
    }

    private void ShowCallout(string message)
    {
        callout = CalloutLine(callout, 300f, 84);
        if (calloutRoutine != null)
            StopCoroutine(calloutRoutine);
        calloutRoutine = StartCoroutine(FadeCallout(callout, message));
    }

    private Text CalloutLine(Text existing, float y, int size)
    {
        if (existing != null)
            return existing;
        if (calloutOverlay == null)
            calloutOverlay = TreadShredUi.CreateOverlay("Impact Callout", 90, receivesTaps: false, parent: transform);
        var line = TreadShredUi.CreateText(string.Empty, calloutOverlay, new Vector2(0f, y), new Vector2(1500f, 110f), size,
            TreadShredUi.FindFont(GameObject.Find("Canvas")));
        var outline = line.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, -4f);
        return line;
    }

    private IEnumerator FadeCallout(Text line, string message)
    {
        line.text = message;
        var elapsed = 0f;
        while (elapsed < ImpactRules.KillCallout)
        {
            var color = TreadShredUi.TitleColor;
            color.a = 1f - elapsed / ImpactRules.KillCallout;
            line.color = color;
            line.transform.localScale = Vector3.one * (1.15f - 0.15f * Mathf.Min(1f, elapsed * 6f));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        line.color = Color.clear;
    }
}
