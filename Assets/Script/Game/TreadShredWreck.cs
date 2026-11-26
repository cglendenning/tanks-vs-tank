using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// D-027-AC-02 within P-4: a destroyed enemy tank is blown apart, realistically and violently. Its hull is cut into
/// fragments of its own armor (its own mesh and paint) that are thrown out, spin and bounce, white-hot at first and then
/// smoldering, scorched but keeping a hint of their paint, shedding embers; its turret is blown off whole, a different
/// way each time, and any other part with it; hot sparks spray out; the tank explosion goes up with secondary blasts as the
/// ammunition cooks off, a dust shockwave rolls across the ground and a fire flash lights it; the wreckage burns over a
/// soft scorch mark. Visual only (no colliders), so nothing it throws can block or hit a tank; cleared in a few seconds.
/// </summary>
public static class TreadShredWreck
{
    private const float Gravity = 26f;
    private const int FragmentsAlong = 3;
    private const int FragmentsAcross = 2;
    private const int Sparks = 20;
    // Large flat faces (a low-poly hull's deck is one quad) are subdivided before cutting, so no fragment is a slab.
    private const int SubdivisionDepth = 4;
    private static readonly Color Charred = new Color(0.14f, 0.13f, 0.12f);
    // Heat is emitted light, added over the armor's own texture; the texture keeps half its paint once scorched.
    private static readonly Color WhiteHot = new Color(1f, 0.78f, 0.45f) * 1.4f;
    private static readonly Color Ember = new Color(0.95f, 0.3f, 0.05f);
    private const float PaintKept = 0.5f;
    private static readonly Color SparkHot = new Color(1f, 0.62f, 0.2f);
    private static readonly Color SparkCold = new Color(0.45f, 0.1f, 0.02f);
    private static readonly Color Fire = new Color(1f, 0.55f, 0.12f);

    // Each model mesh is cut once; every later kill of the same tank reuses its fragments.
    private static readonly Dictionary<Mesh, (Mesh Mesh, Vector3 Centre)[]> Cut = new Dictionary<Mesh, (Mesh, Vector3)[]>();

    /// <summary>Wrecks the tank. Call before its model is hidden.</summary>
    public static void Spawn(GameObject tank, GameObject explosion)
    {
        var parts = TreadShredTankLook.ModelParts(tank);
        if (parts.Count == 0)
            return;

        var whole = parts.Aggregate(parts[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; });
        var ground = whole.min.y;
        var size = Mathf.Max(whole.size.x, whole.size.z);

        // The biggest part is the hull: blown into fragments of its own armor. The rest (turret, gun) fly off whole.
        var hull = parts.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
        foreach (var part in parts)
        {
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
            if (part == hull)
            {
                foreach (var (fragment, centre) in Fragments(mesh))
                {
                    var piece = Piece(part, fragment, centre, part.name + " fragment");
                    var outward = Outward(piece.transform.position, whole.center);
                    Burn(Fly(piece, ground, outward * Random.Range(2.5f, 6.5f) + Vector3.up * Random.Range(6f, 11f),
                        Random.insideUnitSphere * 540f, Random.Range(3.6f, 4.4f)), part, size);
                }
            }
            else
            {
                var piece = Piece(part, mesh, Vector3.zero, part.name + " wreck");
                var (velocity, spin, delay) = BlownOff();
                Burn(Fly(piece, ground, velocity, spin, 4f, delay), part, size);
            }
        }

        for (var i = 0; i < Sparks; i++)
            Spark(whole.center, ground, size);
        Mark("Tread Shred Scorch", TreadShredDecal.Disc, new Color(0.03f, 0.025f, 0.02f, 0.85f), whole.center, ground, size * 0.6f,
            size * 2.3f, grow: 0.15f, life: 4.5f, fadeFrom: 3.3f);
        Mark("Tread Shred Blast Ring", TreadShredDecal.Ring, new Color(0.45f, 0.38f, 0.3f, 0.6f), whole.center, ground, size * 0.6f,
            size * 3.6f, grow: 0.45f, life: 0.45f, fadeFrom: 0.1f);

        var runner = new GameObject("Tread Shred Wreck").AddComponent<WreckRunner>();
        runner.StartCoroutine(runner.Blast(whole.center, size, explosion));
    }

