using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class PromotionTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Constant(string name) => GameType("TreadShredPromotion").GetField(name).GetValue(null);

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, "..", relative));

        [Test(Description = "D-019-AC-05: the promotion plays at the victory screen of the clear, lasts the march (about 10 s) with casings raining throughout, can be tapped away only after 5 s, and its march is the public-domain U.S. Army Band recording")]
        public void PromotionIsAVictoryMarch()
        {
            Assert.AreEqual(("Audio/PromotionMarch", "Audio/ShellRain"), ((string)Constant("MusicResource"), (string)Constant("SoundResource")));
            var march = Resources.Load<AudioClip>((string)Constant("MusicResource"));
            var rain = Resources.Load<AudioClip>((string)Constant("SoundResource"));
            Assert.IsNotNull(march, "the march is in the build");
            Assert.IsNotNull(rain, "the shell rain is in the build");
            Assert.AreEqual(10f, march.length, 0.1f, "the march's closing phrase and final chord");
            Assert.AreEqual(march.length + 0.3f, rain.length, 0.1f, "the rain lasts as long as the march");
            Assert.AreEqual(rain.length, (float)Constant("Duration"), 0.05f, "the promotion lasts as long as both");
            Assert.AreEqual(5f, (float)Constant("TapAfter"));
            Assert.Greater((float)Constant("CasingsPerSecond"), 10f, "a steady rain, not one burst");

            var victory = Read("Assets/Script/ManagerScore/ManagerScore.cs");
            StringAssert.Contains("TreadShredCampaign.TakePromotion();", victory, "taken at the victory screen of the clear");
            StringAssert.Contains("MusicManager.instance.StopBattleMusic();", victory, "the march stands in for the victory music");
            var sources = Read("scripts/make-shell-rain.py");
            StringAssert.Contains("{{PD-USGov-Army}}", sources, "the recording's public-domain status is on record");
            StringAssert.Contains("File:Washington_Post_March_-_U.S._Army_Band.ogg", sources);
        }
    }
}
