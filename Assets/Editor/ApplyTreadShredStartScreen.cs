#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ApplyTreadShredStartScreen
{
    private const string ScenePath = "Assets/Scence/Start.unity";
    private const string BackdropPath = "Assets/Image/TreadShredCommandBaseBackdrop.png";
    private const string BasePath = "Assets/Image/TreadShredBaseIcon.png";
    private const string AdvancePath = "Assets/Image/TreadShredAdvanceIcon.png";
    private const string SettingsPath = "Assets/Image/TreadShredSettingsIcon.png";
    private const string FontPath = "Assets/Art/BlackOpsOne-Regular.ttf";

    public static void Run()
    {
        ImportSprite(BackdropPath, 2048);
        ImportSprite(BasePath, 2048);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var panelStart = FindByName(scene.GetRootGameObjects(), "PanelStart");
        if (panelStart == null)
            throw new System.InvalidOperationException("Could not find PanelStart in " + ScenePath);

        var backdrop = EnsureImage(panelStart.transform, "TreadShredCommandBaseBackdrop");
        SetStretch(backdrop.GetComponent<RectTransform>());
        backdrop.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
        backdrop.preserveAspect = false;
        backdrop.color = new Color(1f, 1f, 1f, 0.90f);
        backdrop.raycastTarget = false;
        backdrop.transform.SetSiblingIndex(0);

        var vignette = EnsureImage(panelStart.transform, "TreadShredCommandBaseVignette");
        SetStretch(vignette.GetComponent<RectTransform>());
        vignette.sprite = null;
        vignette.color = new Color(0.005f, 0.012f, 0.018f, 0.24f);
        vignette.raycastTarget = false;
        vignette.transform.SetSiblingIndex(1);

        var hero = EnsureImage(panelStart.transform, "TreadShredCommandBaseHero");
        var heroRect = hero.GetComponent<RectTransform>();
        heroRect.anchorMin = new Vector2(0.24f, 0.05f);
        heroRect.anchorMax = new Vector2(0.76f, 0.73f);
        heroRect.offsetMin = Vector2.zero;
        heroRect.offsetMax = Vector2.zero;
        heroRect.anchoredPosition = Vector2.zero;
        hero.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BasePath);
        hero.preserveAspect = true;
        hero.color = Color.white;
        hero.raycastTarget = false;
        hero.transform.SetSiblingIndex(2);

        var briefing = panelStart.transform.Find("TreadShredCommandBriefing");
        if (briefing == null)
        {
            briefing = new GameObject("TreadShredCommandBriefing", typeof(RectTransform)).transform;
            briefing.SetParent(panelStart.transform, false);
        }
        var briefingRect = briefing.GetComponent<RectTransform>();
        briefingRect.anchorMin = new Vector2(0.5f, 0.73f);
        briefingRect.anchorMax = new Vector2(0.5f, 0.73f);
        briefingRect.pivot = new Vector2(0.5f, 0.5f);
        briefingRect.anchoredPosition = new Vector2(0f, -22f);
        briefingRect.sizeDelta = new Vector2(720f, 34f);
        var briefingText = briefing.GetComponent<Text>() ?? briefing.gameObject.AddComponent<Text>();
        briefingText.text = "ARMORED COMBAT // LIVE FIRE RANGE";
        briefingText.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        briefingText.fontSize = 22;
        briefingText.fontStyle = FontStyle.Normal;
        briefingText.alignment = TextAnchor.MiddleCenter;
        briefingText.color = new Color(1f, 0.64f, 0.20f, 1f);
        briefingText.horizontalOverflow = HorizontalWrapMode.Overflow;
        briefingText.verticalOverflow = VerticalWrapMode.Overflow;
        briefingText.raycastTarget = false;
        briefing.SetSiblingIndex(3);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[START SCREEN] Added command-base hero and outdoor combat-range backdrop.");
    }

    public static void ApplyControlLabels()
    {
        var commandFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (commandFont == null)
            throw new System.InvalidOperationException("Missing command font at " + FontPath);

        ImportSprite(AdvancePath, 2048);
        ImportSprite(SettingsPath, 2048);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var panelStart = FindByName(scene.GetRootGameObjects(), "PanelStart");
        if (panelStart == null)
            throw new System.InvalidOperationException("Could not find PanelStart in " + ScenePath);

        var option = panelStart.transform.Find("Control/Option");
        var stats = panelStart.transform.Find("Control/Stats");
        var play = panelStart.transform.Find("Control/Play");
        if (option == null || stats == null || play == null)
            throw new System.InvalidOperationException("Could not find the start-screen command controls.");

        var advanceIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AdvancePath);
        var settingsIcon = AssetDatabase.LoadAssetAtPath<Sprite>(SettingsPath);
        ConfigureStartControl(option, settingsIcon, new Vector2(0.20f, 0.16f), 146f);
        ConfigureStartControl(stats, null, new Vector2(0.50f, 0.10f), 108f);
        ConfigureStartControl(play, advanceIcon, new Vector2(0.80f, 0.16f), 146f);
        HideLegacyControlText(play);

        EnsureControlLabel(option, "TreadShredFieldOpsLabel", "SETTINGS", string.Empty, 280f, commandFont);
        EnsureControlLabel(stats, "TreadShredWarRecordLabel", "WAR RECORD", "TOTAL SCORE // CONFIRMED KILLS // RANK", 440f, commandFont);
        EnsureControlLabel(play, "TreadShredDeployLabel", "START MISSION", string.Empty, 310f, commandFont);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[START SCREEN] Rebuilt SETTINGS, WAR RECORD, and START MISSION controls.");
    }

    private static void ConfigureStartControl(Transform control, Sprite sprite, Vector2 anchor, float size)
    {
        var rect = control.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        var image = control.GetComponent<Image>();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        EditorUtility.SetDirty(rect);
        if (image != null)
            EditorUtility.SetDirty(image);
    }

    private static void HideLegacyControlText(Transform control)
    {
        var legacyText = control.Find("Text");
        if (legacyText != null)
            legacyText.gameObject.SetActive(false);
    }

    private static void EnsureControlLabel(Transform control, string name, string title, string detail, float width, Font font)
    {
        var label = control.Find(name);
        if (label == null)
        {
            label = new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            label.SetParent(control, false);
        }

        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -12f);
        labelRect.sizeDelta = new Vector2(width, 68f);

        var background = label.GetComponent<Image>();
        background.color = new Color(0.01f, 0.015f, 0.02f, 0.88f);
        background.raycastTarget = false;

        var textTransform = label.Find("Text");
        if (textTransform == null)
        {
            textTransform = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline)).transform;
            textTransform.SetParent(label, false);
        }

        var textRect = textTransform.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        var text = textTransform.GetComponent<Text>();
        text.text = string.IsNullOrEmpty(detail) ? title : title + "\n" + detail;
        text.font = font;
        text.fontSize = 19;
        text.fontStyle = FontStyle.Normal;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 19;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = textTransform.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static void ImportSprite(string path, int maxSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = maxSize;
        importer.SaveAndReimport();
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
            child.SetParent(parent, false);
        }
        return child.GetComponent<Image>();
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static GameObject FindByName(GameObject[] roots, string name)
    {
        foreach (var root in roots)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                    return transform.gameObject;
            }
        }
        return null;
    }
}
#endif