    private static Vector3 Outward(Vector3 from, Vector3 centre)
    {
        var outward = Flat(from - centre).normalized;
        return outward == Vector3.zero ? Flat(Random.onUnitSphere).normalized : (outward + Flat(Random.insideUnitSphere) * 0.4f).normalized;
    }

    // A copy of a part's mesh (or a fragment of it) where it sat, in its own paint, pivoting about its own centre.
    private static GameObject Piece(MeshRenderer part, Mesh mesh, Vector3 centre, string name)
    {
        var piece = new GameObject(name);
        piece.transform.SetPositionAndRotation(part.transform.TransformPoint(centre), part.transform.rotation);
        piece.transform.localScale = part.transform.lossyScale;
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = part.sharedMaterials;
        return piece;
    }

    // A turret goes a different way every time: popped straight up spinning flat, thrown sideways tumbling end over
    // end, or lobbed in a wobbling arc; in any direction, and sometimes a split second after the blast.
    private static (Vector3 Velocity, Vector3 Spin, float Delay) BlownOff()
    {
        var direction = Flat(Random.onUnitSphere).normalized;
        var delay = Random.value < 0.4f ? Random.Range(0.05f, 0.18f) : 0f;
        switch (Random.Range(0, 3))
        {
            case 0:
                return (direction * Random.Range(0.5f, 2f) + Vector3.up * Random.Range(15f, 19f),
                    Vector3.up * Random.Range(600f, 900f) * (Random.value < 0.5f ? -1f : 1f) + Random.insideUnitSphere * 120f, delay);
            case 1:
                return (direction * Random.Range(5f, 8f) + Vector3.up * Random.Range(7f, 10f),
                    Vector3.Cross(Vector3.up, direction) * Random.Range(600f, 900f), delay);
            default:
                return (direction * Random.Range(2.5f, 5f) + Vector3.up * Random.Range(11f, 14f),
                    Random.insideUnitSphere.normalized * Random.Range(600f, 900f), delay);
        }
    }

    // Burnt by incendiaries: white-hot, then smoldering metal that keeps a hint of its paint and an ember glow.
    private static void Burn(TreadShredDebris debris, MeshRenderer part, float size)
    {
        var paint = part.sharedMaterial != null && part.sharedMaterial.HasProperty("_Color") ? part.sharedMaterial.color : Color.white;
        debris.Smolder(paint, Color.Lerp(Charred, paint, PaintKept), WhiteHot, Ember, size);
    }

    /// <summary>An ember drifting up off a smoldering piece.</summary>
    public static void EmberFrom(Vector3 at, float ground, float size)
    {
        var ember = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(ember.GetComponent<Collider>());
        ember.name = "Tread Shred Spark";
        ember.transform.position = at;
        ember.transform.localScale = new Vector3(0.04f, 0.04f, 0.08f) * Mathf.Max(1f, size / 2f);
        ember.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var debris = Fly(ember, ground, Flat(Random.onUnitSphere).normalized * Random.Range(0.3f, 1.2f) + Vector3.up * Random.Range(2f, 4f),
            Vector3.zero, Random.Range(0.5f, 0.9f));
        debris.Rise();
        debris.AlignToFlight();
        debris.CoolFrom(SparkHot, SparkCold, 0.7f);
    }

    private static void Spark(Vector3 centre, float ground, float size)
    {
        var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(spark.GetComponent<Collider>());
        spark.name = "Tread Shred Spark";
        spark.transform.position = centre + Vector3.up * size * 0.2f;
        spark.transform.localScale = new Vector3(0.045f, 0.045f, Random.Range(0.1f, 0.18f)) * Mathf.Max(1f, size / 2f);
        spark.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var velocity = Flat(Random.onUnitSphere).normalized * Random.Range(9f, 17f) + Vector3.up * Random.Range(3f, 9f);
        var debris = Fly(spark, ground, velocity, Vector3.zero, Random.Range(0.35f, 0.65f));
        debris.AlignToFlight();
        debris.CoolFrom(SparkHot, SparkCold, 0.6f);
    }

    private static void Mark(string name, Texture2D shape, Color color, Vector3 centre, float ground, float from, float to,
        float grow, float life, float fadeFrom)
    {
        var mark = TreadShredDecal.Create(name, shape, color);
        if (mark == null)
            return;
        mark.transform.position = new Vector3(centre.x, ground + 0.03f, centre.z);
        mark.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        mark.AddComponent<TreadShredMark>().Play(from, to, grow, life, fadeFrom);
    }

