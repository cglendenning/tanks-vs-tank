using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// D-027-AC-02: an enemy tank does not just vanish in a puff, it is wrecked. A fireball swells and a fire flash lights
/// the ground; the tank itself is thrown into the air, flipping, and slams down charred over a scorch mark, any
/// separate turret or part blown off on its own; armor plates, track links and red-hot chunks cut from its own metal
/// scatter and bounce; a second blast follows as the ammunition cooks off and black smoke rolls up from the wreck.
/// Everything is visual only (no colliders), so nothing it throws can block or hit a tank, and it all clears within
/// a few seconds.
/// </summary>
public static class TreadShredWreck
{
    private const float Gravity = 26f;
    private const int Plates = 12;
    private const int TrackLinks = 8;
    private const int HotChunks = 14;
    private const int SmokePuffs = 12;
    private static readonly Color Charred = new Color(0.16f, 0.15f, 0.14f);
    private static readonly Color Glow = new Color(1f, 0.55f, 0.12f);
    private static readonly Color WhiteHot = new Color(1f, 0.85f, 0.35f);

    /// <summary>Wrecks the tank. Call before its model is hidden.</summary>
    public static void Spawn(GameObject tank, GameObject explosion)
    {
        var parts = TreadShredTankLook.ModelParts(tank);
        if (parts.Count == 0)
            return;

        var centre = parts.Aggregate(parts[0].bounds, (b, r) => { b.Encapsulate(r.bounds); return b; });
        var ground = centre.min.y;
        var size = Mathf.Max(centre.size.x, centre.size.z);
        var metal = parts[0].sharedMaterial;

        // The biggest piece is the hull: thrown up and flipped, it lands charred where it died. The rest is blown off.
        var hull = parts.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
        var flipAxis = Vector3.Cross(Vector3.up, Flat(Random.onUnitSphere)).normalized;
        foreach (var part in parts)
        {
            var copy = Copy(part);
            Tint(copy, Charred);
            var isHull = part == hull;
            var outward = Flat(part.bounds.center - centre.center).normalized;
            if (outward == Vector3.zero)
                outward = Flat(Random.onUnitSphere).normalized;
            Fly(copy, ground,
                isHull
                    ? Vector3.up * Random.Range(9f, 11f) + Flat(Random.onUnitSphere) * 1.5f
                    : outward * Random.Range(3.5f, 6f) + Vector3.up * Random.Range(12f, 15f),
                isHull ? flipAxis * Random.Range(380f, 560f) : Random.insideUnitSphere * 720f,
                life: isHull ? 4.5f : 3.5f, sinkAtEnd: isHull);
        }

        Scorch(centre.center, ground, size);
        for (var i = 0; i < Plates; i++)
            Chunk(centre.center, ground, size, metal, new Vector3(0.65f, 0.1f, 0.48f), Charred, speed: 9f);
        for (var i = 0; i < TrackLinks; i++)
            Chunk(centre.center, ground, size, metal, new Vector3(0.18f, 0.13f, 0.6f), new Color(0.1f, 0.1f, 0.1f), speed: 8f);
        for (var i = 0; i < HotChunks; i++)
            Chunk(centre.center, ground, size, null, Vector3.one * Random.Range(0.22f, 0.45f), Glow, speed: 11f, hot: true);

        // A white-hot flash for a split second, then the game's own fireball at full size (WreckRunner); smoke keeps
        // rolling up from the wreck in small, staggered puffs.
        Puff(centre.center + Vector3.up * size * 0.3f, 0f, 0.07f, 0.2f, size * 0.4f, size * 1.4f, Vector3.zero, Color.white, WhiteHot);
        for (var i = 0; i < SmokePuffs; i++)
        {
            var life = Random.Range(1.6f, 2.6f);
            var grey = Random.Range(0.12f, 0.22f);
            Puff(centre.center + Flat(Random.insideUnitSphere) * size * 0.35f, Random.Range(0.2f, 1.4f), life * 0.8f, life,
                size * 0.15f, size * Random.Range(0.45f, 0.8f), Vector3.up * Random.Range(1.5f, 2.6f) + Flat(Random.onUnitSphere) * 0.4f,
                new Color(grey, grey * 0.9f, grey * 0.8f), new Color(grey + 0.15f, grey + 0.15f, grey + 0.15f));
        }

        var runner = new GameObject("Tread Shred Wreck").AddComponent<WreckRunner>();
        runner.StartCoroutine(runner.Blast(centre.center, ground, size, explosion));
    }

