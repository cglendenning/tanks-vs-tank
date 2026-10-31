#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TreadShred.BuildPolicy;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class TankBuildAutomation
{
    private const string ProductName = "Tread Shred";
    // These are the identifiers already registered in the stores.
    private const string IosBundleIdentifier = "com.tgts.tanksvstank";
    private const string AndroidBundleIdentifier = "com.tgts.tankkvstank";
    private const string IosOutput = "Builds/iOSDevice";
    private const string IosSimulatorOutput = "Builds/iOSSimulator";
    private const string AndroidApkOutput = "Builds/Android/TreadShred.apk";
    private const string AndroidBundleOutput = "Builds/Android/TreadShred.aab";
    // Screenshot-only App Store metadata update. The live 1.8.0 build remains
    // unchanged; App Store Connect requires a new version/build to replace
    // screenshots after a version is approved.
    private const string ReleaseVersion = "1.9.1";
    private const int ReleaseBuildNumber = 18;
    private const int AndroidReleaseBuildNumber = 6;

    [MenuItem("Tread Shred/Build/iOS device project")]
    public static void BuildIosDeviceProject()
    {
        Environment.SetEnvironmentVariable("TANK_TEAM_ID", "MCALPSQ5P5");
        // Use the profile UUID so Xcode cannot resolve a stale profile with
        // the same display name from another local download.
        Environment.SetEnvironmentVariable("TANK_PROVISIONING_PROFILE_SPECIFIER", "6fd40f8b-c6b6-4cac-8c17-703352d44a8d");
        // Pin the certificate by SHA-1: two certificates on this Mac share the
        // "Apple Distribution: Craig Glendenning" name and only this one is in the profile.
        Environment.SetEnvironmentVariable("TANK_CODE_SIGN_IDENTITY", "66F1F59CE546829AA563FE01BF325E9BC197584A");
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
        ConfigureCapabilities(output);
        EnforceNoAdvertisingId(output);
        // Simulator apps are not provisioned; only device exports need the
        // distribution identity and profile.
        if (!simulator)
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
        // ARMv7 too: budget phones such as the moto g play (2023) run 32-bit Android and cannot install an ARM64-only build.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
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

    // D-005, D-021-AC-01: swap in Firebase Analytics without ad-ID support,
    // then fail the build if anything could still collect the ad ID.
    private static void EnforceNoAdvertisingId(string buildPath)
    {
        var projectPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
        var project = XcodeProjectPolicy.UseAnalyticsWithoutAdvertisingId(File.ReadAllText(projectPath));
        File.WriteAllText(projectPath, project);
        var violations = XcodeProjectPolicy.AdvertisingIdViolations(project,
            File.ReadAllText(Path.Combine(buildPath, "Info.plist")));
        if (violations.Count > 0)
            throw new BuildFailedException("iOS export can collect the advertising ID: " + string.Join(", ", violations));
    }

    private static void PatchIosInfoPlist(string buildPath)
    {
        var plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
            return;

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        // D-023-AC-04: Apple copies each SKAdNetwork install postback to Tenjin.
        plist.root.SetString(AttributionBuildPolicy.SkanReportEndpointKey, AttributionBuildPolicy.SkanReportEndpoint);
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

    // D-032 Game Center for the leaderboard and D-025 iCloud key-value storage for cloud
    // save. The App ID and the Ad Hoc profile carry both capabilities.
    private static void ConfigureCapabilities(string buildPath)
    {
        var projectPath = PBXProject.GetPBXProjectPath(buildPath);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        var capabilities = new ProjectCapabilityManager(projectPath, "TreadShred.entitlements", null,
            project.GetUnityMainTargetGuid());
        capabilities.AddGameCenter();
        capabilities.AddiCloud(true, false, false, false, null);
        capabilities.WriteToFile();
    }

    private static void AssertBuildSucceeded(BuildReport report, string platform)
    {
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(platform + " build failed with " + report.summary.totalErrors + " errors.");
    }
}
#endif
