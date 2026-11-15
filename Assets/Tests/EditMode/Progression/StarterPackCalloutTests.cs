using System;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // The Starter Pack's interstitial opens by itself once; after that the result screen's pulsing callout sells it (D-037-AC-04).
    public sealed class StarterPackCalloutTests
    {
        // Round-trips through JsonUtility, as PlayerPrefs and the cloud snapshot do, so a reload sees only saved fields.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-037-AC-04: the interstitial opens by itself only once: the wallet records it shown, saved and restored, and a reset forgets it; after that only the result screen's callout opens it")]
        public void InterstitialOpensByItselfOnce()
        {
            var store = new JsonWalletStore();
            var wallet = new GemWallet(store);
            Assert.IsFalse(wallet.StarterPackShown);
            wallet.MarkStarterPackShown();
            Assert.IsTrue(new GemWallet(store).StarterPackShown, "saved with the wallet");
            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { starterPackShown = true })));
            Assert.IsTrue(restored.StarterPackShown, "restored from a cloud save (D-025)");
            wallet.Replace(new WalletState());
            Assert.IsFalse(wallet.StarterPackShown, "a reset (D-044) brings it back");

            var offer = Script("Game/TreadShredStarterOffer.cs");
            StringAssert.Contains("public static bool ShowOverResult(Font font, bool won) => !TreadShredStore.Ensure().Wallet.StarterPackShown && Show(font, true, won);", offer);
            StringAssert.Contains("public static bool ShowIfOnSale(Font font) => !TreadShredStore.Ensure().Wallet.StarterPackShown && Show(font, false, false);", offer);
            StringAssert.Contains("public static bool OpenFromCallout(Font font, bool won) => Show(font, true, won);", offer);
            StringAssert.Contains("TreadShredStore.Ensure().Wallet.MarkStarterPackShown();", offer);
            StringAssert.Contains("TreadShredStarterCallout.ResultScreen((RectTransform)VictoryofFail.transform, TreadShredUi.FindFont(VictoryofFail), won);",
                Script("ManagerScore/ManagerScore.cs"));
            StringAssert.Contains("() => TreadShredStarterOffer.OpenFromCallout(font, won));", Script("Game/TreadShredStarterCallout.cs"));
        }

        [Test(Description = "D-037-AC-04: the result screen's callout reads STARTER PACK and ENDS IN <m:ss>, and pulses faster and further every minute the window runs down")]
        public void CalloutPulsesHarderAsTimeRunsDown()
        {
            Assert.AreEqual("STARTER PACK\nENDS IN 14:32", StarterPackPitch.CalloutLabel(TimeSpan.FromSeconds(872)));
            Assert.AreEqual(0f, StarterPackPitch.Urgency(TimeSpan.FromMinutes(20)), "a clock set back is never calmer than the whole window");
            var previous = (Rate: 0f, Strength: 0f);
            for (var minutes = 15; minutes >= 0; minutes--)
            {
                var left = TimeSpan.FromMinutes(minutes);
                var now = (Rate: StarterPackPitch.PulseRate(left), Strength: StarterPackPitch.PulseStrength(left));
                Assert.Greater(now.Rate, previous.Rate, minutes + " minutes left: faster");
                Assert.Greater(now.Strength, previous.Strength, minutes + " minutes left: further");
                previous = now;
            }
            Assert.AreEqual(0.5f, StarterPackPitch.PulseRate(TimeSpan.FromMinutes(15)), 1e-5f, "subtle at first: one pulse every two seconds");
            Assert.AreEqual(0.2f, StarterPackPitch.PulseStrength(TimeSpan.FromMinutes(15)), 1e-5f);
            Assert.AreEqual(3f, StarterPackPitch.PulseRate(TimeSpan.Zero), 1e-5f, "three a second as it ends");
            Assert.AreEqual(1f, StarterPackPitch.PulseStrength(TimeSpan.Zero), 1e-5f);
        }
    }
}