    private static GameObject Copy(MeshRenderer part)
    {
        var copy = new GameObject(part.name + " wreck");
        copy.transform.SetPositionAndRotation(part.transform.position, part.transform.rotation);
        copy.transform.localScale = part.transform.lossyScale;
        copy.AddComponent<MeshFilter>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
        copy.AddComponent<MeshRenderer>().sharedMaterials = part.sharedMaterials;
        return copy;
    }

    private static void Tint(GameObject piece, Color color)
    {
        foreach (var material in piece.GetComponent<Renderer>().materials)
        {
            if (material.HasProperty("_Color"))
                material.color = material.color * color;
        }
    }

    private static void Chunk(Vector3 centre, float ground, float size, Material metal, Vector3 scale, Color color, float speed,
        bool hot = false)
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(piece.GetComponent<Collider>());
        piece.transform.position = centre + Flat(Random.insideUnitSphere) * size * 0.3f + Vector3.up * 0.3f;
        piece.transform.rotation = Random.rotation;
        piece.transform.localScale = scale * Random.Range(0.8f, 1.4f) * Mathf.Max(1f, size / 2f);
        var renderer = piece.GetComponent<Renderer>();
        if (metal != null)
            renderer.sharedMaterial = metal;
        Tint(piece, color);
        var direction = Flat(Random.onUnitSphere).normalized;
        var debris = Fly(piece, ground, direction * Random.Range(speed * 0.5f, speed) + Vector3.up * Random.Range(7f, 13f),
            Random.insideUnitSphere * 900f, life: Random.Range(2.4f, 3.4f), sinkAtEnd: false);
        if (hot)
            debris.CoolFrom(WhiteHot, Charred, 1.6f);
    }

    private static void Scorch(Vector3 centre, float ground, float size)
    {
        var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(mark.GetComponent<Collider>());
        mark.name = "Tread Shred Scorch";
        mark.transform.position = new Vector3(centre.x, ground + 0.02f, centre.z);
        mark.transform.localScale = new Vector3(size * 1.6f, 0.005f, size * 1.6f);
        mark.GetComponent<Renderer>().material.color = new Color(0.05f, 0.04f, 0.035f);
        Object.Destroy(mark, 4.5f);
    }

    private static void Puff(Vector3 at, float delay, float grow, float life, float startSize, float peakSize, Vector3 rise,
        Color from, Color to)
    {
        var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(puff.GetComponent<Collider>());
        puff.name = "Tread Shred Puff";
        puff.transform.position = at;
        puff.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        puff.AddComponent<TreadShredPuff>().Play(delay, grow, life, startSize, peakSize, rise, from, to);
    }

    private static TreadShredDebris Fly(GameObject piece, float ground, Vector3 velocity, Vector3 spin, float life, bool sinkAtEnd)
    {
        var debris = piece.AddComponent<TreadShredDebris>();
        debris.Launch(ground, velocity, spin, Gravity, life, sinkAtEnd);
        return debris;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private sealed class WreckRunner : MonoBehaviour
    {
        public IEnumerator Blast(Vector3 centre, float ground, float size, GameObject explosion)
        {
            var light = new GameObject("Tread Shred Fire Flash").AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Glow;
            light.range = size * 8f;
            light.transform.position = centre + Vector3.up * size;
            if (explosion != null)
            {
                var fireball = Object.Instantiate(explosion, centre, Quaternion.Euler(-90, 0, 0));
                fireball.transform.localScale *= 1.8f;
            }
            var elapsed = 0f;
            var cookedOff = false;
            while (elapsed < 0.7f)
            {
                light.intensity = 12f * (1f - elapsed / 0.7f);
                if (!cookedOff && elapsed >= 0.22f && explosion != null)
                {
                    cookedOff = true;
                    var second = Object.Instantiate(explosion, centre + Flat(Random.onUnitSphere) * size * 0.35f, Quaternion.Euler(-90, 0, 0));
                    second.transform.localScale *= 1.3f;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(light.gameObject);
            Destroy(gameObject);
        }
    }
}

/// <summary>D-027-AC-02: one piece of a wreck, thrown, spun, bounced on the ground and cleared. Moves itself, so it
/// never touches the physics the tanks and shells use.</summary>
public sealed class TreadShredDebris : MonoBehaviour
{
    private float ground;
    private Vector3 velocity;
    private Vector3 spin;
    private float gravity;
    private float life;
    private bool sinkAtEnd;
    private float age;
    private Vector3 scale;
    private Material[] cooling;
    private Color hotColor;
    private Color coldColor;
    private float coolSeconds;
    private Renderer drawn;

    public void Launch(float groundY, Vector3 launch, Vector3 turn, float fall, float seconds, bool sink)
    {
        ground = groundY;
        velocity = launch;
        spin = turn;
        gravity = fall;
        life = seconds;
        sinkAtEnd = sink;
        scale = transform.localScale;
        drawn = GetComponent<Renderer>();
    }

    public void CoolFrom(Color hot, Color cold, float seconds)
    {
        cooling = GetComponent<Renderer>().materials;
        hotColor = hot;
        coldColor = cold;
        coolSeconds = seconds;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        age += dt;
        var fade = life - age;
        var sinking = sinkAtEnd && fade < 0.6f;
        velocity = sinking ? Vector3.zero : velocity + Vector3.down * gravity * dt;
        transform.position += velocity * dt;
        transform.Rotate(spin * dt, Space.World);

        // Keep the piece's lowest point on the ground (meshes differ in scale and pivot), bouncing when it lands hard.
        var below = ground - drawn.bounds.min.y;
        if (!sinking && below > 0f)
        {
            transform.position += Vector3.up * below;
            if (velocity.y < -2f)
            {
                velocity = new Vector3(velocity.x * 0.55f, -velocity.y * 0.32f, velocity.z * 0.55f);
                spin *= 0.5f;
            }
            else
            {
                velocity = new Vector3(velocity.x * 0.85f, 0f, velocity.z * 0.85f);
                spin *= 0.85f;
            }
        }

        if (cooling != null)
        {
            var color = Color.Lerp(hotColor, coldColor, Mathf.Clamp01(age / coolSeconds));
            foreach (var material in cooling)
                material.color = color;
        }

        if (fade < 0.6f)
        {
            if (sinking)
                transform.position += Vector3.down * dt * 0.8f;
            else
                transform.localScale = scale * Mathf.Max(0f, fade / 0.6f);
        }
        if (age >= life)
            Destroy(gameObject);
    }
}

/// <summary>D-027-AC-02: a fireball or smoke puff: swells, drifts up, changes colour and shrinks away.</summary>
public sealed class TreadShredPuff : MonoBehaviour
{
    private float age;
    private float delay;
    private float grow;
    private float life;
    private float startSize;
    private float peakSize;
    private Vector3 rise;
    private Color from;
    private Color to;
    private Renderer drawn;
    private Material material;

    public void Play(float wait, float swell, float seconds, float smallest, float largest, Vector3 drift, Color first, Color last)
    {
        delay = wait;
        grow = swell;
        life = seconds;
        startSize = smallest;
        peakSize = largest;
        rise = drift;
        from = first;
        to = last;
        drawn = GetComponent<Renderer>();
        material = drawn.material;
        drawn.enabled = false;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        age += Time.deltaTime;
        var t = age - delay;
        if (t < 0f)
            return;
        drawn.enabled = true;
        if (t >= life)
        {
            Destroy(gameObject);
            return;
        }
        var size = t < grow
            ? Mathf.Lerp(startSize, peakSize, 1f - (1f - t / grow) * (1f - t / grow))
            : Mathf.Lerp(peakSize, 0f, (t - grow) / (life - grow));
        transform.localScale = Vector3.one * size;
        transform.position += rise * Time.deltaTime;
        material.color = Color.Lerp(from, to, t / life);
    }
}
