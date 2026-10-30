#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off migration: removes components whose scripts were deleted with the
// ad SDK so scenes and prefabs load without missing-script warnings.
public static class TreadShredRemoveMissingScripts
{
    public static void Run()
    {
        var total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var removed = root.GetComponentsInChildren<Transform>(true)
                .Sum(t => GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject));
            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[MissingScripts] " + path + ": removed " + removed);
                total += removed;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (var path in Directory.GetFiles("Assets/Scence", "*.unity"))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var removed = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject));
            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[MissingScripts] " + path + ": removed " + removed);
                total += removed;
            }
        }

        Debug.Log("[MissingScripts] total removed " + total);
    }
}
#endif
