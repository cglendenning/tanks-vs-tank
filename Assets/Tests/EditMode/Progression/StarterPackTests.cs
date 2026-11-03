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

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        private static GemPack Starter => GemCatalog.StarterPack;

        [Test(Description = "D-037-AC-01: the Starter Pack is its own consumable product, 300 gems at the smallest pack's price, kept apart from the regular packs and fetched from the store")]
        public void StarterPackMatchesItsRule()
        {
            Assert.AreEqual(("treadshred.starter300", 300, 99), (Starter.ProductId, Starter.Gems, Starter.UsdCents));
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

            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-1"));
            Assert.AreEqual((300, true), (wallet.Balance, wallet.StarterPackBought));
            Assert.IsTrue(new GemWallet(store).StarterPackBought, "saved with the wallet");
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Starter.ProductId, "order-2"),
                "a paid order is always credited, even a second Starter Pack");
            Assert.AreEqual(600, wallet.Balance);
            Assert.AreEqual(CreditResult.Duplicate, wallet.CreditPurchase(Starter.ProductId, "order-2"));

            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(
                new WalletState { balance = 50, starterPackBought = true, starterPackOffered = true })));
            Assert.AreEqual((true, true), (restored.StarterPackBought, restored.StarterPackOffered), "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-037-AC-02: the Starter Pack is on sale only until it is bought and while the store offers its product; otherwise purchases choose exactly as before")]
        public void OnSaleOnlyWhileUnboughtAndAvailable()
        {
            var available = true;
            var wallet = new GemWallet(new JsonWalletStore());
            var flow = new PurchaseFlow(wallet, new FakeGateway(), () => available);

            Assert.IsTrue(flow.StarterPackOnSale);
            available = false;
            Assert.IsFalse(flow.StarterPackOnSale, "not on sale when the store lacks its product");
            Assert.AreSame(GemCatalog.Packs[0], flow.PlanFor(40).PackToBuy, "every purchase behaves as before");
            Assert.IsFalse(new PurchaseFlow(wallet, new FakeGateway()).StarterPackOnSale, "no store, no sale");
            Assert.AreSame(GemCatalog.Packs[0], GemCatalog.PlanFor(40, 0).PackToBuy, "the plain choice is unchanged");

            available = true;
            wallet.CreditPurchase(Starter.ProductId, "order-1");
            Assert.IsFalse(flow.StarterPackOnSale, "never on sale again once bought");
        }

        [Test(Description = "D-037-AC-04: while on sale every one-tap purchase the Starter Pack covers buys it, never fewer gems for the same price, and the item applies on delivery")]
        public void OneTapPurchasesBuyTheStarterPack()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            var gateway = new FakeGateway();
            var flow = new PurchaseFlow(wallet, gateway, () => true);
            var applied = 0;

            Assert.AreSame(Starter, flow.PlanFor(30).PackToBuy, "a revive");
            Assert.AreSame(Starter, flow.PackCovering(300), "an upgrade's gems up to its 300");
            Assert.AreSame(GemCatalog.Packs[1], flow.PackCovering(301), "past its gems, the 550 pack");

            flow.SpendOrBuy(GemCatalog.Cost(GemOffer.Overdrive), () => applied++, _ => { });
            Assert.AreEqual(new[] { Starter.ProductId }, gateway.Started, "Overdrive opens the Starter Pack's sheet");
            flow.OrderDelivered(Starter.ProductId, "order-1", out _);
            Assert.AreEqual((1, 240), (applied, wallet.Balance), "applied on delivery, the rest stays in the wallet");
            Assert.IsFalse(flow.StarterPackOnSale);
        }

        [Test(Description = "D-037-AC-04: a button that will buy the Starter Pack names its price, the line under it names the pack, and the gem store's slot reads STARTER PACK: 300 GEMS FOR <price>")]
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

        [Test(Description = "D-037-AC-03: the revive panel offers the Starter Pack once, the first time it opens while the pack is on sale, with BUY 300 GEMS FOR <price>, and keeps the revive offer open after it is bought")]
        public void RevivePanelOffersItOnce()
        {
            var revive = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("store.StarterPackOnSale && !store.Wallet.StarterPackOffered", revive);
            StringAssert.Contains("store.Wallet.MarkStarterPackOffered();", revive);
            StringAssert.Contains("OfferLabels.Pack(GemCatalog.StarterPack, store.PriceFor(GemCatalog.StarterPack), \"BUY \")", revive);
            StringAssert.Contains("var plan = store.PlanFor(offer.Cost);", revive);
            Assert.AreEqual("BUY 300 GEMS FOR $0.99", OfferLabels.Pack(Starter, "$0.99", "BUY "));
            Assert.AreEqual("BUY 300 GEMS", OfferLabels.Pack(Starter, null, "BUY "));

            var wallet = new GemWallet(new JsonWalletStore());
            wallet.MarkStarterPackOffered();
            Assert.AreEqual((true, 0), (wallet.StarterPackOffered, wallet.Balance), "showing it records the offer and moves no gems");
        }
    }
}
