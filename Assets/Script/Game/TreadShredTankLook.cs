using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// D-039-AC-03: every tank reads on every ground. Each tank gets a solid outline, lime for the player and warm orange
/// for enemies, so it stands out from the ground by at least 3:1 whatever the ground's colour, and friend and foe
/// tell apart at a glance. Measured on the first mission of every ground by BattlefieldContrastTests.
/// </summary>
public static class TreadShredTankLook
{
    public const string OutlineName = "Tread Shred Outline";
    public static readonly Color PlayerOutline = new Color(0.66f, 1f, 0.24f);
    public static readonly Color EnemyOutline = new Color(1f, 0.62f, 0.24f);

    // The outline's width as a share of the tank's footprint: about five pixels on a phone at the arena camera.
    private const float WidthShare = 0.07f;
    private const string ShaderName = "TreadShred/Outline";

    // The tank's body draws after its outline (Geometry+1), so the body covers all but the rim.
    private const int BodyQueue = 2002;

    private static readonly Dictionary<Mesh, Mesh> SmoothedMeshes = new Dictionary<Mesh, Mesh>();

    // Unity's built-in primitives on a tank are helpers (aim points, colliders' stand-ins), never its model.
    private static readonly HashSet<string> Helpers = new HashSet<string> { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

    /// <summary>The meshes that draw the tank's model, without helpers or outlines.</summary>
    public static List<MeshRenderer> ModelParts(GameObject tank) =>
        tank.GetComponentsInChildren<MeshRenderer>()
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.name != OutlineName)
            .Where(r =>
            {
                var mesh = r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
                return mesh != null && !Helpers.Contains(mesh.name);
            })
            .ToList();

    public static void Outline(GameObject tank, bool player)
    {
        var parts = ModelParts(tank);
        if (parts.Count == 0 || parts.Any(p => p.transform.Find(OutlineName) != null))
            return;
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError("[TankLook] " + ShaderName + " is missing from the build; tanks keep no outline.");
            return;
        }
        var footprint = parts.Aggregate(parts[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; });
        var material = new Material(shader) { color = player ? PlayerOutline : EnemyOutline };
        material.SetFloat("_Width", Mathf.Max(footprint.size.x, footprint.size.z) * WidthShare);
        foreach (var part in parts)
        {
            foreach (var bodyMaterial in part.materials)
                bodyMaterial.renderQueue = BodyQueue;
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
            var outline = new GameObject(OutlineName);
            outline.transform.SetParent(part.transform, false);
            outline.AddComponent<MeshFilter>().sharedMesh = Smoothed(mesh);
            var renderer = outline.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, mesh.subMeshCount)).ToArray();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    // A copy of the mesh whose normals are averaged where faces meet, so the pushed-out outline has no gaps at hard
    // edges. Made once per mesh; a mesh that cannot be read keeps its own normals.
    private static Mesh Smoothed(Mesh mesh)
    {
        if (SmoothedMeshes.TryGetValue(mesh, out var done))
            return done;
        if (!mesh.isReadable)
        {
            Debug.LogWarning("[TankLook] " + mesh.name + " is not readable; its outline keeps the mesh's own normals.");
            SmoothedMeshes[mesh] = mesh;
            return mesh;
        }
        var copy = Object.Instantiate(mesh);
        copy.name = mesh.name + " outline";
        var vertices = copy.vertices;
        var normals = copy.normals;
        var sums = new Dictionary<Vector3Int, Vector3>();
        var keys = new Vector3Int[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            keys[i] = Vector3Int.RoundToInt(vertices[i] * 10000f);
            sums.TryGetValue(keys[i], out var sum);
            sums[keys[i]] = sum + normals[i];
        }
        var smooth = new Vector3[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
            smooth[i] = sums[keys[i]].normalized;
        copy.normals = smooth;
        SmoothedMeshes[mesh] = copy;
        return copy;
    }
}
