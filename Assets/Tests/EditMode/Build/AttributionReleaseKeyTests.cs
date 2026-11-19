using NUnit.Framework;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>D-023: a release can never again ship without Tenjin's SDK key (1.9.7 and 1.9.8 version code 17 did).</summary>
    public sealed class AttributionReleaseKeyTests
    {
        [Test(Description = "D-023-AC-01: an Android release build without Tenjin's SDK key stops; a development build only warns")]
        public void ReleaseWithoutTheTenjinKeyStops()
        {
            StringAssert.Contains(AttributionBuildPolicy.TenjinKeyVariable, AttributionBuildPolicy.MissingKeyStopsBuild(null, release: true));
            StringAssert.Contains(AttributionBuildPolicy.TenjinKeyVariable, AttributionBuildPolicy.MissingKeyStopsBuild("  ", release: true));
            Assert.IsNull(AttributionBuildPolicy.MissingKeyStopsBuild("7XKQ2HDLWBTQKZ5JNSG3EPR6FCVMUAY4", release: true));
            Assert.IsNull(AttributionBuildPolicy.MissingKeyStopsBuild(null, release: false));
        }

        [Test(Description = "D-023-AC-01: the Android release script requires Tenjin's key and checks the finished bundle carries it")]
        public void ReleaseScriptChecksTheBundle()
        {
            var script = System.IO.File.ReadAllText(System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), "scripts", "build-tank-android-release.sh"));
            StringAssert.Contains(": \"${TREAD_SHRED_TENJIN_SDK_KEY:?", script);
            StringAssert.Contains("base/manifest/AndroidManifest.xml", script);
            StringAssert.Contains("TREAD_SHRED_ANDROID_RELEASE=1", script);
        }
    }
}