    private static TreadShredDebris Fly(GameObject piece, float ground, Vector3 velocity, Vector3 spin, float life, float delay = 0f)
    {
        var debris = piece.AddComponent<TreadShredDebris>();
        debris.Launch(ground, velocity, spin, Gravity, life, delay);
        return debris;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Cuts a mesh into a grid of fragments across its two widest axes, each re-centred on its own middle. Faces wider
    // than a fragment are subdivided first, so every fragment is a jagged piece of armor rather than a whole panel.
    private static (Mesh Mesh, Vector3 Centre)[] Fragments(Mesh mesh)
    {
        if (Cut.TryGetValue(mesh, out var done))
            return done;
        if (!mesh.isReadable)
        {
            Debug.LogWarning("[Wreck] " + mesh.name + " is not readable; it flies as one piece.");
            return Cut[mesh] = new[] { (mesh, Vector3.zero) };
        }
        var bounds = mesh.bounds;
        var axes = new[] { 0, 1, 2 }.OrderByDescending(a => bounds.size[a]).ToArray();
        var positions = new List<Vector3>(mesh.vertices);
        var sourceNormals = mesh.normals;
        var sourceUvs = mesh.uv;
        var normalList = sourceNormals.Length == positions.Count ? new List<Vector3>(sourceNormals) : null;
        var uvList = sourceUvs.Length == positions.Count ? new List<Vector2>(sourceUvs) : null;
        var longest = bounds.size[axes[1]] / FragmentsAcross * 0.75f;
        int Midpoint(int a, int b)
        {
            positions.Add((positions[a] + positions[b]) * 0.5f);
            normalList?.Add(((normalList[a] + normalList[b]) * 0.5f).normalized);
            uvList?.Add((uvList[a] + uvList[b]) * 0.5f);
            return positions.Count - 1;
        }
        var subdivided = new List<int>[mesh.subMeshCount];
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
        {
            subdivided[sub] = new List<int>();
            var source = mesh.GetTriangles(sub);
            var pending = new Stack<(int A, int B, int C, int Depth)>();
            for (var t = 0; t < source.Length; t += 3)
                pending.Push((source[t], source[t + 1], source[t + 2], 0));
            while (pending.Count > 0)
            {
                var (a, b, c, depth) = pending.Pop();
                var edge = Mathf.Max((positions[a] - positions[b]).magnitude, (positions[b] - positions[c]).magnitude, (positions[c] - positions[a]).magnitude);
                if (edge <= longest || depth >= SubdivisionDepth)
                {
                    subdivided[sub].AddRange(new[] { a, b, c });
                    continue;
                }
                int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
                pending.Push((a, ab, ca, depth + 1));
                pending.Push((ab, b, bc, depth + 1));
                pending.Push((ca, bc, c, depth + 1));
                pending.Push((ab, bc, ca, depth + 1));
            }
        }
        var vertices = positions.ToArray();
        var normals = normalList != null ? normalList.ToArray() : new Vector3[0];
        var uvs = uvList != null ? uvList.ToArray() : new Vector2[0];
        int Cell(Vector3 p, int axis, int count) =>
            Mathf.Clamp((int)((p[axis] - bounds.min[axis]) / Mathf.Max(bounds.size[axis], 1e-5f) * count), 0, count - 1);

        var cells = new Dictionary<int, (Dictionary<int, int> Map, List<int>[] Triangles)>();
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var triangles = subdivided[sub];
            for (var t = 0; t < triangles.Count; t += 3)
            {
                var middle = (vertices[triangles[t]] + vertices[triangles[t + 1]] + vertices[triangles[t + 2]]) / 3f;
                var key = Cell(middle, axes[0], FragmentsAlong) * FragmentsAcross + Cell(middle, axes[1], FragmentsAcross);
                if (!cells.TryGetValue(key, out var cell))
                {
                    cell = (new Dictionary<int, int>(), Enumerable.Range(0, mesh.subMeshCount).Select(_ => new List<int>()).ToArray());
                    cells[key] = cell;
                }
                for (var k = 0; k < 3; k++)
                {
                    var index = triangles[t + k];
                    if (!cell.Map.TryGetValue(index, out var mapped))
                        cell.Map[index] = mapped = cell.Map.Count;
                    cell.Triangles[sub].Add(mapped);
                }
            }
        }

        var fragments = new List<(Mesh, Vector3)>();
        foreach (var cell in cells.Values)
        {
            var order = cell.Map.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
            var centre = order.Aggregate(Vector3.zero, (sum, i) => sum + vertices[i]) / order.Length;
            var fragment = new Mesh { name = mesh.name + " fragment" };
            fragment.SetVertices(order.Select(i => vertices[i] - centre).ToList());
            if (normals.Length == vertices.Length)
                fragment.SetNormals(order.Select(i => normals[i]).ToList());
            if (uvs.Length == vertices.Length)
                fragment.SetUVs(0, order.Select(i => uvs[i]).ToList());
            fragment.subMeshCount = mesh.subMeshCount;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
                fragment.SetTriangles(cell.Triangles[sub], sub);
            fragment.RecalculateBounds();
            fragments.Add((fragment, centre));
        }
        return Cut[mesh] = fragments.ToArray();
    }

