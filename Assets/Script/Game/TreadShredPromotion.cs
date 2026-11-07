using System;
using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-019-AC-05: the promotion into Veteran or Elite, the moment Mission 40 or 80 is cleared. Spent shell casings rain
/// over the screen for the whole of a victory march (the U.S. Army Band's "The Washington Post", Resources/Audio/
/// PromotionMarch) and bounce on the floor to the shell-rain sound (real recordings of brass casings landing on
/// concrete, Resources/Audio/ShellRain), under PROMOTED TO VETERAN, what changes, and the promotion gems. The march
/// follows the Music setting and the rain the Sound setting. Continues when the march ends, or on a tap after
/// TapAfter seconds.
/// </summary>
public sealed class TreadShredPromotion : MonoBehaviour
{
    public const string SoundResource = "Audio/ShellRain";
    public const string MusicResource = "Audio/PromotionMarch";
    // The march's length plus its last chord's ring-out; the rain falls throughout, thinning over the last stretch.
    public const float Duration = 10.3f;
    public const float TapAfter = 5f;
    public const float CasingsPerSecond = 22f;
    private const float RainStops = Duration - 1.2f;
    private const float RainVolume = 0.45f;
    private const int Pool = 150;
    private const float FadeAfterResting = 1.4f;
    private const float Gravity = -2600f;
    private const float FloorY = -500f;

    private sealed class Casing
    {
        public RectTransform Rect;
        public Vector2 Velocity;
        public float Spin;
        public bool Bounced;
        public float Resting;
        public Image Image;
    }

    private readonly List<Casing> casings = new List<Casing>();
    private RectTransform root;
    private Sprite sprite;
    private System.Random random;
    private float halfWidth;
    private float toSpawn;
    private Action done;
    private float elapsed;
    private bool finished;

    public static TreadShredPromotion Show(DifficultyTier tier, Font font, Action onDone)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredPromotion", 200);
        var promotion = root.gameObject.AddComponent<TreadShredPromotion>();
        promotion.done = onDone;
        promotion.Build(root, tier, font);
        return promotion;
    }

    private void Build(RectTransform root, DifficultyTier tier, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.88f);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(() =>
        {
            if (elapsed >= TapAfter)
                Finish();
        });

        this.root = root;
        sprite = CasingSprite();
        halfWidth = 1080f * Screen.width / Mathf.Max(Screen.height, 1) / 2f;
        random = new System.Random(20261106);

        TreadShredUi.CreateText(Campaign.PromotionTitle(tier), root, new Vector2(0f, 90f), new Vector2(1400f, 120f), 96, font)
            .color = TreadShredUi.AccentColor;
        TreadShredUi.CreateText(Campaign.PromotionLine, root, new Vector2(0f, -10f), new Vector2(1400f, 50f), 40, font)
            .color = Color.white;
        TreadShredUi.CreateText(PromotionGems(tier), root, new Vector2(0f, -80f), new Vector2(1000f, 46f), 34, font)
            .color = TreadShredUi.TitleColor;

        // The march with the Music setting on, the rain with Sound on (0 means on for both).
        Play(MusicResource, 1f, PlayerPrefs.GetFloat("Music") == 0f);
        Play(SoundResource, RainVolume, PlayerPrefs.GetFloat("Sound") == 0f);
    }

    private void Play(string resource, float volume, bool on)
    {
        if (!on)
            return;
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null)
        {
            Debug.LogWarning("[Promotion] " + resource + " is missing; the promotion plays without it");
            return;
        }
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.ignoreListenerPause = true;
        source.Play();
    }

    // A casing at the top of the screen; a settled one that has faded is reused once the pool is full.
    private void Spawn()
    {
        var casing = casings.Count < Pool ? null : casings.Find(c => c.Resting > FadeAfterResting + 0.6f);
        if (casing == null)
        {
            if (casings.Count >= Pool)
                return;
            var rect = new GameObject("Casing", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.SetSiblingIndex(1);
            rect.sizeDelta = new Vector2(18f, 54f);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            casing = new Casing { Rect = rect, Image = image };
            casings.Add(casing);
        }
        casing.Rect.anchoredPosition = new Vector2(Range(random, -halfWidth, halfWidth), 600f + Range(random, 0f, 120f));
        casing.Rect.localRotation = Quaternion.Euler(0f, 0f, Range(random, 0f, 360f));
        casing.Velocity = new Vector2(Range(random, -120f, 120f), Range(random, -300f, 0f));
        casing.Spin = Range(random, -540f, 540f);
        casing.Bounced = false;
        casing.Resting = 0f;
        casing.Image.color = Color.white;
    }

    public static string PromotionGems(DifficultyTier tier) => "+" + ProgressionRules.TierUnlockGems(tier) + " GEMS FOR YOUR PROMOTION";

    private void Update()
    {
        var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        elapsed += dt;
        if (elapsed < RainStops)
        {
            // D-019-AC-05: the rain keeps falling while the march plays.
            for (toSpawn += dt * CasingsPerSecond; toSpawn >= 1f; toSpawn--)
                Spawn();
        }
        foreach (var casing in casings)
        {
            if (casing.Bounced && casing.Velocity == Vector2.zero)
            {
                // Settled on the floor: it lies there a moment, then fades so the floor never fills up.
                casing.Resting += dt;
                if (casing.Resting > FadeAfterResting)
                    casing.Image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (casing.Resting - FadeAfterResting) / 0.6f));
                continue;
            }
            casing.Velocity.y += Gravity * dt;
            var position = casing.Rect.anchoredPosition + casing.Velocity * dt;
            if (position.y < FloorY)
            {
                position.y = FloorY;
                // One lively bounce, then they settle on the floor.
                casing.Velocity = casing.Bounced ? Vector2.zero : new Vector2(casing.Velocity.x * 0.6f, -casing.Velocity.y * 0.35f);
                casing.Spin *= casing.Bounced ? 0f : 0.5f;
                casing.Bounced = true;
            }
            casing.Rect.anchoredPosition = position;
            casing.Rect.Rotate(0f, 0f, casing.Spin * dt);
        }
        if (elapsed >= Duration)
            Finish();
    }

    private void Finish()
    {
        if (finished)
            return;
        finished = true;
        Destroy(gameObject);
        done?.Invoke();
    }

    private static float Range(System.Random random, float min, float max) => (float)(min + random.NextDouble() * (max - min));

    // A spent brass casing: a shaded brass body, a darker rim and primer at the base, an open mouth at the top.
    private static Sprite CasingSprite()
    {
        const int width = 12, height = 36;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var brass = new Color(0.86f, 0.66f, 0.26f);
        var shade = new Color(0.55f, 0.38f, 0.12f);
        var highlight = new Color(1f, 0.9f, 0.6f);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                // Rounded edges across the body: bright near a third of the width, darker at the sides.
                var across = Mathf.Abs((x + 0.5f) / width - 0.35f) / 0.65f;
                var color = Color.Lerp(highlight, brass, Mathf.Clamp01(across * 2.2f));
                color = Color.Lerp(color, shade, Mathf.Clamp01((across - 0.45f) * 1.6f));
                if (y < 4)
                    color = Color.Lerp(color, shade, 0.55f);
                if (y < 2 && x > 3 && x < 8)
                    color = new Color(0.45f, 0.42f, 0.38f);
                if (y >= height - 2)
                    color = Color.Lerp(color, new Color(0.2f, 0.14f, 0.06f), 0.6f);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
