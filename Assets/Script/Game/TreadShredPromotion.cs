using System;
using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-019-AC-05: the promotion into Veteran or Elite. Spent shell casings rain over the screen and bounce on the floor to
/// the shell-rain sound (real recordings of brass casings landing on concrete, Resources/Audio/ShellRain), under
/// PROMOTED TO VETERAN, what changes, and the promotion gems. Continues on its own, or on a tap once it has been read.
/// </summary>
public sealed class TreadShredPromotion : MonoBehaviour
{
    public const string SoundResource = "Audio/ShellRain";
    public const int CasingCount = 70;
    private const float Duration = 4.2f;
    private const float TapAfter = 1.5f;
    private const float Gravity = -2600f;
    private const float FloorY = -500f;

    private sealed class Casing
    {
        public RectTransform Rect;
        public Vector2 Velocity;
        public float Spin;
        public float Delay;
        public bool Bounced;
    }

    private readonly List<Casing> casings = new List<Casing>();
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

        var sprite = CasingSprite();
        var halfWidth = 1080f * Screen.width / Mathf.Max(Screen.height, 1) / 2f;
        var random = new System.Random(20261106);
        for (var i = 0; i < CasingCount; i++)
        {
            var rect = new GameObject("Casing", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(18f, 54f);
            rect.anchoredPosition = new Vector2(Range(random, -halfWidth, halfWidth), 600f + Range(random, 0f, 260f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Range(random, 0f, 360f));
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            casings.Add(new Casing
            {
                Rect = rect,
                Velocity = new Vector2(Range(random, -120f, 120f), Range(random, -300f, 0f)),
                Spin = Range(random, -540f, 540f),
                // Dense at first, then stragglers, like the sound.
                Delay = (float)Math.Pow(random.NextDouble(), 1.8) * 2.2f
            });
        }

        TreadShredUi.CreateText(Campaign.PromotionTitle(tier), root, new Vector2(0f, 90f), new Vector2(1400f, 120f), 96, font)
            .color = TreadShredUi.AccentColor;
        TreadShredUi.CreateText(Campaign.PromotionLine, root, new Vector2(0f, -10f), new Vector2(1400f, 50f), 40, font)
            .color = Color.white;
        TreadShredUi.CreateText(PromotionGems(tier), root, new Vector2(0f, -80f), new Vector2(1000f, 46f), 34, font)
            .color = TreadShredUi.TitleColor;

        if (PlayerPrefs.GetFloat("Sound") == 0f)
        {
            var clip = Resources.Load<AudioClip>(SoundResource);
            if (clip == null)
            {
                Debug.LogWarning("[Promotion] " + SoundResource + " is missing; the promotion plays without its sound");
                return;
            }
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.ignoreListenerPause = true;
            source.Play();
        }
    }

    public static string PromotionGems(DifficultyTier tier) => "+" + ProgressionRules.TierUnlockGems(tier) + " GEMS FOR YOUR PROMOTION";

    private void Update()
    {
        var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        elapsed += dt;
        foreach (var casing in casings)
        {
            if (elapsed < casing.Delay)
                continue;
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
