using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class RentalPauseTests
    {
        [Test(Description = "D-043-AC-03: the store opening or a purchase under way keeps the paused game paused; only an outcome (cancelled, failed, unavailable, short of gems, waiting for approval) returns to the mission, through the 3-2-1, and renting waits on READY?")]
        public void OnlyAnOutcomeEndsThePause()
        {
            Assert.IsFalse(PurchaseMessages.Ends(PurchaseMessages.Opening), "the store sheet is opening");
            Assert.IsFalse(PurchaseMessages.Ends(PurchaseMessages.InProgress), "a purchase is under way");
            foreach (var outcome in new[]
            {
                PurchaseMessages.Cancelled, PurchaseMessages.Failed, PurchaseMessages.Unavailable,
                PurchaseMessages.NotEnoughGems, PurchaseMessages.AwaitingApproval
            })
                Assert.IsTrue(PurchaseMessages.Ends(outcome), outcome);

            var offer = File.ReadAllText(Path.Combine(Application.dataPath, "Script/Game/TreadShredRentalOffer.cs"));
            StringAssert.Contains("if (!PurchaseMessages.Ends(status))\n                return;", offer, "the rental ignores the store opening");
            StringAssert.Contains("TreadShredReadyPanel.Show(font);", offer, "renting waits on READY?");
            StringAssert.Contains("if (Count.Replay())", offer, "NOT NOW and a failed purchase go back through the 3-2-1");
        }
    }
}
