using System;
using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-032-AC-02: before Play Games' settings open, shows a private player the two taps that make the profile public,
/// beside pictures of Google's own screens, then opens them. NOT NOW or the system Back closes it.
/// </summary>
public sealed class TreadShredProfileHelp : MonoBehaviour
{
    // Cropped from Play Games' settings, with gold rings on the button and the switch. Kept as PNG bytes and decoded at
    // full quality, so texture compression never blurs Google's small print.
    public const string StepOneImage = "ProfileHelp/step-1-privacy-settings";
    public const string StepTwoImage = "ProfileHelp/step-2-make-profile-private";
    private const float ImageWidth = 620f;
    private const float ImagesTop = 185f;

    private readonly List<Texture2D> textures = new List<Texture2D>();
    private Func<bool> openSettings;
    private Text fallback;

    /// <summary>Opens the dialog; <paramref name="openSettings"/> opens Play Games and says whether it did.</summary>
    public static TreadShredProfileHelp Show(Font font, Func<bool> openSettings)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredProfileHelp", 135);
        var help = root.gameObject.AddComponent<TreadShredProfileHelp>();
        help.openSettings = openSettings;
        help.Build(root, font);
        return help;
    }

    private void Build(RectTransform root, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.9f);
        var panel = TreadShredUi.CreateRect("ProfileHelpPanel", backdrop, Vector2.zero, new Vector2(1360f, 920f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);
        var heading = TreadShredUi.CreateText(LeaderboardRules.MakePublicButton, panel, new Vector2(0f, 390f),
            new Vector2(1280f, 70f), 52, font);
        heading.color = TreadShredUi.TitleColor;
        var intro = TreadShredUi.CreateText(LeaderboardRules.ProfileHelpIntro, panel, new Vector2(0f, 312f),
            new Vector2(1280f, 60f), 26, font);
        intro.color = TreadShredUi.DetailColor;
        TreadShredUi.ShrinkToFit(intro, 18);
        Step(LeaderboardRules.ProfileHelpStepOne, StepOneImage, panel, -330f, font);
        Step(LeaderboardRules.ProfileHelpStepTwo, StepTwoImage, panel, 330f, font);
        TreadShredUi.CreateText(LeaderboardRules.ProfileHelpStepThree, panel, new Vector2(0f, -80f),
            new Vector2(1280f, 50f), 30, font);
        fallback = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -170f), new Vector2(1280f, 40f), 22, font);
        fallback.color = TreadShredUi.DetailColor;
        TreadShredUi.CreateButton(LeaderboardRules.NotNow, panel, new Vector2(-300f, -345f), new Vector2(420f, 110f), 38,
            font, Close);
        TreadShredUi.CreateButton(LeaderboardRules.OpenPlayGames, panel, new Vector2(260f, -345f), new Vector2(560f, 110f),
            38, font, Open);
    }

    // A numbered caption with its picture beneath, the pictures' tops aligned.
    private void Step(string caption, string image, RectTransform panel, float x, Font font)
    {
        TreadShredUi.CreateText(caption, panel, new Vector2(x, 225f), new Vector2(ImageWidth, 50f), 30, font);
        var png = Resources.Load<TextAsset>(image);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (png == null || !texture.LoadImage(png.bytes))
        {
            Debug.LogError("[ProfileHelp] picture missing or unreadable: " + image);
            Destroy(texture);
            return;
        }
        textures.Add(texture);
        var height = ImageWidth * texture.height / texture.width;
        var picture = TreadShredUi.CreateRect("StepPicture", panel, new Vector2(x, ImagesTop - height / 2f),
            new Vector2(ImageWidth, height), typeof(RectTransform), typeof(RawImage));
        picture.GetComponent<RawImage>().texture = texture;
    }

    // Closes once Play Games opens; when it cannot, the steps to do it by hand appear and the dialog stays.
    private void Open()
    {
        if (openSettings())
            Close();
        else
            fallback.text = LeaderboardRules.MakeProfilePublic;
    }

    private void Close() => Destroy(gameObject);

    private void Start() => TreadShredBack.Opened(this, Close);

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        foreach (var texture in textures)
            Destroy(texture);
    }
}
