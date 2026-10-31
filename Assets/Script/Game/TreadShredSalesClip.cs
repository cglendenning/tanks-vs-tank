using TreadShred.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// D-034: a looping, silent clip of a purchasable item working in real gameplay. It runs on unscaled time, so
/// it keeps playing while game time is paused, and tapping it opens the item's sales screen.
/// </summary>
public sealed class TreadShredSalesClip : MonoBehaviour
{
    private RenderTexture frame;

    public static RawImage Create(RectTransform parent, Vector2 position, Vector2 size, SaleItem item, Font font,
        UnityAction onTap)
    {
        var rect = TreadShredUi.CreateRect("SalesClip " + item, parent, position, size,
            typeof(RectTransform), typeof(RawImage), typeof(Outline));
        var image = rect.GetComponent<RawImage>();
        // Black until the first frame is decoded.
        image.color = Color.black;
        var outline = rect.GetComponent<Outline>();
        outline.effectColor = TreadShredUi.AccentColor;
        outline.effectDistance = new Vector2(3f, -3f);
        rect.gameObject.AddComponent<TreadShredSalesClip>().Play(item, image);

        if (onTap != null)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onTap);
            var bandHeight = Mathf.Round(size.y * 0.2f);
            var band = TreadShredUi.CreateRect("Caption", rect, new Vector2(0f, (bandHeight - size.y) / 2f),
                new Vector2(size.x, bandHeight), typeof(RectTransform), typeof(Image));
            band.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            band.GetComponent<Image>().raycastTarget = false;
            var caption = TreadShredUi.CreateText(SalesPitches.ClipCaption, band, Vector2.zero,
                new Vector2(size.x - 12f, bandHeight), Mathf.RoundToInt(bandHeight * 0.55f), font);
            caption.color = TreadShredUi.TitleColor;
            caption.raycastTarget = false;
        }
        return image;
    }

    /// <summary>Loops silently into the frame on unscaled time (D-034-AC-02).</summary>
    public static void Configure(VideoPlayer player, VideoClip video, RenderTexture target)
    {
        player.playOnAwake = false;
        player.source = VideoSource.VideoClip;
        player.clip = video;
        player.isLooping = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = target;
        player.skipOnDrop = true;
    }

    private void Play(SaleItem item, RawImage image)
    {
        // Batch test runs have no graphics device to decode into; the frame stays black.
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            return;
        var video = Resources.Load<VideoClip>(SalesPitches.ClipResource(item));
        if (video == null)
        {
            Debug.LogError("[SalesClip] Missing clip Resources/" + SalesPitches.ClipResource(item));
            return;
        }
        frame = new RenderTexture((int)video.width, (int)video.height, 0);
        frame.Create();
        image.texture = frame;
        var player = gameObject.AddComponent<VideoPlayer>();
        Configure(player, video, frame);
        player.prepareCompleted += _ => image.color = Color.white;
        player.errorReceived += (_, message) => Debug.LogError("[SalesClip] " + item + ": " + message);
        player.Play();
    }

    private void OnDestroy()
    {
        if (frame == null)
            return;
        frame.Release();
        Destroy(frame);
    }
}
