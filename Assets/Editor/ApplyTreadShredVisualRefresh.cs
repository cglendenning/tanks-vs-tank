#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyTreadShredVisualRefresh
{
    private const string TankTexturePath = "Assets/Art/ArmoredTankAtlas.png";
    private const string MissileTexturePath = "Assets/Art/BattlefieldMissileAtlas.png";
    private const string CanvasPath = "Assets/Prefab/Canvas.prefab";

    public static void Run()
    {
        var tankTexture = LoadTexture(TankTexturePath);
        var missileTexture = LoadTexture(MissileTexturePath);
        ApplyTankMaterials(tankTexture);
        ApplyMissileMaterials(missileTexture);
        ApplyNextMissionLayout();
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

    private static void ApplyNextMissionLayout()
    {
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            var nextMission = FindByPath(canvas.transform, "PanelVictoryFailure/Victory/ButtonNext/Text");
            if (nextMission == null)
                return;

            var text = nextMission.GetComponent<Text>();
            if (text == null)
                return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(230f, 56f);
            rect.anchoredPosition = new Vector2(-102f, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            text.text = "NEXT MISSION";
            text.fontSize = 30;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 30;
            text.alignment = TextAnchor.MiddleRight;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvas);
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
