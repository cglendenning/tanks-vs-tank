#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class TankBuildAutomation
{
    private const string ProductName = "Tread Shred";
    // The legacy identifier is already claimed by another Apple team. Keep a
    // team-owned identifier so automatic Ad Hoc provisioning can work.
    private const string BundleIdentifier = "com.cglendenning.tanksvstank";
    private const string IosOutput = "Builds/iOSDevice";
    private const string AndroidApkOutput = "Builds/Android/TreadShred.apk";
    private const string AndroidBundleOutput = "Builds/Android/TreadShred.aab";

    [MenuItem("Tread Shred/Build/iOS device project")]
    public static void BuildIosDeviceProject() => BuildIos(false);

    [MenuItem("Tread Shred/Build/Android APK")]
    public static void BuildAndroidApk() => BuildAndroid(false);

    public static void BuildIosFromCommandLine() => BuildIos(false);
    public static void BuildAndroidFromCommandLine() => BuildAndroid(false);

    public static void BuildAndroidBundleFromCommandLine()
    {
        BuildAndroid(true);
    }

    private static void BuildIos(bool simulator)
    {
        PreparePlayerSettings();
        var output = Path.GetFullPath(IosOutput);
        if (Directory.Exists(output))
            FileUtil.DeleteFileOrDirectory(output);
        Directory.CreateDirectory(output);

        PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = ScenePaths(),
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        });
        AssertBuildSucceeded(report, "iOS");
        PatchIosInfoPlist(output);
        PatchIosEntitlements(output);
        PatchIosPodfile(output);
        Debug.Log("Tread Shred iOS export created at " + output);
    }

    private static void BuildAndroid(bool bundle)
    {
        PreparePlayerSettings();
        var output = Path.GetFullPath(bundle ? AndroidBundleOutput : AndroidApkOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = bundle;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = ScenePaths(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        AssertBuildSucceeded(report, "Android");
        Debug.Log("Tread Shred Android build created at " + output);
    }

    private static void PreparePlayerSettings()
    {
        EnsureAdConfigurationAsset();
        var scenes = ScenePaths();
        EditorBuildSettings.scenes = scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();

        PlayerSettings.companyName = "TGT Studios";
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = "1.5.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleIdentifier);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdentifier);
        PlayerSettings.Android.bundleVersionCode = 2;
        // Unity 6 no longer supports API 23; keep the project aligned with the
        // current Android player minimum and avoid the legacy project's setting.
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // The checked-in project contained an obsolete keystore path from a
        // different machine. Let Unity use its debug signing configuration for
        // local validation; release signing is supplied by the release script.
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.requiresFullScreen = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android, 1);
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
    }

    private static string[] ScenePaths()
    {
        var sceneDirectory = Path.Combine(Application.dataPath, "Scence");
        var paths = Directory.GetFiles(sceneDirectory, "*.unity")
            .Select(path => path.Replace("\\", "/"))
            .Select(path => "Assets" + path.Substring(Application.dataPath.Length))
            .OrderBy(path => path == "Assets/Scence/Start.unity" ? 0 : 1)
            .ThenBy(path => ParseLevel(path))
            .ToArray();
        if (paths.Length == 0)
            throw new BuildFailedException("No scenes were found in Assets/Scence.");
        return paths;
    }

    private static int ParseLevel(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.StartsWith("Lv", StringComparison.OrdinalIgnoreCase) && int.TryParse(name.Substring(2), out var level)
            ? level
            : int.MaxValue;
    }

    private static void EnsureAdConfigurationAsset()
    {
        const string directory = "Assets/Resources";
        const string path = directory + "/TankAdConfiguration.asset";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
        if (AssetDatabase.LoadAssetAtPath<TankAdConfiguration>(path) == null)
        {
            var configuration = ScriptableObject.CreateInstance<TankAdConfiguration>();
            AssetDatabase.CreateAsset(configuration, path);
            AssetDatabase.SaveAssets();
        }
    }

    private static void PatchIosInfoPlist(string buildPath)
    {
        var plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
            return;

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetString("NSUserTrackingUsageDescription", "This identifier will be used to deliver personalized ads to you.");
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        File.WriteAllText(plistPath, plist.WriteToString());
    }

    private static void PatchIosPodfile(string buildPath)
    {
        var podfilePath = Path.Combine(buildPath, "Podfile");
        if (!File.Exists(podfilePath))
            return;

        var podfile = File.ReadAllText(podfilePath);
        const string marker = "# Tread Shred deployment target normalization";
        if (podfile.Contains(marker))
            return;

        podfile += "\n" + marker + "\n";
        podfile += "post_install do |installer|\n";
        podfile += "  installer.pods_project.targets.each do |target|\n";
        podfile += "    target.build_configurations.each do |config|\n";
        podfile += "      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '15.0'\n";
        podfile += "    end\n";
        podfile += "  end\n";
        podfile += "end\n";
        File.WriteAllText(podfilePath, podfile);
    }

    private static void PatchIosEntitlements(string buildPath)
    {
        // The legacy project enabled Game Center without an App ID capability.
        // Remove that stale entitlement so Ad Hoc provisioning remains valid.
        var path = Path.Combine(buildPath, "TanksVSTank.entitlements");
        if (!File.Exists(path))
            return;

        File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
            "<plist version=\"1.0\"><dict/></plist>\n");
    }

    private static void AssertBuildSucceeded(BuildReport report, string platform)
    {
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(platform + " build failed with " + report.summary.totalErrors + " errors.");
    }
}
#endif
