using System;
using System.IO;
using TreadShred.BuildPolicy;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// D-023: writes Tenjin's SDK key into the generated Android manifest from TREAD_SHRED_TENJIN_SDK_KEY. A release
/// build without the key fails; a development build without it still works but reports no attribution, and says so.
/// </summary>
public sealed class TenjinAndroidBuild : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var key = Environment.GetEnvironmentVariable(AttributionBuildPolicy.TenjinKeyVariable);
        var release = Environment.GetEnvironmentVariable(AttributionBuildPolicy.AndroidReleaseVariable) == "1";
        if (AttributionBuildPolicy.MissingKeyStopsBuild(key, release) is string stop)
            throw new BuildFailedException("[Tenjin] " + stop);
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[Tenjin] " + AttributionBuildPolicy.TenjinKeyVariable +
                             " is not set: this Android build reports no install attribution");
            return;
        }
        var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        File.WriteAllText(manifestPath, AttributionBuildPolicy.WithTenjinSdkKey(File.ReadAllText(manifestPath), key));
        Debug.Log("[Tenjin] SDK key written to the Android manifest");
    }
}
