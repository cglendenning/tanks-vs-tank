using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class StarterPackTests
    {
        // Round-trips through JsonUtility, as PlayerPrefs and the cloud snapshot do, so a reload sees only saved fields.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private sealed class FakeGateway : IPurchaseGateway
        {
            public readonly List<string> Started = new List<string>();

            public bool TryStartPurchase(string productId)
            {
                Started.Add(productId);
                return true;
            }
        }

        private static readonly DateTime Launch = new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc);

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        private static GemPack Starter => GemCatalog.StarterPack;

        // A wallet whose Starter Pack window opened at Launch, and a flow that reads the given clock.
        private static (GemWallet Wallet, PurchaseFlow Flow, FakeGateway Gateway) OpenAt(Func<DateTime> clock, bool available = true)
        {
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.OpenStarterPackWindow(Launch);
            var gateway = new FakeGateway();
            return (wallet, new PurchaseFlow(wallet, gateway, () => available, clock), gateway);
        }

        [Test(Description = "D-037-AC-01: the Starter Pack is its own consumable product, 300 gems at the smallest pack's price for 15 minutes, kept apart from the regular packs and fetched from the store")]
        public void StarterPackMatchesItsRule()
        {
            Assert.AreEqual(("treadshred.starter300", 300, 99), (Starter.ProductId, Starter.Gems, Starter.UsdCents));
            Assert.AreEqual(TimeSpan.FromMinutes(15), GemCatalog.StarterPackWindow, "R-STARTER-PACK offer_minutes");
            CollectionAssert.DoesNotContain(GemCatalog.Packs, Starter, "kept apart from R-GEM-PACKS");
            Assert.AreSame(Starter, GemCatalog.FindPack(Starter.ProductId));
            Assert.AreEqual(3 * GemCatalog.Packs[0].Gems, Starter.Gems, "three times the smallest pack's gems");
            Assert.AreEqual(GemCatalog.Packs[0].UsdCents, Starter.UsdCents, "for the same price");
            StringAssert.Contains(".Append(GemCatalog.StarterPack)", Script("Store/TreadShredStore.cs"));
        }

        [Test(Description = "D-037-AC-01: buying the Starter Pack credits its gems and ends the offer, survives a reload and a cloud restore, and a second paid order still credits")]
        public void BuyingItEndsTheOfferAndPersists()
        {
            var store = new JsonWalletStore();
            var wallet = new GemWallet(store);
            wallet.OpenStarterPackWindow(Launch);

            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-1"));
            Assert.AreEqual((300, true), (wallet.Balance, wallet.StarterPackBought));
            Assert.AreEqual(TimeSpan.Zero, wallet.StarterPackTimeLeft(Launch.AddMinutes(1)), "bought: no time left to buy it");
            Assert.IsTrue(new GemWallet(store).StarterPackBought, "saved with the wallet");
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-2"),
                "a paid order is always credited, even a second Starter Pack");
            Assert.AreEqual(600, wallet.Balance);
            Assert.AreEqual(CreditResult.Duplicate, wallet.CreditPurchase(Starter.ProductId, "order-2"));

            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { balance = 50, starterPackBought = true })));
            Assert.IsTrue(restored.StarterPackBought, "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-037-AC-02: the Starter Pack is on sale for 15 minutes from the first launch, never after it is bought or once the window ends, never while the store lacks its product, and a clock set back never shows more than 15 minutes")]
        public void OnSaleOnlyInsideItsWindow()
        {
            var now = Launch;
            var available = true;
            var wallet = new GemWallet(new JsonWalletStore());
            var flow = new PurchaseFlow(wallet, new FakeGateway(), () => available, () => now);

            Assert.IsFalse(flow.StarterPackOnSale, "not before its window opens");
            wallet.OpenStarterPackWindow(Launch);
            Assert.AreEqual((true, TimeSpan.FromMinutes(15)), (flow.StarterPackOnSale, flow.StarterPackTimeLeft));
            now = Launch.AddMinutes(14).AddSeconds(59);
            Assert.AreEqual((true, TimeSpan.FromSeconds(1)), (flow.StarterPackOnSale, flow.StarterPackTimeLeft));
            now = Launch.AddMinutes(15);
            Assert.AreEqual((false, TimeSpan.Zero), (flow.StarterPackOnSale, flow.StarterPackTimeLeft), "the window has ended");
            Assert.AreSame(GemCatalog.Packs[0], flow.PlanFor(40).PackToBuy, "every purchase behaves as before");

            now = Launch.AddHours(-3);
            Assert.AreEqual(TimeSpan.FromMinutes(15), flow.StarterPackTimeLeft, "a clock set back never shows more than the window");
            now = Launch.AddMinutes(5);
            available = false;
            Assert.IsFalse(flow.StarterPackOnSale, "not on sale when the store lacks its product");
            Assert.IsFalse(new PurchaseFlow(wallet, new FakeGateway()).StarterPackOnSale, "no store, no sale");
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.PlanFor(40, 0).PackToBuy, "the plain choice is unchanged");
        }

        [Test(Description = "D-037-AC-02: the window opens once at the player's first launch that sells the pack, and a cloud restore keeps the restored window so a reinstall never reopens it")]
        public void WindowOpensOnceAndSurvivesReinstall()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.OpenStarterPackWindow(Launch);
            wallet.OpenStarterPackWindow(Launch.AddDays(1));
            Assert.AreEqual(TimeSpan.Zero, wallet.StarterPackTimeLeft(Launch.AddDays(1)), "a later launch never reopens it");

            var reinstall = new GemWallet(new JsonWalletStore());
            reinstall.OpenStarterPackWindow(Launch.AddDays(1));
            reinstall.Replace(new WalletState { balance = 120, starterPackWindowStartTicks = Launch.Ticks });
            Assert.AreEqual(TimeSpan.Zero, reinstall.StarterPackTimeLeft(Launch.AddDays(1)), "the restored save's window stands");

            var update = new GemWallet(new JsonWalletStore());
            update.OpenStarterPackWindow(Launch);
            update.Replace(new WalletState { balance = 120 });
            Assert.AreEqual(TimeSpan.FromMinutes(10), update.StarterPackTimeLeft(Launch.AddMinutes(5)),
                "a save from before the Starter Pack keeps the window this install opened");
            StringAssert.Contains("Wallet.OpenStarterPackWindow(DateTime.UtcNow);", Script("Store/TreadShredStore.cs"));
        }

        [Test(Description = "D-037-AC-04: while on sale every one-tap purchase the Starter Pack covers buys it, never fewer gems for the same price, and the item applies on delivery")]
        public void OneTapPurchasesBuyTheStarterPack()
        {
            var (wallet, flow, gateway) = OpenAt(() => Launch.AddMinutes(1));
            var applied = 0;

            Assert.AreSame(Starter, flow.PlanFor(30).PackToBuy, "a revive");
            Assert.AreSame(Starter, flow.PackCovering(300), "an upgrade's gems up to its 300");
            Assert.AreSame(GemCatalog.Packs[1], flow.PackCovering(301), "past its gems, the 550 pack");

            flow.SpendOrBuy(GemCatalog.Cost(GemOffer.Overdrive), () => applied++, _ => { });
            Assert.AreEqual(new[] { Starter.ProductId }, gateway.Started, "Overdrive opens the Starter Pack's sheet");
            flow.OrderDelivered(Starter.ProductId, "order-1", out _);
            Assert.AreEqual((1, 200), (applied, wallet.Balance), "applied on delivery (Overdrive is 100 gems, D-045), the rest stays in the wallet");
            Assert.IsFalse(flow.StarterPackOnSale);
        }

        [Test(Description = "D-037-AC-04: buttons that will buy the Starter Pack name its price, the line under result-screen offers names the pack, and the gem store's slot reads STARTER PACK: 300 GEMS FOR <price>")]
        public void LabelsNameTheStarterPack()
        {
            var plan = GemCatalog.PlanFor(30, 0, starterOnSale: true);
            Func<GemPack, string> priceFor = pack => pack.ProductId;

            Assert.AreEqual("REVIVE: $0.99", OfferLabels.ActionButton("REVIVE", plan, "$0.99"));
            Assert.AreEqual("STARTER PACK: 300 GEMS. ONE TIME ONLY", OfferLabels.BalanceLine(0, plan));
            Assert.AreEqual("YOU HAVE 0 GEMS. NEED 30", OfferLabels.BalanceLine(0, GemCatalog.PlanFor(30, 0)), "unchanged without it");
            Assert.AreEqual("UPGRADE: treadshred.starter300",
                ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 0, priceFor, starterOnSale: true));
            Assert.AreEqual("UPGRADE: treadshred.gems100", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 0, priceFor));
            Assert.AreEqual("STARTER PACK: 300 GEMS FOR $0.99", OfferLabels.StarterPackStoreButton("$0.99"));
            Assert.AreEqual("STARTER PACK: 300 GEMS", OfferLabels.StarterPackStoreButton(null));

            var settings = Script("TreadShredSettingsPanel.cs");
            StringAssert.Contains("slot == 0 && TreadShredStore.Ensure().StarterPackOnSale ? GemCatalog.StarterPack : GemCatalog.Packs[slot]", settings);
            StringAssert.Contains("OfferLabels.StarterPackStoreButton(price)", settings);
            StringAssert.Contains("store.StarterPackOnSale);", Script("Game/TreadShredUpgradePanel.cs"));
        }

        [Test(Description = "D-037-AC-03: while on sale the revive panel leads with STARTER PACK, 300 GEMS + THIS REVIVE FREE, 3X THE GEMS, the countdown and REVIVE + 300 GEMS: <price>, which revives without spending gems; the plain revive shows only when gems cover it")]
        public void RevivePanelLeadsWithTheStarterPack()
        {
            Assert.AreEqual("300 GEMS + THIS REVIVE FREE", OfferLabels.StarterPackReviveHeadline);
            Assert.AreEqual("3X THE GEMS OF THE 100-GEM PACK", OfferLabels.StarterPackValue);
            Assert.AreEqual("REVIVE + 300 GEMS: $0.99", OfferLabels.StarterPackReviveButton("$0.99"));
            Assert.AreEqual("REVIVE + 300 GEMS", OfferLabels.StarterPackReviveButton(null));

            var revive = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("starterLayout = store.StarterPackOnSale;", revive);
            StringAssert.Contains("OfferLabels.StarterPackReviveButton(store.PriceFor(GemCatalog.StarterPack))", revive);
            StringAssert.Contains("reviveButton.gameObject.SetActive(!starterShown || plan.CanSpendNow);", revive);
            // Bought here: the revive applies on delivery and spends nothing (no SpendOrBuy, no TrySpend).
            var buy = revive.Substring(revive.IndexOf("private void BuyStarter()", StringComparison.Ordinal));
            buy = buy.Substring(0, buy.IndexOf("private void Update()", StringComparison.Ordinal));
            StringAssert.Contains("BuyPack(GemCatalog.StarterPack", buy);
            StringAssert.Contains("player.Revive();", buy);
            StringAssert.DoesNotContain("SpendOrBuy", buy);
            StringAssert.DoesNotContain("TrySpend", buy);
        }

        [Test(Description = "D-037-AC-04: the Starter Pack interstitial opens over every result screen and on the start screen while on sale, with ONE TIME ONLY, the countdown, 300 GEMS FOR <price>, what the gems buy, BUY and NOT NOW, and Back closes it")]
        public void InterstitialOpensAtEveryBreak()
        {
            Assert.AreEqual("THAT'S 3 OVERDRIVES OR 7 ARMORED REDEPLOYS", OfferLabels.StarterPackBuys);
            Assert.AreEqual("300 GEMS FOR $0.99", OfferLabels.Pack(Starter, "$0.99"));

            var offer = Script("Game/TreadShredStarterOffer.cs");
            StringAssert.Contains("if (!TreadShredStore.Ensure().StarterPackOnSale || FindFirstObjectByType<TreadShredStarterOffer>() != null)", offer);
            StringAssert.Contains("OfferLabels.StarterPackLimit + \"  |  \" + OfferLabels.StarterPackCountdown(store.StarterPackTimeLeft)", offer);
            StringAssert.Contains("OfferLabels.StarterPackBuys", offer);
            StringAssert.Contains("\"NOT NOW\"", offer);
            StringAssert.Contains("TreadShredBack.Opened(this, Close);", offer);
            StringAssert.Contains("TreadShredStore.Instance.Release(showStatus);", offer);

            var results = Script("ManagerScore/ManagerScore.cs");
            var actions = results.IndexOf("HighlightNextMission();\n        }\n        // D-037-AC-04", StringComparison.Ordinal);
            Assert.Greater(actions, 0, "after the result screen's own offers are built");
            StringAssert.Contains("TreadShredStarterOffer.ShowIfOnSale(TreadShredUi.FindFont(VictoryofFail));", results);
            var start = Script("LoadLv.cs");
            StringAssert.Contains("TreadShredStarterOffer.ShowIfOnSale(font);", start);
            Assert.Greater(start.IndexOf("TreadShredStarterOffer.ShowIfOnSale(font);", StringComparison.Ordinal),
                start.IndexOf("TreadShredFirstMinute.TakeInstantStart()", StringComparison.Ordinal),
                "never on the first launch's straight-into-battle start (D-035)");
        }

        [Test(Description = "D-037-AC-07: the countdown reads minutes and seconds, never 0:00 while on sale, and when it ends the interstitial closes and the revive panel drops the Starter Pack at once")]
        public void CountdownEndsTheOfferEverywhere()
        {
            Assert.AreEqual("ENDS IN 15:00", OfferLabels.StarterPackCountdown(TimeSpan.FromMinutes(15)));
            Assert.AreEqual("ENDS IN 14:59", OfferLabels.StarterPackCountdown(TimeSpan.FromSeconds(899)));
            Assert.AreEqual("ENDS IN 0:01", OfferLabels.StarterPackCountdown(TimeSpan.FromMilliseconds(200)), "rounded up while on sale");
            Assert.AreEqual("ENDS IN 0:00", OfferLabels.StarterPackCountdown(TimeSpan.Zero));

            var now = Launch.AddMinutes(14).AddSeconds(59);
            var (_, flow, _) = OpenAt(() => now);
            Assert.IsTrue(flow.StarterPackOnSale);
            now = now.AddSeconds(1);
            Assert.IsFalse(flow.StarterPackOnSale, "gone the second the window ends");

            StringAssert.Contains("if (!TreadShredStore.Ensure().StarterPackOnSale)\n        {\n            Close();", Script("Game/TreadShredStarterOffer.cs"));
            StringAssert.Contains("Refresh(null); // D-037-AC-07", Script("Game/TreadShredRevivePanel.cs"));
        }
    }
}
