#if UNITY_EDITOR
using System.Linq;
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
    private const string LivesIconPath = "Assets/Image/TreadShredLivesIcon.png";
    private const string WallTexturePath = "Assets/Art/TreadShredFortificationWallTexture.png";
    private const string WallTorchPath = "Assets/Art/TreadShredWallTorch.png";
    private const string MissionBoardPath = "Assets/Image/TreadShredMissionBoard.png";
    private const string CanvasPath = "Assets/Prefab/Canvas.prefab";
    private const string PanelMenuPath = "Assets/Prefab/PanelMenu.prefab";
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
        var livesIcon = LoadSprite(LivesIconPath);
        var wallTexture = LoadTexture(WallTexturePath);
        var wallTorch = LoadSprite(WallTorchPath);
        var missionBoard = LoadSprite(MissionBoardPath);
        var commandFont = AssetDatabase.LoadAssetAtPath<Font>(CommandFontPath);
        if (commandFont == null)
            throw new FileNotFoundException("Missing command font", CommandFontPath);
        ApplyTankMaterials(tankTexture);
        ApplyMissileMaterials(missileTexture);
        ApplyGroundMaterials();
        ApplyWallMaterials(wallTexture);
        ApplyTeamTankMaterials(tankTexture);
        ApplyArenaTorches(wallTorch);
        ApplyMissionSelectUi(missionBoard, commandFont);
        ApplyCombatUi(baseIcon, advanceIcon, redeployIcon, livesIcon, commandFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VISUAL REFRESH] Applied armored tank, missile, mission board, fortified walls, and animated torches.");
    }

    public static void ApplyTorchesOnly()
    {
        var wallTorch = LoadSprite(WallTorchPath);
        ApplyArenaTorches(wallTorch);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VISUAL REFRESH] Re-attached animated torches to the arena corner posts.");
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

    private static void ApplyWallMaterials(Texture2D texture)
    {
        foreach (var path in Directory.GetFiles("Assets/Art/Envi1/Materials", "Wall*.mat", SearchOption.AllDirectories))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\', '/'));
            if (material == null)
                continue;

            SetMaterialPresentation(material, texture, 0.72f, 0.34f);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(0.78f, 0.86f, 0.90f, 1f));
            if (material.HasProperty("_EmissionMap") && texture != null)
                material.SetTexture("_EmissionMap", texture);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.02f, 0.09f, 0.12f, 1f));
            }
            EditorUtility.SetDirty(material);
        }
    }

    private static void ApplyArenaTorches(Sprite torchSprite)
    {
        if (torchSprite == null)
            return;

        foreach (var path in Directory.GetFiles("Assets/Prefab/Art", "Enviroment*.prefab", SearchOption.TopDirectoryOnly))
        {
            var prefabPath = path.Replace('\\', '/');
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.GetComponent<TreadShredTorchFlicker>() == null)
                    .ToArray();
                if (renderers.Length == 0)
                    continue;

                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                var cornerPosts = renderers
                    .Where(renderer => renderer.name == "Cot" || renderer.name.StartsWith("Cot ("))
                    .OrderBy(renderer => renderer.bounds.center.x)
                    .ThenBy(renderer => renderer.bounds.center.z)
                    .Take(4)
                    .ToArray();
                if (cornerPosts.Length < 4)
                    continue;

                var torchWidth = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.06f, 1.8f, 2.45f);
                var torchScale = torchWidth / 11.45f;

                for (var i = 0; i < cornerPosts.Length; i++)
                {
                    var childName = "TreadShredTorch_" + (i + 1);
                    var existing = root.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(transform => transform.name == childName);
                    var torch = existing == null ? new GameObject(childName) : existing.gameObject;
                    if (existing == null)
                        torch.transform.SetParent(root.transform, false);

                    var post = cornerPosts[i];
                    var postCenter = post.bounds.center;
                    var inward = new Vector3(
                        postCenter.x < bounds.center.x ? 0.45f : -0.45f,
                        0f,
                        postCenter.z < bounds.center.z ? 0.45f : -0.45f);
                    var attachPoint = postCenter + inward;
                    attachPoint.y = post.bounds.max.y + 0.16f;
                    torch.transform.SetParent(post.transform, true);
                    torch.transform.position = attachPoint;
                    torch.transform.localScale = Vector3.one * torchScale;
                    torch.transform.rotation = Quaternion.Euler(90f, 0f, postCenter.x < bounds.center.x ? 0f : 180f);

                    var spriteRenderer = torch.GetComponent<SpriteRenderer>();
                    if (spriteRenderer == null)
                        spriteRenderer = torch.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = torchSprite;
                    spriteRenderer.color = Color.white;
                    spriteRenderer.sortingOrder = 50;
                    spriteRenderer.flipX = postCenter.x >= bounds.center.x;
                    var flicker = torch.GetComponent<TreadShredTorchFlicker>();
                    if (flicker == null)
                        flicker = torch.AddComponent<TreadShredTorchFlicker>();
                    var torchLight = torch.GetComponent<Light>();
                    if (torchLight == null)
                        torchLight = torch.AddComponent<Light>();
                    torchLight.type = LightType.Point;
                    torchLight.color = new Color(1f, 0.30f, 0.06f, 1f);
                    torchLight.range = 4.5f;
                    torchLight.intensity = 0.72f;
                    torchLight.shadows = LightShadows.None;
                    EditorUtility.SetDirty(torch);
                    EditorUtility.SetDirty(spriteRenderer);
                    EditorUtility.SetDirty(flicker);
                    EditorUtility.SetDirty(torchLight);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ApplyTeamTankMaterials(Texture2D tankTexture)
    {
        var playerMaterial = CreateOrLoadTeamMaterial(
            PlayerTankMaterialPath, "Tread Shred Player Armor", tankTexture,
            new Color(1.55f, 2.18f, 1.35f, 1f));
        var enemyMaterial = CreateOrLoadTeamMaterial(
            EnemyTankMaterialPath, "Tread Shred Enemy Armor", tankTexture,
            new Color(2.05f, 1.34f, 2.30f, 1f));

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

        SetMaterialPresentation(material, texture, 0.32f, 0.30f);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        if (material.HasProperty("_EmissionMap") && texture != null)
            material.SetTexture("_EmissionMap", texture);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            var emission = tint.r > tint.b
                ? new Color(0.10f, 0.20f, 0.06f, 1f)
                : new Color(0.18f, 0.06f, 0.22f, 1f);
            material.SetColor("_EmissionColor", emission);
        }
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

    private static void ApplyCombatUi(Sprite baseIcon, Sprite advanceIcon, Sprite redeployIcon, Sprite livesIcon, Font commandFont)
    {
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            var victoryRoot = FindByPath(canvas.transform, "PanelVictoryFailure/Victory");
            if (victoryRoot != null)
                SetStretchRect(victoryRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            ConfigureCommandStrip(victoryRoot);

            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/ButtonRetry"),
                redeployIcon, "REPLAY", new Vector2(0.22f, 0.20f), 152f, commandFont);
            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/ButtonMenu"),
                baseIcon, "BASE", new Vector2(0.50f, 0.20f), 152f, commandFont);
            ConfigureCommandButton(
                FindByPath(canvas.transform, "PanelVictoryFailure/Victory/ButtonNext"),
                advanceIcon, "NEXT MISSION", new Vector2(0.78f, 0.20f), 152f, commandFont);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonMenu (1)"), baseIcon, 96f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonRetry (1)"), redeployIcon, 96f);
            ApplyLivesIndicators(canvas.transform, livesIcon);
            HideLegacyMedalBackdrop(canvas.transform);

            PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvas);
        }
    }

    private static void ApplyMissionSelectUi(Sprite boardSprite, Font commandFont)
    {
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            var menu = canvas.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "PanelMenu");
            if (menu == null)
                menu = canvas.transform;
            if (menu == null)
                return;

            var board = EnsureImage(menu, "MissionBoardBackdrop");
            var boardRect = board.GetComponent<RectTransform>();
            SetStretchRect(boardRect, new Vector2(0.115f, 0.035f), new Vector2(0.965f, 0.975f));
            board.transform.SetSiblingIndex(0);
            var boardImage = board.GetComponent<Image>();
            boardImage.sprite = boardSprite;
            boardImage.type = Image.Type.Simple;
            boardImage.preserveAspect = false;
            boardImage.color = Color.white;
            boardImage.raycastTarget = false;

            var header = EnsureImage(menu, "MissionBoardHeader");
            var headerRect = header.GetComponent<RectTransform>();
            SetStretchRect(headerRect, new Vector2(0.155f, 0.825f), new Vector2(0.925f, 0.965f));
            header.transform.SetSiblingIndex(1);
            var headerImage = header.GetComponent<Image>();
            headerImage.color = new Color(0.01f, 0.025f, 0.04f, 0.88f);
            headerImage.raycastTarget = false;
            var headerOutline = header.GetComponent<Outline>() ?? header.AddComponent<Outline>();
            headerOutline.effectColor = new Color(0.20f, 0.75f, 0.82f, 0.62f);
            headerOutline.effectDistance = new Vector2(2f, -2f);
            headerOutline.useGraphicAlpha = true;
            ConfigureStripAccent(header.transform, "CyanRule", new Vector2(0f, 0f), new Vector2(0.72f, 0.025f),
                new Color(0.20f, 0.78f, 0.85f, 0.90f));
            ConfigureStripAccent(header.transform, "OrangeRule", new Vector2(0.72f, 0f), new Vector2(1f, 0.025f),
                new Color(0.96f, 0.43f, 0.10f, 0.90f));

            ConfigureMissionHeaderText(menu, "TextTank (2)", "MISSION SELECT // OPERATIONS", commandFont,
                new Vector2(0.175f, 0.85f), new Vector2(0.90f, 0.955f), 34, TextAnchor.MiddleLeft);
            ConfigureMissionHeaderText(menu, "TextTank (3)", "SELECT AN OPERATION // 01—25", commandFont,
                new Vector2(0.175f, 0.80f), new Vector2(0.90f, 0.855f), 15, TextAnchor.MiddleLeft);

            var scroll = FindByPath(menu, "Scroll");
            if (scroll != null)
            {
                var scrollRect = scroll.GetComponent<RectTransform>();
                SetStretchRect(scrollRect, new Vector2(0.165f, 0.155f), new Vector2(0.915f, 0.805f));
                var scrollImage = scroll.GetComponent<Image>();
                if (scrollImage != null)
                {
                    scrollImage.color = new Color(0.01f, 0.025f, 0.04f, 0.32f);
                    scrollImage.raycastTarget = false;
                }
            }

            var content = FindByPath(menu, "Scroll/Panel");
            if (content != null)
            {
                var contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 13f * 58f + 16f);

                var layoutGroups = content.GetComponents<LayoutGroup>();
                foreach (var layout in layoutGroups)
                    layout.enabled = false;

                var missionIndex = 0;
                foreach (Transform child in content)
                {
                    var button = child.GetComponent<Button>();
                    var childRect = child.GetComponent<RectTransform>();
                    if (button == null || childRect == null)
                        continue;

                    var column = missionIndex % 2;
                    var row = missionIndex / 2;
                    childRect.anchorMin = new Vector2(0.5f, 1f);
                    childRect.anchorMax = new Vector2(0.5f, 1f);
                    childRect.pivot = new Vector2(0.5f, 1f);
                    childRect.sizeDelta = new Vector2(350f, 48f);
                    childRect.anchoredPosition = new Vector2(column == 0 ? -190f : 190f, -8f - row * 58f);

                    var buttonImage = child.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = new Color(0.025f, 0.055f, 0.075f, 0.97f);
                        buttonImage.raycastTarget = true;
                        var outline = child.GetComponent<Outline>() ?? child.gameObject.AddComponent<Outline>();
                        outline.effectColor = new Color(0.14f, 0.52f, 0.60f, 0.56f);
                        outline.effectDistance = new Vector2(1.5f, -1.5f);
                        outline.useGraphicAlpha = true;
                    }

                    if (child.childCount > 2)
                    {
                        var label = child.GetChild(2).GetComponent<Text>();
                        if (label != null)
                        {
                            var labelRect = label.GetComponent<RectTransform>();
                            labelRect.anchorMin = new Vector2(0.06f, 0f);
                            labelRect.anchorMax = new Vector2(0.80f, 1f);
                            labelRect.offsetMin = Vector2.zero;
                            labelRect.offsetMax = Vector2.zero;
                            label.alignment = TextAnchor.MiddleLeft;
                            label.font = commandFont;
                            label.fontSize = 22;
                            label.resizeTextForBestFit = true;
                            label.resizeTextMinSize = 14;
                            label.resizeTextMaxSize = 22;
                            label.color = Color.white;
                            label.horizontalOverflow = HorizontalWrapMode.Overflow;
                            label.verticalOverflow = VerticalWrapMode.Truncate;
                            var textOutline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
                            textOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                            textOutline.effectDistance = new Vector2(1.5f, -1.5f);
                            textOutline.useGraphicAlpha = true;
                            EditorUtility.SetDirty(label);
                        }
                    }

                    if (child.childCount > 3)
                    {
                        var statusRect = child.GetChild(3).GetComponent<RectTransform>();
                        statusRect.anchorMin = new Vector2(0.80f, 0.12f);
                        statusRect.anchorMax = new Vector2(0.96f, 0.88f);
                        statusRect.offsetMin = Vector2.zero;
                        statusRect.offsetMax = Vector2.zero;
                        statusRect.localScale = Vector3.one * 0.72f;
                        EditorUtility.SetDirty(statusRect);
                    }

                    EditorUtility.SetDirty(childRect);
                    EditorUtility.SetDirty(button);
                    missionIndex++;
                }
            }

            ConfigureMissionDeployButton(FindByPath(menu, "Play"), commandFont);
            ConfigureMissionInfoText(FindByPath(menu, "Level"), commandFont, "MISSION: 01", new Vector2(0.175f, 0.075f), new Vector2(0.40f, 0.13f));
            ConfigureMissionInfoText(FindByPath(menu, "Hightscore"), commandFont, "BEST SCORE // 0", new Vector2(0.40f, 0.075f), new Vector2(0.67f, 0.13f));
            ConfigureMissionInfoText(FindByPath(menu, "Rank"), commandFont, "MEDAL: UNRANKED", new Vector2(0.67f, 0.075f), new Vector2(0.88f, 0.13f));

            foreach (var button in menu.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.ToLowerInvariant().Contains("back"))
                    continue;
                var rect = button.GetComponent<RectTransform>();
                if (rect == null)
                    continue;
                rect.anchorMin = rect.anchorMax = new Vector2(0.055f, 0.875f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(72f, 72f);
                EditorUtility.SetDirty(rect);
            }

            PrefabUtility.SaveAsPrefabAsset(canvas, PanelMenuPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvas);
        }
    }

    private static void ConfigureMissionHeaderText(Transform menu, string path, string value, Font font, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAnchor alignment)
    {
        var text = FindByPath(menu, path);
        if (text == null)
            return;
        var label = text.GetComponent<Text>();
        var rect = text.GetComponent<RectTransform>();
        if (label == null || rect == null)
            return;
        SetStretchRect(rect, anchorMin, anchorMax);
        label.text = value;
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(10, fontSize - 10);
        label.resizeTextMaxSize = fontSize;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        var outline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        EditorUtility.SetDirty(label);
    }

    private static void ConfigureMissionInfoText(Transform textTransform, Font font, string value, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (textTransform == null)
            return;
        var label = textTransform.GetComponent<Text>();
        var rect = textTransform.GetComponent<RectTransform>();
        if (label == null || rect == null)
            return;
        SetStretchRect(rect, anchorMin, anchorMax);
        label.text = value;
        label.font = font;
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 15;
        label.color = new Color(0.78f, 0.90f, 0.92f, 1f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        EditorUtility.SetDirty(label);
    }

    private static void ConfigureMissionDeployButton(Transform button, Font commandFont)
    {
        if (button == null)
            return;
        var rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.90f, 0.085f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(132f, 58f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            EditorUtility.SetDirty(rect);
        }
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.92f, 0.30f, 0.08f, 1f);
            image.raycastTarget = true;
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.62f, 0.18f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }
        var legacyText = button.Find("Text");
        if (legacyText != null)
        {
            var label = legacyText.GetComponent<Text>();
            if (label != null)
            {
                label.text = "DEPLOY";
                label.font = commandFont;
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = 22;
                label.raycastTarget = false;
                EditorUtility.SetDirty(label);
            }
        }
    }

    private static void ApplyLivesIndicators(Transform canvas, Sprite livesIcon)
    {
        foreach (var transform in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name != "Heart")
                continue;

            var images = transform.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (livesIcon == null)
                    continue;

                image.sprite = livesIcon;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
                EditorUtility.SetDirty(image);
            }
        }
    }

    private static void HideLegacyMedalBackdrop(Transform canvas)
    {
        var backdrop = FindByPath(canvas, "PanelVictoryFailure/Huanchuong/Image");
        var image = backdrop == null ? null : backdrop.GetComponent<Image>();
        if (image == null)
            return;

        image.enabled = false;
        EditorUtility.SetDirty(image);
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
        SetStretchRect(stripRect, new Vector2(0.055f, 0.025f), new Vector2(0.945f, 0.35f));
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
        var isNextMission = label == "NEXT MISSION";
        plateRect.sizeDelta = new Vector2(isNextMission ? 54f : -10f, 38f);
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
        textRect.sizeDelta = new Vector2(isNextMission ? 46f : -14f, 38f);
        text.text = label;
        text.font = commandFont;
        text.fontSize = isNextMission ? 20 : 24;
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
