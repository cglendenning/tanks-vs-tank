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

    public static void Run()
    {
        var tankTexture = LoadTexture(TankTexturePath);
        var missileTexture = LoadTexture(MissileTexturePath);
        var baseIcon = LoadSprite(BaseIconPath);
        var advanceIcon = LoadSprite(AdvanceIconPath);
        var redeployIcon = LoadSprite(RedeployIconPath);
        ApplyTankMaterials(tankTexture);
        ApplyMissileMaterials(missileTexture);
        ApplyTeamTankMaterials(tankTexture);
        ApplyCombatUi(baseIcon, advanceIcon, redeployIcon);
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

    private static void ApplyTeamTankMaterials(Texture2D tankTexture)
    {
        var playerMaterial = CreateOrLoadTeamMaterial(
            PlayerTankMaterialPath, "Tread Shred Player Armor", tankTexture,
            new Color(0.52f, 1f, 0.55f, 1f));
        var enemyMaterial = CreateOrLoadTeamMaterial(
            EnemyTankMaterialPath, "Tread Shred Enemy Armor", tankTexture,
            new Color(0.78f, 0.54f, 1f, 1f));

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

        SetMaterialPresentation(material, texture, 0.74f, 0.44f);
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

    private static void ApplyCombatUi(Sprite baseIcon, Sprite advanceIcon, Sprite redeployIcon)
    {
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            ConfigureIconButton(FindByPath(canvas.transform, "PanelVictoryFailure/Victory/ButtonNext"), advanceIcon, 112f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelVictoryFailure/ButtonMenu"), baseIcon, 112f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelVictoryFailure/ButtonRetry"), redeployIcon, 112f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonMenu (1)"), baseIcon, 96f);
            ConfigureIconButton(FindByPath(canvas.transform, "PanelPause/ButtonRetry (1)"), redeployIcon, 96f);

            var nextMissionText = FindByPath(canvas.transform, "PanelVictoryFailure/Victory/ButtonNext/Text");
            if (nextMissionText != null)
                nextMissionText.gameObject.SetActive(false);

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
