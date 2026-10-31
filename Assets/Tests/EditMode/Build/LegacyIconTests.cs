using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace TreadShred.BuildPolicy.Tests
{
    public sealed class LegacyIconTests
    {
        private const string LegacyPlay = "Assets/Image/png/shopPlayButton.png";
        private const string LegacyBack = "Assets/Image/png/back_button.png";

        [Test(Description = "D-013-AC-02: no scene in the build and no prefab shows the original game's white play triangle or back arrow")]
        public void NoLegacyPlayOrBackIcons()
        {
            var assets = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
                .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath));
            var offenders = assets.Where(path => AssetDatabase.GetDependencies(path, false)
                .Any(d => d == LegacyPlay || d == LegacyBack)).ToList();
            Assert.IsEmpty(offenders, "still using the legacy icons: " + string.Join(", ", offenders));
        }
    }
}
