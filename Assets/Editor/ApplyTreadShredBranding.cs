#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ApplyTreadShredBranding
{
    private const string ProductName = "Tread Shred";
    private const string FontPath = "Assets/Art/UTM Bebas.ttf";

    public static void Run()
    {
        var commandFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (commandFont == null)
            throw new InvalidOperationException("Missing command font at " + FontPath);

        PlayerSettings.productName = ProductName;
        ApplyScenes(commandFont);
        ApplyPrefab(commandFont, "Assets/Prefab/Canvas.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("[BRANDING] Applied Tread Shred copy and command font.");
    }

    private static void ApplyScenes(Font commandFont)
    {
        foreach (var scenePath in Directory.GetFiles("Assets/Scence", "*.unity"))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = false;
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (!text.gameObject.scene.IsValid() || text.gameObject.scene != scene)
                    continue;
                changed |= ApplyText(text, commandFont);
            }
            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void ApplyPrefab(Font commandFont, string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                ApplyText(text, commandFont);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ApplyText(Text text, Font commandFont)
    {
        var before = text.text;
        var path = GetPath(text.transform);
        var name = text.gameObject.name;
        var replacement = Replacement(path, name, before);

        text.font = commandFont;
        text.fontStyle = FontStyle.Bold;
        text.supportRichText = false;
        if (replacement != null)
            text.text = replacement;

        EditorUtility.SetDirty(text);
        if (PrefabUtility.IsPartOfPrefabInstance(text))
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);

        return before != text.text;
    }

    private static string Replacement(string path, string name, string current)
    {
        if (name == "TextTank" && path.Contains("PanelStart"))
            return "TREAD SHRED";
        if (name == "TextTank (1)" && path.Contains("PanelStart"))
            return "NO MERCY. NO REVERSE.";
        if (path.Contains("PanelStart/Control/Play/"))
            return "DEPLOY";
        if (path.Contains("PanelStart/Control/Option/"))
            return "FIELD OPS";
        if (path.Contains("PanelStart/Control/Stats/"))
            return "WAR RECORD";
        if (path.Contains("PanelStart/OptionPanel/Exit"))
            return "ABORT MISSION";
        if (path.Contains("PanelStart/OptionPanel/Camera/fix"))
            return "CAMERA: LOCKED";
        if (path.Contains("PanelStart/OptionPanel/Camera/fllow"))
            return "CAMERA: TRACKING";
        if (path.Contains("PanelStart/PanelStatics/Score"))
            return "TOTAL SCORE // 0";
        if (path.Contains("PanelStart/PanelStatics/Kill"))
            return "CONFIRMED KILLS // 0";
        if (path.Contains("PanelStart/PanelStatics/Rank"))
            return "RANK // PRIVATE";
        if (path.Contains("PanelStart/Button/T"))
            return "PURCHASED";

        if (path.Contains("PanelMenu/TextTank (2)"))
            return "MISSION SELECT";
        if (path.Contains("PanelMenu/TextTank (3)"))
            return string.Empty;
        if (path.Contains("PanelMenu/Play/"))
            return "DEPLOY";
        if (path.Contains("PanelMenu/Hightscore"))
            return "BEST SCORE // 0";
        if (path.Contains("PanelMenu/Level"))
            return "MISSION 0";
        if (path.Contains("PanelMenu/Rank"))
            return "MEDAL: UNRANKED";
        if (path.Contains("PanelMenu/Scroll/Panel/Button") && name == "Text")
            return "MISSION 1";

        if (path.Contains("PanelIntro/Button/"))
            return "ARM UP";
        if (path.Contains("PanelIntro/Image/Text1"))
            return "DRAG TO DRIVE YOUR TANK";
        if (path.Contains("PanelIntro/Image/Text2"))
            return "TAP TARGET TO FIRE";
        if (path.Contains("PanelPause/Text"))
            return "PAUSED // HOLD";
        if (path.Contains("PanelPause/ButtonRetry"))
            return "REDEPLOY";
        if (path.Contains("PanelPause/ButtonMenu"))
            return "COMMAND";
        if (path.Contains("PanelCount/Level"))
            return "MISSION: 1";
        if (path.Contains("PanelCount/LetStart"))
            return "ENGAGE";
        if (path.EndsWith("Canvas/Panel/Text", StringComparison.Ordinal) || path.EndsWith("Canvas/FadeIg/Text", StringComparison.Ordinal))
            return "STAGING...";

        if (path.Contains("PanelVictoryFailure/ButtonRetry/"))
            return "REDEPLOY";
        if (path.Contains("PanelVictoryFailure/ButtonMenu/"))
            return "COMMAND";
        if (path.Contains("PanelVictoryFailure/Victory/ButtonNext/"))
            return "NEXT MISSION";
        if (path.Contains("PanelVictoryFailure/Victory/A WESOME"))
            return "TARGETS CLEARED";
        if (name == "TOTAL")
            return "TOTAL SCORE //";
        if (name == "KILL RATING")
            return "KILL SCORE //";
        if (name == "HEALTH RATING")
            return "ARMOR SCORE //";
        if (name == "ACCURACY RATING")
            return "ACCURACY //";
        if (name == "LEVEL SOCRE")
            return "MISSION SCORE //";
        if (name == "TOTAL FAILURE")
            return "MISSION FAILED";
        if (name == "Kill Score")
            return "SCORE // 0";

        if (current == "FIGHT") return "ENGAGE";
        if (current == "PAUSED") return "PAUSED // HOLD";
        if (current == "FAILED") return "MISSION FAILED";
        if (current == "Good Job") return "TARGETS CLEARED";
        if (current == "EXIT") return "ABORT MISSION";
        if (current == "STATS") return "WAR RECORD";
        if (current == "OPTIONS") return "FIELD OPS";
        if (current == "PLAY") return "DEPLOY";
        if (current == "NEXT") return "NEXT MISSION";
        if (current == "RETRY") return "REDEPLOY";
        if (current == "MENU") return "COMMAND";
        if (current == "LOADING...") return "STAGING...";
        if (current.StartsWith("LEVEL: ", StringComparison.Ordinal)) return current.Replace("LEVEL: ", "MISSION: ");
        if (current.StartsWith("LEVEL ", StringComparison.Ordinal)) return current.Replace("LEVEL ", "MISSION ");
        if (current.StartsWith("HIGHT SCORE: ", StringComparison.Ordinal)) return current.Replace("HIGHT SCORE: ", "BEST SCORE // ");
        if (current.StartsWith("RANKING: ", StringComparison.Ordinal)) return current.Replace("RANKING: ", "MEDAL: ");
        if (current.StartsWith("SCORE: ", StringComparison.Ordinal)) return current.Replace("SCORE: ", "SCORE // ");
        if (current.StartsWith("SCORE : ", StringComparison.Ordinal)) return current.Replace("SCORE : ", "SCORE // ");
        if (current.StartsWith("KILL: ", StringComparison.Ordinal)) return current.Replace("KILL: ", "KILLS // ");
        if (current.StartsWith("RANK: ", StringComparison.Ordinal)) return current.Replace("RANK: ", "RANK // ");
        if (current == "SLIVER") return "SILVER";
        return current;
    }

    private static string GetPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
#endif
