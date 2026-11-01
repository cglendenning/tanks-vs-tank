using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>D-013-AC-02: the Android launcher icon is the game's tank artwork, made for Android's masked shapes.</summary>
    public sealed class AndroidIconTests
    {
        private static string Root => Path.GetDirectoryName(Application.dataPath);

        private static Texture2D Load(string relative)
        {
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(Path.Combine(Root, relative))), relative);
            return texture;
        }

        [Test(Description = "D-013-AC-02: the adaptive icon has a full-bleed background and a foreground whose artwork fills the visible area, plus a round icon")]
        public void AdaptiveLayersFitAndroidMasks()
        {
            var background = Load("Assets/Art/UI-Game/Android/adaptive-icon-background.png");
            var foreground = Load("Assets/Art/UI-Game/Android/adaptive-icon-foreground.png");
            var round = Load("Assets/Art/UI-Game/Android/round-icon.png");

            // 108 dp layers at the highest density; launchers show the middle 72 dp (288 px).
            Assert.AreEqual((432, 432), (background.width, background.height));
            Assert.AreEqual((432, 432), (foreground.width, foreground.height));
            Assert.AreEqual(1f, background.GetPixel(0, 0).a, "the background reaches every edge");
            Assert.AreEqual(0f, foreground.GetPixel(0, 0).a, "the foreground leaves the outer margin to the background");
            // Every launcher shape shows Android's 66 dp safe circle (radius 132 px); the artwork must cover all of it.
            foreach (var (x, y) in new[] { (123, 123), (309, 123), (123, 309), (309, 309), (216, 84), (216, 348), (84, 216), (348, 216), (216, 216) })
                Assert.AreEqual(1f, foreground.GetPixel(x, y).a, $"the artwork covers the safe circle at {x},{y}");
            Assert.AreEqual(0f, round.GetPixel(0, 0).a, "the round icon is a circle");
            Assert.AreEqual(1f, round.GetPixel(round.width / 2, round.height / 2).a);
        }

        [Test(Description = "D-013-AC-02: the build gives Android those adaptive and round icons and keeps iOS on the original artwork")]
        public void BuildConfiguresAndroidLauncherIcons()
        {
            var automation = File.ReadAllText(Path.Combine(Root, "Assets/Editor/TankBuildAutomation.cs"));
            StringAssert.Contains("AndroidPlatformIconKind.Adaptive", automation);
            StringAssert.Contains("slot.SetTextures(background, foreground)", automation);
            StringAssert.Contains("AndroidPlatformIconKind.Round", automation);
            StringAssert.Contains("SetIconsForKind(BuildTargetGroup.iOS, icon, IconKind.Store)", automation);
        }
    }
}
