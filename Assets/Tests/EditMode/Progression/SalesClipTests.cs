using System.IO;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.Video;

namespace TreadShred.Progression.Tests
{
    public sealed class SalesClipTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-034-AC-01: every item sold has its own bundled clip, 5 seconds at 640x360, silent and within 600 KB")]
        public void EveryItemHasItsClip()
        {
            CollectionAssert.AreEqual(new[] { "overdrive", "armored-redeploy", "armor", "firepower", "speed", "arc-shot", "triple-shot", "seeker" },
                SalesPitches.All.Select(SalesPitches.ClipName), "the items in R-SALES-CLIPS");
            foreach (var item in SalesPitches.All)
            {
                var file = Path.Combine(Application.dataPath, "Resources", SalesPitches.ClipResource(item) + ".mp4");
                Assert.IsTrue(File.Exists(file), file);
                Assert.LessOrEqual(new FileInfo(file).Length, 600 * 1024, item + " clip size");
                var clip = Resources.Load<VideoClip>(SalesPitches.ClipResource(item));
                Assert.IsNotNull(clip, item + " loads from Resources, so it ships inside the app");
                Assert.AreEqual(640u, clip.width, item + " width");
                Assert.AreEqual(360u, clip.height, item + " height");
                Assert.AreEqual(5.0, clip.length, 0.25, item + " length");
                Assert.AreEqual(0, clip.audioTrackCount, item + " is silent");
            }
        }

        [Test(Description = "D-034-AC-01: each clip is captured from the real game with only its own item's effect active")]
        public void ClipsCaptureOnlyTheirOwnEffect()
        {
            var capture = Script("TreadShredStoreCaptureMode.cs");
            StringAssert.Contains("mode == ClipArmor ? top : 0, mode == ClipFirepower ? top : 0, mode == ClipSpeed ? top : 0", capture);
            StringAssert.Contains("if (mode == Trailer || mode == ClipOverdrive)\n            TreadShredProgress.Ensure().Progression.AddSupply(TreadShred.Progression.SupplyTrack.Overdrive);", capture);
            StringAssert.Contains("if (mode == ClipRedeploy)\n            TreadShredBoosts.Current.ArmRedeploy();", capture);
        }

        [Test(Description = "D-034-AC-02: a clip loops silently on unscaled time, so it keeps playing while game time is paused")]
        public void ClipLoopsSilentlyOnUnscaledTime()
        {
            var host = new GameObject("clip");
            try
            {
                var player = host.AddComponent<VideoPlayer>();
                var configure = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("TreadShredSalesClip")).First(t => t != null).GetMethod("Configure");
                configure.Invoke(null, new object[] { player, null, null });
                Assert.IsTrue(player.isLooping);
                Assert.IsFalse(player.playOnAwake);
                Assert.AreEqual(VideoAudioOutputMode.None, player.audioOutputMode);
                Assert.AreEqual(VideoTimeUpdateMode.UnscaledGameTime, player.timeUpdateMode);
                Assert.AreEqual(VideoRenderMode.RenderTexture, player.renderMode);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test(Description = "D-034-AC-02: a clip plays beside both result-screen offers and at the start of every upgrade row")]
        public void ClipsPlayWhereItemsAreOffered()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("TreadShredSalesClip.Create(rootRect", results);
            StringAssert.Contains("SalesPitches.ForOffer(offer)", results);
            var upgrades = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("TreadShredSalesClip.Create(backdrop", upgrades);
            StringAssert.Contains("SalesPitches.ForTrack(track)", upgrades);
            Assert.AreEqual(SaleItem.Overdrive, SalesPitches.ForOffer(GemOffer.Overdrive));
            Assert.AreEqual(SaleItem.ArmoredRedeploy, SalesPitches.ForOffer(GemOffer.ArmoredRedeploy));
            Assert.AreEqual(SaleItem.Armor, SalesPitches.ForTrack(UpgradeTrack.Armor));
            Assert.AreEqual(SaleItem.Firepower, SalesPitches.ForTrack(UpgradeTrack.Firepower));
            Assert.AreEqual(SaleItem.Speed, SalesPitches.ForTrack(UpgradeTrack.Speed));
        }

        [Test(Description = "D-034-AC-03: each sales screen states only effects its item has, with numbers from the rules")]
        public void SalesCopyMatchesTheRules()
        {
            Assert.AreEqual(2, CombatBoosts.OverdriveRoundsPerTap, "Overdrive's copy says two shots");
            Assert.AreEqual("TWO SHOTS EVERY TAP", SalesPitches.For(SaleItem.Overdrive).Outcome);
            CollectionAssert.Contains(SalesPitches.For(SaleItem.Overdrive).Points, "FOR AT LEAST 5 MISSIONS");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.ArmoredRedeploy).Points, "REPLAY THIS MISSION");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.ArmoredRedeploy).Points, "WITH 1 EXTRA HIT OF ARMOR");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.ArmoredRedeploy).Points, "YOUR SCORE RUN CONTINUES");
            CollectionAssert.AreEqual(new[] { "+2 HITS FOR EVERY 100 GEMS", "FOR AT LEAST 5 MISSIONS" }, SalesPitches.For(SaleItem.Armor).Points, "armor is supply (D-045)");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.Firepower).Points, "UP TO X2.0 DAMAGE AT LEVEL 10");
            CollectionAssert.Contains(SalesPitches.For(SaleItem.Speed).Points, "UP TO X1.50 MOVE AND TURRET SPEED AT LEVEL 10");
            foreach (var item in SalesPitches.All)
            {
                var pitch = SalesPitches.For(item);
                foreach (var line in new[] { pitch.Title, pitch.Outcome }.Concat(pitch.Points))
                    Assert.AreEqual(line.ToUpperInvariant(), line, "D-013: labels are all caps");
            }
        }

        [Test(Description = "D-032-AC-01: the Armored Redeploy offer says the SCORE run continues, and the free retry reads RETRY")]
        public void RedeployOfferSaysTheRunContinues()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("CombatBoosts.RedeployExtraHits + \" EXTRA HIT. \" + SalesPitches.RunContinues,", results);
            StringAssert.Contains("RelabelFreeRetry();", results);
            Assert.AreEqual("RETRY", OfferLabels.Retry);
            Assert.AreEqual("YOUR SCORE RUN CONTINUES", SalesPitches.RunContinues);
        }

        [Test(Description = "D-034-AC-03: the sales screen's button is the offer's own, buying closes it first, and Back closes it")]
        public void SalesScreenSellsWithTheOffersOwnButton()
        {
            var screen = Script("Game/TreadShredSalesScreen.cs");
            StringAssert.Contains("Destroy(gameObject);\n                buy();", screen);
            StringAssert.Contains("TreadShredBack.Opened(this, Close)", screen);
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("rewardButton.GetComponentInChildren<Text>().text, rewardBalanceText.text,", results);
            StringAssert.Contains("rewardButton.interactable && !rewardUsedThisScreen, onTap);", results);
            var upgrades = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("ButtonLabel(track, false), PreviewLine(track),", upgrades);
            StringAssert.Contains("!TreadShredStore.Ensure().PurchaseInProgress, () => Upgrade(track));", upgrades, "never MAXED (D-045)");
        }
    }
}