    private sealed class WreckRunner : MonoBehaviour
    {
        public IEnumerator Blast(Vector3 centre, float size, GameObject explosion)
        {
            if (explosion != null)
                Object.Instantiate(explosion, centre, Quaternion.Euler(-90, 0, 0)).transform.localScale *= 1.8f;
            var light = new GameObject("Tread Shred Fire Flash").AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Fire;
            light.range = size * 8f;
            light.transform.position = centre + Vector3.up * size;

            // The ammunition cooks off in two more blasts around the hull.
            var cookOffs = new[] { 0.15f, 0.32f };
            var next = 0;
            var elapsed = 0f;
            while (elapsed < 0.7f)
            {
                light.intensity = 12f * (1f - elapsed / 0.7f);
                if (next < cookOffs.Length && elapsed >= cookOffs[next] && explosion != null)
                {
                    Object.Instantiate(explosion, centre + Flat(Random.onUnitSphere).normalized * size * 0.5f, Quaternion.Euler(-90, 0, 0));
                    next++;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // The wreckage burns: a low, flickering fire glow.
            light.range = size * 4f;
            light.transform.position = centre + Vector3.up * size * 0.5f;
            for (var burn = 0f; burn < 1.8f; burn += Time.deltaTime)
            {
                light.intensity = Random.Range(1.5f, 3.5f) * (1f - burn / 1.8f);
                yield return null;
            }
            Destroy(light.gameObject);
            Destroy(gameObject);
        }
    }
}

/// <summary>D-027-AC-02: one piece of a wreck, thrown, spun, bounced on the ground, burnt and cleared. Moves itself, so
/// it never touches the physics the tanks and shells use.</summary>
public sealed class TreadShredDebris : MonoBehaviour
{
    private float ground;
    private Vector3 velocity;
    private Vector3 spin;
    private float gravity;
    private float life;
    private float age;
    private bool alongFlight;
    private Vector3 scale;
    private Material[] cooling;
    private Color hotColor;
    private Color coldColor;
    private float coolSeconds;
    private Renderer drawn;
    private float delay;
    private bool rising;
    private bool smoldering;
    private Color paint;
    private Color scorched;
    private Color ember;
    private float phase;
    private float nextEmber;
    private float size;

    // A smoldering piece is white-hot for this long, then cools to scorched metal over the next second, its ember glow
    // dying from the first strength to the last over its life.
    private const float WhiteHotSeconds = 0.2f;
    private const float CoolingSeconds = 1f;
    private const float EmberSeconds = 1.8f;
    private const float GlowFirst = 0.5f;
    private const float GlowLast = 0.2f;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public void Launch(float groundY, Vector3 launch, Vector3 turn, float fall, float seconds, float wait = 0f)
    {
        ground = groundY;
        velocity = launch;
        spin = turn;
        gravity = fall;
        life = seconds;
        delay = wait;
        scale = transform.localScale;
        drawn = GetComponent<Renderer>();
    }

    /// <summary>An ember: it floats up instead of falling.</summary>
    public void Rise() => rising = true;

