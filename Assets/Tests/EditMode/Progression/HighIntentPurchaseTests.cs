using System;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class HighIntentPurchaseTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-036-AC-01: an upgrade button names one price: coins, the gems it spends, or the store price of the pack it buys")]
        public void UpgradeButtonNamesOnePrice()
        {
            Func<GemPack, string> price = pack => pack.ProductId == "treadshred.gems100" ? "$0.99" : "$4.99";

            Assert.AreEqual("UPGRADE FOR 200 COINS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 0), false, 0, price));
            Assert.AreEqual("UPGRADE FOR 5 GEMS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 5, price),
                "the gems the player holds cover the rest");
            Assert.AreEqual("UPGRADE: $0.99", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 4, price),
                "one gem short: the smallest pack covers it");
            Assert.AreEqual("UPGRADE: $4.99", ProgressionLabels.UpgradeButton(new UpgradeQuote(4000, 4000), false, 0, price),
                "200 gems short: the 550-gem pack");
            Assert.AreEqual("UPGRADE: BUY 100 GEMS", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 0, _ => null),
                "the pack's gems until the store reports a price");
            Assert.AreEqual("MAXED", ProgressionLabels.UpgradeButton(new UpgradeQuote(0, 0), true, 0, price));
        }

        [Test(Description = "D-036-AC-01: an offer short of gems names the outcome and the store price, not the pack")]
        public void OfferButtonNamesTheOutcomeAndPrice()
        {
            Assert.AreEqual("REVIVE: £0.89", OfferLabels.ActionButton("REVIVE", GemCatalog.Plan(GemOffer.ArmoredRedeploy, 10), "£0.89"));
            Assert.AreEqual("REVIVE FOR 40 GEMS", OfferLabels.ActionButton("REVIVE", GemCatalog.Plan(GemOffer.ArmoredRedeploy, 40), "£0.89"));
        }

        [Test(Description = "D-036-AC-03: coins apply on the tap, gems the player holds are confirmed once, and a purchase goes to the store")]
        public void TapPathFollowsWhatThePlayerHolds()
        {
            Assert.AreEqual(UpgradeCheckout.ApplyNow, UpgradePurchase.Checkout(new UpgradeQuote(200, 0), 0));
            Assert.AreEqual(UpgradeCheckout.ConfirmGems, UpgradePurchase.Checkout(new UpgradeQuote(200, 100), 5));
            Assert.AreEqual(UpgradeCheckout.BuyGems, UpgradePurchase.Checkout(new UpgradeQuote(200, 100), 4));
        }

        [Test(Description = "D-036-AC-02: a purchase has no in-game confirmation; the store's own sheet is the only one")]
        public void NoConfirmationWhenGemsFallShort()
        {
            Assert.Throws<ArgumentException>(() =>
                ProgressionLabels.UpgradeConfirmation(UpgradeTrack.Armor, 2, new UpgradeQuote(400, 367), 2));
            StringAssert.Contains("if (UpgradePurchase.Checkout(quote, balance) != UpgradeCheckout.ConfirmGems)",
                Script("Game/TreadShredUpgradePanel.cs"));
        }

        [Test(Description = "D-036-AC-04: the note says what each currency is for, that upgrades spend coins first at the R-COINS rate, and that a priced button buys in one tap")]
        public void NoteExplainsSpendingAndOneTap()
        {
            var note = string.Join(" ", ProgressionLabels.CurrencyNote());

            StringAssert.Contains("COINS: YOU EARN THEM IN EVERY MISSION", note);
            StringAssert.Contains("GEMS: BUY THEM OR EARN THEM AT MILESTONES", note);
            StringAssert.Contains("COINS FIRST, THEN GEMS FOR THE REST: 1 GEM = " + ProgressionRules.CoinsPerGem + " COINS", note);
            StringAssert.Contains("A BUTTON SHOWING A PRICE BUYS IT AND APPLIES IT IN ONE TAP", note);
        }
    }
}
