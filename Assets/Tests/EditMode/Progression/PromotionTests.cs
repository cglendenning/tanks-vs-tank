using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class PromotionTests
    {

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, "..", relative));

        [Test(Description = "D-019-AC-05: the promotion plays at the victory screen of the clear, lasts the march (about 10 s) with casings raining throughout, can be tapped away only after 5 s, and its march is the public-domain U.S. Army Band recording")]
        public void PromotionIsAVictoryMarch()
        {
            Assert.AreEqual(("Audio/PromotionMarch", "Audio/ShellRain"), ((string)TreadShredPromotion.MusicResource, (string)TreadShredPromotion.SoundResource));
            var march = Resources.Load<AudioClip>((string)TreadShredPromotion.MusicResource);
            var rain = Resources.Load<AudioClip>((string)TreadShredPromotion.SoundResource);
            Assert.IsNotNull(march, "the march is in the build");
            Assert.IsNotNull(rain, "the shell rain is in the build");
            Assert.AreEqual(10f, march.length, 0.1f, "the march's closing phrase and final chord");
            Assert.AreEqual(march.length + 0.3f, rain.length, 0.1f, "the rain lasts as long as the march");
            Assert.AreEqual(rain.length, (float)TreadShredPromotion.Duration, 0.05f, "the promotion lasts as long as both");
            Assert.AreEqual(5f, (float)TreadShredPromotion.TapAfter);
            Assert.Greater((float)TreadShredPromotion.CasingsPerSecond, 10f, "a steady rain, not one burst");

            var victory = Read("Assets/Script/ManagerScore/ManagerScore.cs");
            StringAssert.Contains("TreadShredCampaign.TakePromotion();", victory, "taken at the victory screen of the clear");
            StringAssert.Contains("MusicManager.instance.StopBattleMusic();", victory, "the march stands in for the victory music");
            var sources = Read("scripts/make-shell-rain.py");
            StringAssert.Contains("{{PD-USGov-Army}}", sources, "the recording's public-domain status is on record");
            StringAssert.Contains("File:Washington_Post_March_-_U.S._Army_Band.ogg", sources);
        }
    }
}
