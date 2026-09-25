#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TreadShredScreenshotBuild
{
    [MenuItem("Tread Shred/Prepare project visuals")]
    public static void PrepareProjectVisualsFromMenu()
    {
        PrepareProjectVisuals();
    }

    public static void PrepareProjectVisuals()
    {
        ApplyTreadShredBranding.Run();
        ApplyTreadShredVisualRefresh.Run();
        ApplyTreadShredStartScreen.Run();
        ApplyTreadShredStartScreen.ApplyControlLabels();
    }

    public static void BuildMacScreenshotPlayer()
    {
        var output = Path.GetFullPath("/private/tmp/tread-shred-screenshot-player");
        if (Directory.Exists(output))
            FileUtil.DeleteFileOrDirectory(output);

        Directory.CreateDirectory(output);
        var scenes = new[]
        {
            "Assets/Scence/Start.unity",
            "Assets/Scence/Lv1.unity",
            "Assets/Scence/Lv2.unity",
            "Assets/Scence/Lv3.unity",
            "Assets/Scence/Lv4.unity",
            "Assets/Scence/Lv5.unity",
            "Assets/Scence/Lv6.unity"
        };

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(output, "Tread Shred.app"),
            target = BuildTarget.StandaloneOSX,
            // Store-capture player: never include Unity's Development Build watermark.
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception(report.summary.ToString());

        Debug.Log("Tread Shred screenshot player created at " + output);
    }
}
#endif
