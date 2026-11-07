using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class NextMissionOfferTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-040-AC-01: NEXT MISSION opens an offer before the mission loads: Overdrive when it is not running, else the cheapest next upgrade, and with nothing left the mission loads at once")]
        public void NextMissionOpensTheRightOffer()
        {
            StringAssert.Contains("if (TreadShredNextMissionOffer.ShowBeforeNext(TreadShredUi.FindFont(GameObject.Find(\"Canvas\")), LoadNextMission))\n            return;\n        LoadNextMission();",
                Script("ManagerScore/ManagerButton.cs"));
            var offer = Script("Game/TreadShredNextMissionOffer.cs");
            StringAssert.Contains("if (TreadShredOverdrive.Active)\n        {\n            track = TreadShredProgress.Ensure().Progression.CheapestNextUpgrade();\n            if (track == null)\n                return false;", offer);
            StringAssert.Contains("overdrive ? OfferLabels.ArmOverdrive : \"UPGRADE \" + ProgressionLabels.Track(track.Value)", offer);
            Assert.AreEqual(("ARM OVERDRIVE", "FOR YOUR NEXT MISSION", "2 SHOTS PER TAP", "FOR AT LEAST 5 MISSIONS"),
                (OfferLabels.ArmOverdrive, OfferLabels.ForNextMission, OfferLabels.OverdriveShots, OfferLabels.OverdriveLasts));
            StringAssert.Contains("TreadShredUi.StylePrimary(buyButton);", offer);
        }

        [Test(Description = "D-040-AC-02: NO THANKS, PLAY loads the mission, Back stays on the victory screen, Overdrive bought there loads the mission once armed, and an upgrade bought there offers PLAY with UNDO")]
        public void OfferOutcomes()
        {
            Assert.AreEqual("NO THANKS, PLAY", OfferLabels.NoThanksPlay);
            var offer = Script("Game/TreadShredNextMissionOffer.cs");
            StringAssert.Contains("CreateButton(OfferLabels.NoThanksPlay, panel,", offer);
            StringAssert.Contains("TreadShredBack.Opened(this, () => Destroy(gameObject));", offer, "Back closes without loading the mission");
            StringAssert.Contains("TreadShredOverdrive.Activate(font, \"overdrive_next_mission\", Continue, showStatus);", offer);
            StringAssert.Contains("TreadShredUpgradePanel.Checkout(shop, track.Value, font,", offer, "the upgrade screen's own checkout (D-036)");
            StringAssert.Contains("TreadShredUi.SetLabel(buyButton, \"PLAY\");", offer);
            StringAssert.Contains("undoButton.gameObject.SetActive(true);", offer);
        }

        [Test(Description = "D-040-AC-03: each next-mission offer is recorded as offer_shown, and Overdrive bought there as offer_accepted with overdrive_next_mission")]
        public void OffersAreRecorded()
        {
            StringAssert.Contains("GameEvents.OfferShown(overdrive ? \"overdrive_next_mission\" : \"upgrade_next_mission\",", Script("Game/TreadShredNextMissionOffer.cs"));
            StringAssert.Contains("TreadShredAnalytics.Log(GameEvents.OfferAccepted(offerName, GemCatalog.Cost(GemOffer.Overdrive), packBought));",
                Script("Game/TreadShredOverdrive.cs"));
        }

        [Test(Description = "D-017-AC-08: the upgrade screen lists Overdrive after the tracks, with its clip, FOR YOUR NEXT MISSION or ACTIVE, 2 SHOTS PER TAP and the one-price ACTIVATE button, which reads ACTIVE while it runs")]
        public void UpgradeScreenListsOverdrive()
        {
            Assert.AreEqual("ACTIVE", OfferLabels.Active);
            var panel = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("SalesPitches.ForOffer(GemOffer.Overdrive), font, OpenOverdriveSalesScreen);", panel);
            // D-045: Overdrive is supply; the row shows what is held and can always add more.
            StringAssert.Contains("ProgressionLabels.SupplyLine(overdrive, Progression.SupplyMissionsLeft(SupplyTrack.Overdrive))", panel);
            StringAssert.Contains("TreadShredUi.SetLabel(overdriveButton, TreadShredOverdrive.ButtonLabel());", panel);
            StringAssert.Contains("return OfferLabels.ActionButton(Active ? \"ADD\" : \"ACTIVATE\", plan,", Script("Game/TreadShredOverdrive.cs"), "ADD while some is held (D-045)");
            StringAssert.Contains("if (OfferLabels.NeedsConfirmation(store.PlanFor(GemOffer.Overdrive)))", Script("Game/TreadShredOverdrive.cs"),
                "gems the player holds are confirmed once (D-036-AC-03)");
        }
    }
}
