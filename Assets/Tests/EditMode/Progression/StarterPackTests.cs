using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private sealed class MemoryProgressionStore : IProgressionStore
        {
            public ProgressionState Saved;
            public ProgressionState Load() => Saved;
            public void Save(ProgressionState state) => Saved = state;
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

        [Test(Description = "D-037-AC-01: the Starter Pack is its own consumable product at the smallest pack's price for 15 minutes, its gems exactly its 6 units of supply, kept apart from the regular packs and fetched from the store")]
        public void StarterPackMatchesItsRule()
        {
            Assert.AreEqual(("treadshred.starter300", 600, 99), (Starter.ProductId, Starter.Gems, Starter.UsdCents));
            Assert.AreEqual(TimeSpan.FromMinutes(15), GemCatalog.StarterPackWindow, "R-STARTER-PACK offer_minutes");
            CollectionAssert.DoesNotContain(GemCatalog.Packs, Starter, "kept apart from R-GEM-PACKS");
            Assert.AreSame(Starter, GemCatalog.FindPack(Starter.ProductId));
            Assert.AreEqual(StarterPackPitch.Stack.Units * SupplyRules.UnitGems, Starter.Gems, "its gems are exactly its units");
            Assert.AreEqual(6, StarterPackPitch.Stack.Units, "R-STARTER-PACK units: six times the smallest pack");
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
            Assert.AreEqual((600, true), (wallet.Balance, wallet.StarterPackBought));
            Assert.AreEqual(TimeSpan.Zero, wallet.StarterPackTimeLeft(Launch.AddMinutes(1)), "bought: no time left to buy it");
            Assert.IsTrue(new GemWallet(store).StarterPackBought, "saved with the wallet");
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-2"),
                "a paid order is always credited, even a second Starter Pack");
            Assert.AreEqual(1200, wallet.Balance);
            Assert.AreEqual(CreditResult.Duplicate, wallet.CreditPurchase(Starter.ProductId, "order-2"));

            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { balance = 50, starterPackBought = true })));
            Assert.IsTrue(restored.StarterPackBought, "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-037-AC-01: a delivered Starter Pack's gems become its supply on every track before the order is confirmed and Seeker joins the drawer, as the War Chest's do; a unit the gems no longer cover stays as gems")]
        public void DeliveryEquipsTheStack()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            var progression = new PlayerProgression(new MemoryProgressionStore(), wallet);
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-1"));
            progression.EquipStarterPack();
            foreach (var item in StarterPackPitch.Stack.Items)
                Assert.AreEqual(item.Units, progression.Supply(item.Track), item.Track.ToString());
            Assert.IsTrue(progression.HasUsed(Weapon.Seeker), "Seeker joins the drawer");
            Assert.AreEqual(0, wallet.Balance, "every gem became supply");

            var short250 = new GemWallet(new JsonWalletStore());
            short250.Replace(new WalletState { balance = 250, starterGranted = true });
            var partial = new PlayerProgression(new MemoryProgressionStore(), short250);
            partial.EquipStarterPack();
            Assert.AreEqual((2, 0, 50), (partial.Supply(SupplyTrack.Armor), partial.Supply(SupplyTrack.Firepower), short250.Balance),
                "the units the gems cover, the rest stays as gems");

            var store = Script("Store/TreadShredStore.cs");
            Assert.Less(store.IndexOf("Progression.EquipStarterPack();", StringComparison.Ordinal),
                store.IndexOf("storeController.ConfirmPurchase(order);", StringComparison.Ordinal), "equipped before the order is confirmed");
        }

        [Test(Description = "D-037-AC-08: while the Starter Pack is test driven its supply and Seeker count in the mission, nothing is saved or used up, ending the drive takes them away, the drive is logged as starter_pack_test_drive, and its end goes on to the next mission after a victory")]
        public void TestDriveTheStack()
        {
            var saved = new MemoryProgressionStore();
            var progression = new PlayerProgression(saved, new GemWallet(new JsonWalletStore()));
            progression.StartTestDrive(StarterPackPitch.Stack);
            Assert.IsTrue(progression.TestDriving);
            foreach (var item in StarterPackPitch.Stack.Items)
                Assert.AreEqual(item.Units, progression.Supply(item.Track), item.Track.ToString());
            Assert.IsTrue(progression.HasUsed(Weapon.Seeker));
            Assert.IsFalse(progression.HasUsed(Weapon.ArcShot), "only the pack's own weapon");
            Assert.AreEqual(0, new PlayerProgression(saved, new GemWallet(new JsonWalletStore())).Supply(SupplyTrack.Armor), "never saved");
            progression.EndTestDrive();
            Assert.AreEqual((0, false), (progression.Supply(SupplyTrack.Armor), progression.HasUsed(Weapon.Seeker)), "gone with the drive");

            Assert.AreEqual(("starter_pack", "ALL STARTER PACK GEAR ON", "THAT WAS THE STARTER PACK. KEEP ALL OF IT"),
                (StarterPackPitch.Stack.Offer, StarterPackPitch.Stack.TestDriveNote, StarterPackPitch.Stack.AfterDriveSubtitle));
            StringAssert.Contains("GameEvents.OfferAccepted(stack.Offer + \"_test_drive\", 0, false)", Script("Game/TreadShredTestDrive.cs"));
            StringAssert.Contains("TreadShredTestDrive.Begin(driveFont, StarterPackPitch.Stack, ShowAfterTestDrive, won ? TreadShredCampaign.Next : (System.Action)null);",
                Script("Game/TreadShredStarterOffer.cs"));
            StringAssert.Contains("if (TreadShredTestDrive.Active)\n            return false;", Script("Game/TreadShredRevivePanel.cs"), "no revive during a drive");
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

        [Test(Description = "D-037-AC-04: while on sale a one-tap purchase short of gems buys a regular pack, never the Starter Pack, which holds supply rather than gems to spend, and the item applies on delivery")]
        public void OneTapPurchasesBuyARegularPack()
        {
            var (wallet, flow, gateway) = OpenAt(() => Launch.AddMinutes(1));
            var applied = 0;

            Assert.IsTrue(flow.StarterPackOnSale);
            Assert.AreSame(GemCatalog.Packs[0], flow.PlanFor(30).PackToBuy, "a revive");
            Assert.AreSame(GemCatalog.Packs[0], flow.PackCovering(100), "an upgrade's gems up to 100");
            Assert.AreSame(GemCatalog.Packs[1], flow.PackCovering(101), "past 100, the 550 pack");

            flow.SpendOrBuy(GemCatalog.Cost(GemOffer.Overdrive), () => applied++, _ => { });
            Assert.AreEqual(new[] { GemCatalog.Packs[0].ProductId }, gateway.Started, "Overdrive opens the smallest pack's sheet");
            flow.OrderDelivered(GemCatalog.Packs[0].ProductId, "order-1", out _);
            Assert.AreEqual((1, 0), (applied, wallet.Balance), "applied on delivery (Overdrive is 100 gems, D-045)");
            Assert.IsTrue(flow.StarterPackOnSale, "the Starter Pack is still on sale");
            Assert.AreSame(Starter, GemCatalog.PackAlongside(true, true), "sold as itself beside a covered offer (D-040-AC-05)");
        }

        [Test(Description = "D-037-AC-04: no button short of gems names the Starter Pack, the gem store's slot reads STARTER PACK: <armor> + MORE: <price>, and buying it there or beside NEXT MISSION reads STARTER PACK EQUIPPED")]
        public void LabelsNameTheStarterPack()
        {
            Func<GemPack, string> priceFor = pack => pack.ProductId;

            Assert.AreEqual("YOU HAVE 0 GEMS. NEED 30", OfferLabels.BalanceLine(0, GemCatalog.PlanFor(30, 0)));
            Assert.AreEqual("UPGRADE: treadshred.gems100", ProgressionLabels.UpgradeButton(new UpgradeQuote(200, 100), false, 0, priceFor));
            Assert.AreEqual("STARTER PACK: TRIPLE ARMOR + MORE: $0.99", StarterPackPitch.StoreButton(2, "$0.99"));
            Assert.AreEqual("STARTER PACK: DOUBLE ARMOR + MORE", StarterPackPitch.StoreButton(4, null));
            Assert.AreEqual(("STARTER PACK EQUIPPED", "+100 GEMS ADDED"), (OfferLabels.GemsAdded(Starter), OfferLabels.GemsAdded(GemCatalog.Packs[0])));

            var settings = Script("TreadShredSettingsPanel.cs");
            Assert.AreSame(GemCatalog.StarterPack, GemCatalog.StorePack(0, starterOnSale: true), "the Starter Pack takes the smallest pack's slot");
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.StorePack(0, starterOnSale: false), "the smallest pack returns once it is off sale");
            Assert.AreSame(GemCatalog.Packs[1], GemCatalog.StorePack(1, starterOnSale: true), "the other slots keep their packs");
            StringAssert.Contains("StarterPackPitch.StoreButton(TreadShredProgress.Ensure().ArmorHits, price)", settings);
            StringAssert.Contains("() => statusText.text = OfferLabels.GemsAdded(pack),", settings);
            StringAssert.DoesNotContain("StarterPackOnSale", Script("Game/TreadShredUpgradePanel.cs"));
        }

        [Test(Description = "D-037-AC-03: while on sale the revive panel leads with STARTER PACK, REVIVE FREE + the armor it adds, PLUS the rest of the pack, the countdown and REVIVE + STARTER PACK: <price>, which revives without spending gems; the plain revive shows only when gems cover it")]
        public void RevivePanelLeadsWithTheStarterPack()
        {
            Assert.AreEqual("REVIVE FREE + TRIPLE YOUR ARMOR", StarterPackPitch.ReviveHeadline(2));
            Assert.AreEqual("PLUS FIREPOWER, SPEED, OVERDRIVE AND SEEKER", StarterPackPitch.ReviveAlternative);
            Assert.AreEqual("REVIVE + STARTER PACK: $0.99", StarterPackPitch.ReviveButton("$0.99"));
            Assert.AreEqual("REVIVE + STARTER PACK", StarterPackPitch.ReviveButton(null));

            var revive = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("starterLayout = store.StarterPackOnSale;", revive);
            StringAssert.Contains("StarterPackPitch.ReviveButton(store.PriceFor(GemCatalog.StarterPack))", revive);
            StringAssert.Contains("reviveButton.gameObject.SetActive(!starterShown || plan.CanSpendNow);", revive);
            // Bought here: the revive applies on delivery and spends nothing (no SpendOrBuy, no TrySpend).
            var buy = revive.Substring(revive.IndexOf("private void BuyStarter()", StringComparison.Ordinal));
            buy = buy.Substring(0, buy.IndexOf("private void Update()", StringComparison.Ordinal));
            StringAssert.Contains("BuyPack(GemCatalog.StarterPack", buy);
            StringAssert.Contains("player.Revive();", buy);
            StringAssert.DoesNotContain("SpendOrBuy", buy);
            StringAssert.DoesNotContain("TrySpend", buy);
        }

        [Test(Description = "D-037-AC-04: the Starter Pack interstitial opens over every result screen and on the start screen while on sale and reveals its stack as the War Chest's: ONE TIME ONLY and the countdown, each item with its worth, SEEKER UNLOCKS NOW, the total, YOURS FOR, GET THE STARTER PACK and NOT NOW, which with Back waits for the reveal")]
        public void InterstitialOpensAtEveryBreak()
        {
            // Sold by what it does in the game (owner direction 2026-11-07), every number from the rules.
            Assert.AreEqual(("TRIPLE YOUR ARMOR", "DOUBLE YOUR ARMOR", "+4 ARMOR HITS"),
                (StarterPackPitch.Headline(2), StarterPackPitch.Headline(4), StarterPackPitch.Headline(5)), "only as true as the tank's own armor");
            var stack = StarterPackPitch.Stack;
            CollectionAssert.AreEqual(new[] { "+4 ARMOR HITS", "+20% FIREPOWER", "+10% SPEED", "OVERDRIVE: 5+ MISSIONS", "SEEKER: 5+ MISSIONS" },
                stack.Items.Select(SupplyStack.Line), "R-STARTER-PACK units: armor 2, firepower, speed, overdrive and seeker 1");
            CollectionAssert.AreEqual(new[] { "$1.98", "$0.99", "$0.99", "$0.99", "$0.99" }, stack.Items.Select(item => SupplyStack.Value(item, "$0.99")));
            Assert.AreEqual("SEEKER UNLOCKS NOW", stack.WeaponsNote);
            Assert.AreEqual(("TOTAL VALUE $5.94", "6X THE GEAR FOR THE PRICE"), (stack.TotalValue("$0.99"), stack.TotalValue("0,99 €")));
            Assert.AreEqual(("YOURS FOR $0.99", "GET THE STARTER PACK: $0.99", "GET THE STARTER PACK"),
                (SupplyStack.YoursFor("$0.99"), stack.BuyButton("$0.99"), stack.BuyButton(null)));

            var offer = Script("Game/TreadShredStarterOffer.cs");
            StringAssert.Contains("public sealed class TreadShredStarterOffer : TreadShredStackOffer", offer);
            StringAssert.Contains("SubtitleText.text = limit + \"  |  \" + OfferLabels.StarterPackCountdown(TreadShredStore.Ensure().StarterPackTimeLeft);", offer);
            StringAssert.Contains("protected override SupplyStack Stack => StarterPackPitch.Stack;", offer);
            // The reveal the War Chest and the Starter Pack share.
            var reveal = Script("Game/TreadShredWarChestOffer.cs");
            StringAssert.Contains("\"NOT NOW\"", reveal);
            StringAssert.Contains("if (Revealed)\n            Close();", reveal);
            StringAssert.Contains("TreadShredStore.Instance.Release(showStatus);", reveal);

            var results = Script("ManagerScore/ManagerScore.cs");
            var actions = results.IndexOf("HighlightNextMission();\n        }\n        // D-037-AC-04", StringComparison.Ordinal);
            Assert.Greater(actions, 0, "after the result screen's own offers are built");
            StringAssert.Contains("TreadShredStarterOffer.ShowOverResult(TreadShredUi.FindFont(VictoryofFail), won);", results);
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

            StringAssert.Contains("protected override bool OnSale => TreadShredStore.Ensure().StarterPackOnSale;", Script("Game/TreadShredStarterOffer.cs"));
            StringAssert.Contains("if (!OnSale)\n        {\n            Close();", Script("Game/TreadShredWarChestOffer.cs"));
            StringAssert.Contains("Refresh(null); // D-037-AC-07", Script("Game/TreadShredRevivePanel.cs"));
        }
    }
}
