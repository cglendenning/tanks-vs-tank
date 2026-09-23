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
