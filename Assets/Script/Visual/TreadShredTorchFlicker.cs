using UnityEngine;

/// <summary>
/// Gives the wall sconces a small, deterministic flame motion without adding
/// another particle system to every arena. The sprite remains readable at
/// phone resolution while scale, lean, glow and brightness move independently.
/// </summary>
public sealed class TreadShredTorchFlicker : MonoBehaviour
{
    [SerializeField] private float intensity = 0.12f;
    [SerializeField] private float speed = 2.1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Color baseColor;
    private float phase;
    private Light torchLight;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        baseColor = spriteRenderer == null ? Color.white : spriteRenderer.color;
        torchLight = GetComponent<Light>();
        phase = Mathf.Abs(Mathf.Sin(transform.position.x * 0.73f + transform.position.z * 1.17f)) * 6.28f;
    }

    private void Update()
    {
        var time = Time.time * speed + phase;
        var swell = 1f + Mathf.Sin(time * 1.17f) * intensity + Mathf.Sin(time * 2.43f) * intensity * 0.34f;
        var lean = Mathf.Sin(time * 1.61f) * intensity * 8f;
        transform.localScale = new Vector3(baseScale.x * (1f + (swell - 1f) * 0.68f), baseScale.y * swell, baseScale.z);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, lean);

        if (spriteRenderer != null)
        {
            var glow = 1f + Mathf.Sin(time * 1.83f) * 0.08f;
            var color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a * glow);
            spriteRenderer.color = color;
        }

        if (torchLight != null)
            torchLight.intensity = 0.72f + Mathf.Sin(time * 1.83f) * 0.12f + Mathf.Sin(time * 3.11f) * 0.06f;
    }
}
