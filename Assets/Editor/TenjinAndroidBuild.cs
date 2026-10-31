using System;
using System.IO;
using TreadShred.BuildPolicy;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// D-023: writes Tenjin's SDK key into the generated Android manifest from TREAD_SHRED_TENJIN_SDK_KEY. A build
/// without the key still works but reports no attribution, and says so in the build log.
/// </summary>
public sealed class TenjinAndroidBuild : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var key = Environment.GetEnvironmentVariable(AttributionBuildPolicy.TenjinKeyVariable);
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
