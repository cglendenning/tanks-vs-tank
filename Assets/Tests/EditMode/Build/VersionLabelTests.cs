using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Build.Tests
{
    public sealed class VersionLabelTests
    {
        [Test(Description = "D-014-AC-04: the settings screen labels the version VERSION <x.y.z>, and the build sets that version from the release version, so it matches the stores")]
        public void SettingsNamesTheReleaseVersion()
        {
            var panel = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TreadShredSettingsPanel")).First(t => t != null);
            Assert.AreEqual("VERSION 1.9.7", panel.GetMethod("VersionLabel").Invoke(null, new object[] { "1.9.7" }));
            StringAssert.Contains("VersionLabel(Application.version)",
                File.ReadAllText(Path.Combine(Application.dataPath, "Script", "TreadShredSettingsPanel.cs")));
            StringAssert.Contains("PlayerSettings.bundleVersion = ReleaseVersion;",
                File.ReadAllText(Path.Combine(Application.dataPath, "Editor", "TankBuildAutomation.cs")));
        }
    }
}