    /// <summary>Burnt by incendiaries: white-hot, cooling to scorched paint with a flickering, fading ember glow, and
    /// shedding embers while it is hot.</summary>
    public void Smolder(Color ownPaint, Color scorchedPaint, Color whiteHot, Color glow, float pieceSize)
    {
        cooling = drawn.materials;
        foreach (var material in cooling)
            material.EnableKeyword("_EMISSION");
        paint = ownPaint;
        hotColor = whiteHot;
        scorched = scorchedPaint;
        ember = glow;
        smoldering = true;
        size = pieceSize;
        phase = Random.Range(0f, 10f);
        nextEmber = Random.Range(0.15f, 0.35f);
    }

    /// <summary>A spark: it points along its flight and shrinks out instead of settling.</summary>
    public void AlignToFlight() => alongFlight = true;

    public void CoolFrom(Color hot, Color cold, float seconds)
    {
        cooling = drawn.materials;
        hotColor = hot;
        coldColor = cold;
        coolSeconds = seconds;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        age += dt;
        var fade = life - age;
        var sinking = !alongFlight && fade < 0.6f;
        if (smoldering)
            Glow();
        if (age < delay)
            return;
        velocity = sinking ? Vector3.zero : rising ? velocity : velocity + Vector3.down * gravity * dt;
        transform.position += velocity * dt;
        if (alongFlight && velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(velocity);
        else
            transform.Rotate(spin * dt, Space.World);

        // Keep the piece's lowest point on the ground (meshes differ in scale and pivot), bouncing when it lands hard.
        var below = ground - drawn.bounds.min.y;
        if (!sinking && below > 0f)
        {
            transform.position += Vector3.up * below;
            if (velocity.y < -2f)
            {
                velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f);
                spin *= 0.5f;
            }
            else
            {
                velocity = new Vector3(velocity.x * 0.8f, 0f, velocity.z * 0.8f);
                spin *= 0.8f;
            }
        }

        if (cooling != null && !smoldering)
        {
            var color = Color.Lerp(hotColor, coldColor, Mathf.Clamp01(age / coolSeconds));
            foreach (var material in cooling)
                material.color = color;
        }

        if (alongFlight)
            transform.localScale = scale * Mathf.Clamp01(fade / life * 2f);
        else if (sinking)
            transform.position += Vector3.down * dt * 0.8f;
        if (age >= life)
            Destroy(gameObject);
    }

    // White-hot, then scorched paint under an ember glow that flickers and dies down; embers rise while it is hot.
    private void Glow()
    {
        var cooled = Mathf.Clamp01((age - WhiteHotSeconds) / CoolingSeconds);
        var smolder = Mathf.Lerp(GlowFirst, GlowLast, Mathf.Clamp01(age / life));
        var flicker = 0.65f + 0.35f * Mathf.Sin(age * 9f + phase) * Mathf.Sin(age * 3.7f + phase * 2f);
        var surface = Color.Lerp(paint, scorched, cooled);
        var heat = Color.Lerp(hotColor, ember * (smolder * flicker), cooled);
        surface.a = paint.a;
        foreach (var material in cooling)
        {
            material.color = surface;
            if (material.HasProperty(EmissionColor))
                material.SetColor(EmissionColor, heat);
        }

        if (age < EmberSeconds && age >= nextEmber)
        {
            nextEmber = age + Random.Range(0.25f, 0.45f);
            TreadShredWreck.EmberFrom(drawn.bounds.center + Vector3.up * drawn.bounds.extents.y, ground, size);
        }
    }
}

/// <summary>D-027-AC-02: a mark on the ground (scorch, blast ring) that spreads, holds and fades.</summary>
public sealed class TreadShredMark : MonoBehaviour
{
    private float from;
    private float to;
    private float grow;
    private float life;
    private float fadeFrom;
    private float age;
    private Material material;
    private float alpha;

    public void Play(float startSize, float endSize, float spread, float seconds, float fadeAfter)
    {
        from = startSize;
        to = endSize;
        grow = spread;
        life = seconds;
        fadeFrom = fadeAfter;
        material = GetComponent<Renderer>().material;
        alpha = material.color.a;
        transform.localScale = Vector3.one * from;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }
        var t = Mathf.Clamp01(age / grow);
        transform.localScale = Vector3.one * Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t));
        var color = material.color;
        color.a = age < fadeFrom ? alpha : alpha * (1f - (age - fadeFrom) / (life - fadeFrom));
        material.color = color;
    }
}
