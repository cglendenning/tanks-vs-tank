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
    // These are the identifiers already registered in the stores and AdMob.
    private const string IosBundleIdentifier = "com.tgts.tanksvstank";
    private const string AndroidBundleIdentifier = "com.tgts.tankkvstank";
    private const string IosOutput = "Builds/iOSDevice";
    private const string IosSimulatorOutput = "Builds/iOSSimulator";
    private const string AndroidApkOutput = "Builds/Android/TreadShred.apk";
    private const string AndroidBundleOutput = "Builds/Android/TreadShred.aab";
    // Screenshot-only App Store metadata update. The live 1.8.0 build remains
    // unchanged; App Store Connect requires a new version/build to replace
    // screenshots after a version is approved.
    private const string ReleaseVersion = "1.8.1";
    private const int ReleaseBuildNumber = 12;
    private const int AndroidReleaseBuildNumber = 4;
    private const string ProductionIosAppId = "ca-app-pub-4402198490627677~4284546322";
    private const string ProductionAndroidAppId = "ca-app-pub-4402198490627677~8381484915";
    private const string ProductionIosInterstitial = "ca-app-pub-4402198490627677/5342886245";
    private const string ProductionAndroidInterstitial = "ca-app-pub-4402198490627677/3893066258";
    private const string ProductionIosRewarded = "ca-app-pub-4402198490627677/3118275391";
    private const string ProductionAndroidRewarded = "ca-app-pub-4402198490627677/6501143322";

    [MenuItem("Tread Shred/Build/iOS device project")]
    public static void BuildIosDeviceProject()
    {
        Environment.SetEnvironmentVariable("TREAD_SHRED_USE_TEST_ADS", null);
        Environment.SetEnvironmentVariable("TANK_TEAM_ID", "MCALPSQ5P5");
        Environment.SetEnvironmentVariable("TANK_PROVISIONING_PROFILE_SPECIFIER", "e11cb221-3c7d-436b-a97e-b153ce24a285");
        Environment.SetEnvironmentVariable("TANK_CODE_SIGN_IDENTITY", "Apple Distribution: Craig Glendenning (MCALPSQ5P5)");
        BuildIos(false);
    }

    [MenuItem("Tread Shred/Build/iOS device project (test ads)")]
    public static void BuildIosDeviceProjectWithTestAds()
    {
        Environment.SetEnvironmentVariable("TREAD_SHRED_USE_TEST_ADS", "1");
        Environment.SetEnvironmentVariable("TANK_TEAM_ID", "MCALPSQ5P5");
        // Use the profile UUID so Xcode cannot resolve a stale profile with
        // the same display name from another local download.
        Environment.SetEnvironmentVariable("TANK_PROVISIONING_PROFILE_SPECIFIER", "e11cb221-3c7d-436b-a97e-b153ce24a285");
        Environment.SetEnvironmentVariable("TANK_CODE_SIGN_IDENTITY", "Apple Distribution: Craig Glendenning (MCALPSQ5P5)");
        BuildIos(false);
    }

    [MenuItem("Tread Shred/Build/iOS simulator project")]
    public static void BuildIosSimulatorProject() => BuildIos(true);

    [MenuItem("Tread Shred/Build/Android APK")]
    public static void BuildAndroidApk() => BuildAndroid(false);

    public static void BuildIosFromCommandLine() => BuildIos(false);
    public static void BuildIosSimulatorFromCommandLine() => BuildIos(true);
    public static void BuildAndroidFromCommandLine() => BuildAndroid(false);

    public static void BuildAndroidBundleFromCommandLine()
    {
        BuildAndroid(true);
    }

    private static void BuildIos(bool simulator)
    {
        PreparePlayerSettings();
        var output = Path.GetFullPath(simulator ? IosSimulatorOutput : IosOutput);
        if (Directory.Exists(output))
            FileUtil.DeleteFileOrDirectory(output);
        Directory.CreateDirectory(output);

        PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
        if (simulator)
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
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
        PatchIosSigning(output);
        PatchIosPodfile(output);
        Debug.Log("Tread Shred iOS export created at " + output);
    }

    private static void BuildAndroid(bool bundle)
    {
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new BuildFailedException("Unable to switch Unity to the Android build target.");

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
        ConfigureApplicationIcons();
        PlayerSettings.bundleVersion = ReleaseVersion;
        PlayerSettings.iOS.buildNumber = ReleaseBuildNumber.ToString();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, IosBundleIdentifier);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidBundleIdentifier);
        PlayerSettings.Android.bundleVersionCode = AndroidReleaseBuildNumber;
        // Unity 6 no longer supports API 23; keep the project aligned with the
        // current Android player minimum and avoid the legacy project's setting.
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        ConfigureAndroidSigning();
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

    private static void ConfigureApplicationIcons()
    {
        const string iconPath = "Assets/Art/UI-Game/tanks-app-icon.png";
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
            throw new BuildFailedException("Tread Shred application icon not found: " + iconPath);

        foreach (var platform in new[] { BuildTargetGroup.iOS, BuildTargetGroup.Android })
        {
            SetIconsForKind(platform, icon, IconKind.Application);
        }

        // iOS keeps its App Store artwork in a separate Store icon kind.
        SetIconsForKind(BuildTargetGroup.iOS, icon, IconKind.Store);
    }

    private static void SetIconsForKind(BuildTargetGroup platform, Texture2D icon, IconKind kind)
    {
        var sizes = PlayerSettings.GetIconSizesForTargetGroup(platform, kind);
        var icons = Enumerable.Repeat(icon, sizes.Length).ToArray();
        PlayerSettings.SetIconsForTargetGroup(platform, icons, kind);
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
        var configuration = AssetDatabase.LoadAssetAtPath<TankAdConfiguration>(path);
        if (configuration == null)
        {
            configuration = ScriptableObject.CreateInstance<TankAdConfiguration>();
            AssetDatabase.CreateAsset(configuration, path);
        }

        // The checked-in binary asset predates the store migration, so do not
        // rely on C# field initializers to update it. Test builds opt in via
        // TREAD_SHRED_USE_TEST_ADS; production is the safe default.
        configuration.iosAppId = ProductionIosAppId;
        configuration.androidAppId = ProductionAndroidAppId;
        configuration.iosInterstitialUnitId = ProductionIosInterstitial;
        configuration.androidInterstitialUnitId = ProductionAndroidInterstitial;
        configuration.iosRewardedUnitId = ProductionIosRewarded;
        configuration.androidRewardedUnitId = ProductionAndroidRewarded;
        configuration.useTestAds = string.Equals(
            Environment.GetEnvironmentVariable("TREAD_SHRED_USE_TEST_ADS"),
            "1",
            StringComparison.Ordinal);
        EditorUtility.SetDirty(configuration);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureAndroidSigning()
    {
        var keystore = Environment.GetEnvironmentVariable("TREAD_SHRED_ANDROID_KEYSTORE");
        var alias = Environment.GetEnvironmentVariable("TREAD_SHRED_ANDROID_KEY_ALIAS");
        var keystorePassword = Environment.GetEnvironmentVariable("TREAD_SHRED_ANDROID_KEYSTORE_PASSWORD");
        var aliasPassword = Environment.GetEnvironmentVariable("TREAD_SHRED_ANDROID_KEY_PASSWORD");
        var productionBuild = string.Equals(
            Environment.GetEnvironmentVariable("TREAD_SHRED_ANDROID_RELEASE"),
            "1",
            StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(keystore) || string.IsNullOrWhiteSpace(alias) ||
            string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(aliasPassword))
        {
            if (productionBuild)
                throw new BuildFailedException(
                    "Android release signing is required. Set TREAD_SHRED_ANDROID_KEYSTORE, " +
                    "TREAD_SHRED_ANDROID_KEY_ALIAS, TREAD_SHRED_ANDROID_KEYSTORE_PASSWORD, " +
                    "and TREAD_SHRED_ANDROID_KEY_PASSWORD.");

            PlayerSettings.Android.useCustomKeystore = false;
            return;
        }

        if (!File.Exists(keystore))
            throw new BuildFailedException("Android release keystore not found: " + keystore);

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = alias;
        PlayerSettings.Android.keyaliasPass = aliasPassword;
    }

    private static void PatchIosInfoPlist(string buildPath)
    {
        var plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
            return;

        var configuration = AssetDatabase.LoadAssetAtPath<TankAdConfiguration>(
            "Assets/Resources/TankAdConfiguration.asset");
        var iosAppId = configuration != null ? configuration.iosAppId : ProductionIosAppId;
        if (string.IsNullOrWhiteSpace(iosAppId))
            iosAppId = ProductionIosAppId;

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        // Do not rely solely on the Google Mobile Ads postprocessor here. A
        // release export must always carry the app ID or the native iOS SDK
        // throws GADInvalidInitializationException during launch.
        plist.root.SetString("GADApplicationIdentifier", iosAppId);
        plist.root.SetString("NSUserTrackingUsageDescription", "This identifier will be used to deliver personalized ads to you.");
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        File.WriteAllText(plistPath, plist.WriteToString());
    }

    private static void PatchIosSigning(string buildPath)
    {
        var teamId = Environment.GetEnvironmentVariable("TANK_TEAM_ID");
        var profile = Environment.GetEnvironmentVariable("TANK_PROVISIONING_PROFILE_SPECIFIER");
        var identity = Environment.GetEnvironmentVariable("TANK_CODE_SIGN_IDENTITY");
        if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(profile))
            throw new BuildFailedException(
                "iOS release signing requires TANK_TEAM_ID and TANK_PROVISIONING_PROFILE_SPECIFIER.");

        var projectPath = PBXProject.GetPBXProjectPath(buildPath);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        var mainTarget = project.GetUnityMainTargetGuid();

        // Sign only the application target. UnityFramework and CocoaPods are
        // embedded code and must not inherit the app's provisioning profile.
        project.SetBuildProperty(mainTarget, "CODE_SIGN_STYLE", "Manual");
        project.SetBuildProperty(mainTarget, "DEVELOPMENT_TEAM", teamId);
        if (string.IsNullOrWhiteSpace(identity))
            identity = "Apple Distribution";
        project.SetBuildProperty(mainTarget, "CODE_SIGN_IDENTITY", identity);
        project.SetBuildProperty(mainTarget, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", identity);
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE_APP", profile);
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE_SPECIFIER_APP", profile);
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE", "$(PROVISIONING_PROFILE_APP)");
        project.SetBuildProperty(mainTarget, "PROVISIONING_PROFILE_SPECIFIER", "$(PROVISIONING_PROFILE_SPECIFIER_APP)");
        project.WriteToFile(projectPath);
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
        var path = Path.Combine(buildPath, "TreadShred.entitlements");
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
