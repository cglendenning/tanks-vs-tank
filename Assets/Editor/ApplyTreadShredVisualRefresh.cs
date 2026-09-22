#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyTreadShredVisualRefresh
{
    private const string TankTexturePath = "Assets/Art/ArmoredTankAtlas.png";
    private const string MissileTexturePath = "Assets/Art/BattlefieldMissileAtlas.png";
    private const string BaseIconPath = "Assets/Image/TreadShredBaseIcon.png";
    private const string AdvanceIconPath = "Assets/Image/TreadShredAdvanceIcon.png";
    private const string RedeployIconPath = "Assets/Image/TreadShredRedeployIcon.png";
    private const string CanvasPath = "Assets/Prefab/Canvas.prefab";
    private const string PlayerTankMaterialPath = "Assets/Art/TreadShredPlayerTank.mat";
    private const string EnemyTankMaterialPath = "Assets/Art/TreadShredEnemyTank.mat";
    private const string CommandFontPath = "Assets/Art/BlackOpsOne-Regular.ttf";

    public static void Run()
    {
        var tankTexture = LoadTexture(TankTexturePath);
        var missileTexture = LoadTexture(MissileTexturePath);
        var baseIcon = LoadSprite(BaseIconPath);
        var advanceIcon = LoadSprite(AdvanceIconPath);
        var redeployIcon = LoadSprite(RedeployIconPath);
        var commandFont = AssetDatabase.LoadAssetAtPath<Font>(CommandFontPath);
        if (commandFont == null)
            throw new FileNotFoundException("Missing command font", CommandFontPath);
        ApplyTankMaterials(tankTexture);
        ApplyMissileMaterials(missileTexture);
        ApplyGroundMaterials();
        ApplyTeamTankMaterials(tankTexture);
        ApplyCombatUi(baseIcon, advanceIcon, redeployIcon, commandFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VISUAL REFRESH] Applied armored tank, missile, and next-mission presentation.");
    }

    private static Texture2D LoadTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Sprite LoadSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ApplyTankMaterials(Texture2D texture)
    {
        foreach (var path in Directory.GetFiles("Assets/Art/Tank-enemy/Materials", "*.mat", SearchOption.AllDirectories))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\', '/'));
            if (material == null)
                continue;

            SetMaterialPresentation(material, texture, 0.72f, 0.42f);
        }
    }

    private static void ApplyMissileMaterials(Texture2D texture)
    {
        string[] paths =
        {
            "Assets/Art/Bullet/Materials/Bullet 1.mat",
            "Assets/Art/Bullet/Materials/Bullet 2.mat",
            "Assets/Art/Bullet/Materials/Bullet 3.mat",
            "Assets/Art/Bullet/Materials/Bullet 4.mat",
            "Assets/Art/Bullet/Materials/Bullet 5.mat",
            "Assets/Art/Bullet/Materials/Bullet 6.mat",
            "Assets/Art/Bullet/Materials/Bullet 7.mat",
            "Assets/Art/Bullet/Materials/Bullet.mat",
            "Assets/Art/Bullet/Materials/Bullet_2.mat"
        };

        foreach (var path in paths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            SetMaterialPresentation(material, texture, 0.86f, 0.5f);
        }
    }

    private static void ApplyGroundMaterials()
    {
        foreach (var path in Directory.GetFiles("Assets/Art", "Ground*.mat", SearchOption.AllDirectories))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\', '/'));
            if (material == null)
                continue;

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(0.76f, 0.79f, 0.82f, 1f));
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.12f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.24f);
            EditorUtility.SetDirty(material);
        }
    }

    private static void ApplyTeamTankMaterials(Texture2D tankTexture)
    {
        var playerMaterial = CreateOrLoadTeamMaterial(
            PlayerTankMaterialPath, "Tread Shred Player Armor", tankTexture,
            new Color(1.36f, 1.96f, 1.26f, 1f));
        var enemyMaterial = CreateOrLoadTeamMaterial(
            EnemyTankMaterialPath, "Tread Shred Enemy Armor", tankTexture,
            new Color(1.86f, 1.27f, 2.10f, 1f));

        ApplyTankMaterialToPrefab("Assets/Prefab/Player 1.prefab", playerMaterial);
        foreach (var path in Directory.GetFiles("Assets/Prefab/Emnemy", "*.prefab", SearchOption.AllDirectories))
            ApplyTankMaterialToPrefab(path.Replace('\\', '/'), enemyMaterial);
    }

    private static Material CreateOrLoadTeamMaterial(string path, string name, Texture2D texture, Color tint)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        SetMaterialPresentation(material, texture, 0.48f, 0.36f);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyTankMaterialToPrefab(string path, Material teamMaterial)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;
                var meshPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!meshPath.Contains("Assets/Art/Tank-enemy/"))
                    continue;

                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = teamMaterial;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetMaterialPresentation(Material material, Texture2D texture, float metallic, float smoothness)
    {
        if (texture != null && material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_SpecularHighlights"))
            material.SetFloat("_SpecularHighlights", 1f);
        EditorUtility.SetDirty(material);
    }

    private static void ApplyCombatUi(Sprite baseIcon, Sprite advanceIcon, Sprite redeployIcon, Font commandFont)
    {
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            var victoryRoot = FindByPath(canvas.transform, "PanelVictoryFailure/Victory");
            ConfigureCommandStrip(victoryRoot);

            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/ButtonRetry"),
                redeployIcon, "REPLAY", new Vector2(0.22f, 0.17f), 152f, commandFont);
            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/ButtonMenu"),
                baseIcon, "BASE", new Vector2(0.50f, 0.17f), 152f, commandFont);
            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/Victory/ButtonNext"),
                advanceIcon, "NEXT MISSION", new Vector2(0.78f, 0.17f), 152f, commandFont);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonMenu (1)"), baseIcon, 96f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonRetry (1)"), redeployIcon, 96f);

            PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvas);
        }
    }

    private static void ConfigureIconButton(Transform button, Sprite sprite, float size)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            EditorUtility.SetDirty(image);
        }

        var rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            EditorUtility.SetDirty(rect);
        }
    }

    private static void ConfigureCommandStrip(Transform victoryRoot)
    {
        if (victoryRoot == null)
            return;

        var strip = EnsureImage(victoryRoot, "CommandStrip");
        var stripRect = strip.GetComponent<RectTransform>();
        SetStretchRect(stripRect, new Vector2(0.055f, 0.025f), new Vector2(0.945f, 0.31f));
        strip.transform.SetSiblingIndex(0);
        var stripImage = strip.GetComponent<Image>();
        stripImage.color = new Color(0.015f, 0.035f, 0.055f, 0.94f);
        stripImage.raycastTarget = false;

        var outline = strip.GetComponent<Outline>() ?? strip.AddComponent<Outline>();
        outline.effectColor = new Color(0.20f, 0.72f, 0.78f, 0.48f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        ConfigureStripAccent(strip.transform, "TopRule", new Vector2(0f, 0.96f), new Vector2(1f, 1f),
            new Color(0.20f, 0.78f, 0.82f, 0.85f));
        ConfigureStripAccent(strip.transform, "BottomRule", new Vector2(0f, 0f), new Vector2(1f, 0.025f),
            new Color(0.95f, 0.44f, 0.10f, 0.78f));
        ConfigureStripAccent(strip.transform, "DividerLeft", new Vector2(0.333f, 0.17f), new Vector2(0.335f, 0.83f),
            new Color(0.20f, 0.72f, 0.78f, 0.24f));
        ConfigureStripAccent(strip.transform, "DividerRight", new Vector2(0.665f, 0.17f), new Vector2(0.667f, 0.83f),
            new Color(0.20f, 0.72f, 0.78f, 0.24f));
    }

    private static void ConfigureCommandButton(Transform button, Sprite sprite, string label, Vector2 anchor, float size, Font commandFont)
    {
        if (button == null)
            return;

        ConfigureIconButton(button, sprite, size);
        var rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            EditorUtility.SetDirty(rect);
        }

        var legacyText = button.Find("Text");
        if (legacyText != null)
            legacyText.gameObject.SetActive(false);

        var plate = EnsureImage(button, "LabelPlate");
        var plateRect = plate.GetComponent<RectTransform>();
        plateRect.anchorMin = new Vector2(0f, 0f);
        plateRect.anchorMax = new Vector2(1f, 0f);
        plateRect.pivot = new Vector2(0.5f, 1f);
        plateRect.anchoredPosition = new Vector2(0f, -4f);
        plateRect.sizeDelta = new Vector2(-10f, 38f);
        plate.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.88f);
        plate.GetComponent<Image>().raycastTarget = false;
        var plateOutline = plate.GetComponent<Outline>() ?? plate.AddComponent<Outline>();
        plateOutline.effectColor = new Color(0.93f, 0.42f, 0.10f, 0.62f);
        plateOutline.effectDistance = new Vector2(1f, -1f);
        plateOutline.useGraphicAlpha = true;
        plate.transform.SetSiblingIndex(0);

        var text = EnsureText(button, "CommandLabel");
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -4f);
        textRect.sizeDelta = new Vector2(-14f, 38f);
        text.text = label;
        text.font = commandFont;
        text.fontSize = label == "NEXT MISSION" ? 20 : 24;
        text.fontStyle = FontStyle.Normal;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = text.fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        var textOutline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
        textOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        textOutline.effectDistance = new Vector2(2f, -2f);
        textOutline.useGraphicAlpha = true;
        EditorUtility.SetDirty(text);
    }

    private static GameObject EnsureImage(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text EnsureText(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<Text>();

        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Text>();
    }

    private static void ConfigureStripAccent(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var accent = EnsureImage(parent, name);
        var rect = accent.GetComponent<RectTransform>();
        SetStretchRect(rect, anchorMin, anchorMax);
        var image = accent.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void SetStretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static Transform FindByPath(Transform root, string path)
    {
        var current = root;
        foreach (var part in path.Split('/'))
        {
            current = current.Find(part);
            if (current == null)
                return null;
        }
        return current;
    }
}
#endif
