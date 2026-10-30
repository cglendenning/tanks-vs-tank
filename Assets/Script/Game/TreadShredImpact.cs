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
    private Text callout;
    private Coroutine calloutRoutine;
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
    }

    public static void Kill(Vector3 position)
    {
        var impact = Ensure();
        impact.Shake(ImpactRules.ShakeOnKill);
        impact.SpawnDebris(position);
        impact.ShowCallout();
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

    private void ShowCallout()
    {
        if (callout == null)
        {
            var canvasObject = new GameObject("Impact Callout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            callout = TreadShredUi.CreateText("TARGET DESTROYED", (RectTransform)canvasObject.transform,
                new Vector2(0f, 300f), new Vector2(1200f, 110f), 84, TreadShredUi.FindFont(GameObject.Find("Canvas")));
            var outline = callout.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, -4f);
        }
        if (calloutRoutine != null)
            StopCoroutine(calloutRoutine);
        calloutRoutine = StartCoroutine(FadeCallout());
    }

    private IEnumerator FadeCallout()
    {
        var elapsed = 0f;
        while (elapsed < ImpactRules.KillCallout)
        {
            var color = TreadShredUi.TitleColor;
            color.a = 1f - elapsed / ImpactRules.KillCallout;
            callout.color = color;
            callout.transform.localScale = Vector3.one * (1.15f - 0.15f * Mathf.Min(1f, elapsed * 6f));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        callout.color = Color.clear;
    }
}
