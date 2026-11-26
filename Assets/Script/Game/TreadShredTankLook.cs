using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// D-039-AC-03 within P-4: every tank reads on every ground as heavy, lit armor, never as a cartoon. Its own paint is
/// kept and given a steel sheen; a key light that only the tanks receive makes the armor catch the light the ground does
/// not; and each tank sits in a soft contact shadow that separates it from the ground. No outlines and no repainting
/// (owner direction 2026-11-26). Measured on the first mission of every ground by BattlefieldContrastTests.
/// </summary>
public static class TreadShredTankLook
{
    public const string ShadowName = "Tread Shred Contact Shadow";

    public const string KeyLightName = "Tread Shred Tank Key Light";

    // A steel sheen on the tanks' own paint, and a key light from above the camera's shoulder that only they receive.
    private const float Sheen = 0.7f;
    private const float KeyLightIntensity = 0.75f;
    private static readonly Color KeyLightColor = new Color(0.92f, 0.96f, 1f);
    private const float ShadowSpread = 2.2f;
    private const float ShadowStrength = 0.92f;

    private static int tankLayer = -2;
    private static Light keyLight;

    // Unity's built-in primitives on a tank are helpers (aim points and the like), never its model.
    private static readonly HashSet<string> Helpers = new HashSet<string> { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

    /// <summary>The meshes that draw the tank's model, without helpers or its contact shadow.</summary>
    public static List<MeshRenderer> ModelParts(GameObject tank) =>
        tank.GetComponentsInChildren<MeshRenderer>()
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.name != ShadowName)
            .Where(r =>
            {
                var mesh = r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                return mesh != null && !Helpers.Contains(mesh.name);
            })
            .ToList();

    public static void Apply(GameObject tank)
    {
        var parts = ModelParts(tank);
        if (parts.Count == 0 || tank.transform.Find(ShadowName) != null)
            return;
        var layer = TankLayer();
        foreach (var part in parts)
        {
            foreach (var material in part.materials)
            {
                if (material.HasProperty("_Glossiness"))
                    material.SetFloat("_Glossiness", Sheen);
            }
            // Only meshes without colliders move, so aiming, sight and shells (which use layers) are untouched.
            if (layer >= 0 && part.GetComponent<Collider>() == null)
                part.gameObject.layer = layer;
        }
        if (layer >= 0)
            EnsureKeyLight(layer);

        var footprint = parts.Aggregate(parts[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; });
        var shadow = TreadShredDecal.Create(ShadowName, TreadShredDecal.Disc, new Color(0f, 0f, 0f, ShadowStrength));
        if (shadow == null)
            return;
        shadow.transform.SetParent(tank.transform, false);
        shadow.transform.position = new Vector3(footprint.center.x, footprint.min.y + 0.02f, footprint.center.z);
        shadow.transform.rotation = Quaternion.Euler(90f, tank.transform.eulerAngles.y, 0f);
        var parentScale = tank.transform.lossyScale;
        shadow.transform.localScale = new Vector3(footprint.size.x * ShadowSpread / parentScale.x, footprint.size.z * ShadowSpread / parentScale.z, 1f);
    }

    // The highest user layer the project leaves unnamed, so the key light can reach the tanks alone; -1 if none is free.
    private static int TankLayer()
    {
        if (tankLayer != -2)
            return tankLayer;
        tankLayer = -1;
        for (var layer = 31; layer >= 8; layer--)
        {
            if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
            {
                tankLayer = layer;
                break;
            }
        }
        if (tankLayer < 0)
            Debug.LogWarning("[TankLook] no free layer: tanks get no key light.");
        return tankLayer;
    }

    // One per mission: the light goes when its scene unloads.
    private static void EnsureKeyLight(int layer)
    {
        if (keyLight != null)
            return;
        keyLight = new GameObject(KeyLightName).AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = KeyLightIntensity;
        keyLight.color = KeyLightColor;
        keyLight.shadows = LightShadows.None;
        keyLight.cullingMask = 1 << layer;
        keyLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
    }
}

/// <summary>Soft, flat marks drawn on the ground: contact shadows, scorch marks and blast rings (TreadShred/Decal).</summary>
public static class TreadShredDecal
{
    private const string ShaderName = "TreadShred/Decal";
    private static Texture2D disc;
    private static Texture2D ring;

    /// <summary>A soft disc, dark at the centre and fading to nothing at the edge.</summary>
    public static Texture2D Disc => disc != null ? disc : disc = Make(d => Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));

    /// <summary>A thin soft ring, for a blast's dust shockwave.</summary>
    public static Texture2D Ring => ring != null ? ring : ring = Make(d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.2f));

    public static GameObject Create(string name, Texture2D texture, Color color)
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError("[Decal] " + ShaderName + " is missing from the build; no " + name + ".");
            return null;
        }
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(quad.GetComponent<Collider>());
        quad.name = name;
        var renderer = quad.GetComponent<Renderer>();
        renderer.material = new Material(shader) { mainTexture = texture, color = color };
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return quad;
    }

    private static Texture2D Make(System.Func<float, float> alphaAtDistance)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude / (size / 2f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * alphaAtDistance(d)));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
