using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class WarChestTests
    {
        // Round-trips through JsonUtility, as PlayerPrefs and the cloud snapshot do, so a reload sees only saved fields.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private sealed class JsonProgressionStore : IProgressionStore
        {
            private string saved;
            public ProgressionState Load() => saved == null ? null : JsonUtility.FromJson<ProgressionState>(saved);
            public void Save(ProgressionState state) => saved = JsonUtility.ToJson(state);
        }

        private sealed class FakeGateway : IPurchaseGateway
        {
            public bool TryStartPurchase(string productId) => true;
        }

        private static readonly DateTime Launch = new DateTime(2026, 11, 13, 20, 0, 0, DateTimeKind.Utc);

        private static GemPack Chest => GemCatalog.WarChest;

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-047-AC-01: the War Chest is its own consumable product at the 550-gem pack's price, kept apart from the regular packs, holding 15 units of supply (1,500 gems) across every track")]
        public void WarChestMatchesItsRule()
        {
            Assert.AreEqual(("treadshred.warchest", 1500, 499), (Chest.ProductId, Chest.Gems, Chest.UsdCents));
            CollectionAssert.DoesNotContain(GemCatalog.Packs, Chest, "kept apart from R-GEM-PACKS");
            Assert.AreSame(Chest, GemCatalog.FindPack(Chest.ProductId));
            Assert.AreEqual(GemCatalog.Packs[1].UsdCents, Chest.UsdCents, "the regular $4.99 pack's price");
            Assert.AreEqual(15, WarChest.Units);
            Assert.AreEqual(WarChest.Units * SupplyRules.UnitGems, Chest.Gems, "its gems are exactly its supply");
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(SupplyTrack)), WarChest.Items.Select(item => item.Track), "every kind of supply");
            Assert.GreaterOrEqual(WarChest.Units * GemCatalog.Packs[0].UsdCents, 2.9 * Chest.UsdCents, "about three times its price");
            StringAssert.Contains(".Append(GemCatalog.WarChest)", Script("Store/TreadShredStore.cs"));
        }

        [Test(Description = "D-047-AC-02: buying the War Chest ends its offer, survives a reload and a cloud restore, and a second paid order still credits")]
        public void BuyingItEndsTheOfferAndPersists()
        {
            var store = new JsonWalletStore();
            var wallet = new GemWallet(store);
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Chest.ProductId, "order-1"));
            Assert.AreEqual((1500, true), (wallet.Balance, wallet.WarChestBought));
            Assert.IsTrue(new GemWallet(store).WarChestBought, "saved with the wallet");
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Chest.ProductId, "order-2"), "a paid order is always credited");
            Assert.AreEqual(CreditResult.Duplicate, wallet.CreditPurchase(Chest.ProductId, "order-2"));
            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { balance = 50, warChestBought = true })));
            Assert.IsTrue(restored.WarChestBought, "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-047-AC-02: the War Chest is on sale only until bought, only while the store offers its product, and never during the Starter Pack's window")]
        public void OnSaleOnlyAfterTheStarterPackWindow()
        {
            var clock = Launch;
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.OpenStarterPackWindow(Launch);
            var flow = new PurchaseFlow(wallet, new FakeGateway(), () => true, () => clock, () => true);
            Assert.IsTrue(flow.StarterPackOnSale);
            Assert.IsFalse(flow.WarChestOnSale, "never alongside the Starter Pack");
            clock = Launch.AddMinutes(16);
            Assert.IsTrue(flow.WarChestOnSale, "once the Starter Pack's window is over");
            Assert.IsFalse(new PurchaseFlow(wallet, new FakeGateway(), () => true, () => clock).WarChestOnSale,
                "never while the store lacks its product");
            wallet.CreditPurchase(Chest.ProductId, "order-1");
            Assert.IsFalse(flow.WarChestOnSale, "never once bought");
        }

        [Test(Description = "D-047-AC-03: delivering the War Chest turns its gems into supply on every track and unlocks its special weapons in the drawer, keeping the gems the player already had")]
        public void EquippingTurnsItsGemsIntoSupply()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.CreditReward(70);
            var progressionStore = new JsonProgressionStore();
            var progression = new PlayerProgression(progressionStore, wallet);
            Assert.IsFalse(progression.HasUsed(Weapon.Seeker));
            wallet.CreditPurchase(Chest.ProductId, "order-1");
            progression.EquipWarChest();
            Assert.AreEqual(70, wallet.Balance, "the chest's gems all became supply");
            foreach (var item in WarChest.Items)
                Assert.AreEqual(item.Units, progression.Supply(item.Track), item.Track.ToString());
            foreach (var weapon in new[] { Weapon.ArcShot, Weapon.TripleShot, Weapon.Seeker })
            {
                Assert.IsTrue(progression.HasUsed(weapon), weapon + " joins the drawer");
                Assert.IsTrue(progression.Drawer(Weapon.Cannon).Any(entry => entry.Weapon == weapon && entry.State == DrawerState.Rented),
                    weapon + " is usable on a cannon mission now");
            }
            var reloaded = new PlayerProgression(progressionStore, wallet);
            Assert.AreEqual(3, reloaded.Supply(SupplyTrack.Armor), "saved with the progression");
        }

        [Test(Description = "D-047-AC-03: a War Chest whose gems are no longer all there equips what they cover and leaves the rest as gems, so nothing paid for is lost")]
        public void EquippingStopsAtTheGemsHeld()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.CreditReward(450);
            var progression = new PlayerProgression(new JsonProgressionStore(), wallet);
            progression.EquipWarChest();
            Assert.AreEqual(50, wallet.Balance);
            Assert.AreEqual(3, progression.Supply(SupplyTrack.Armor));
            Assert.AreEqual(1, progression.Supply(SupplyTrack.Firepower));
            Assert.AreEqual(0, progression.Supply(SupplyTrack.Seeker));
        }

        [Test(Description = "D-047-AC-04: each item says what it does in the game and its worth at the smallest pack's price, then the total against the price; plain multiples when prices are not in dollars")]
        public void ItemsAreSoldByWhatTheyDo()
        {
            CollectionAssert.AreEqual(new[]
            {
                "+6 ARMOR HITS", "+40% FIREPOWER", "+20% SPEED", "OVERDRIVE: 10+ MISSIONS", "ARC SHOT: 10+ MISSIONS",
                "TRIPLE SHOT: 10+ MISSIONS", "SEEKER: 10+ MISSIONS"
            }, WarChest.Items.Select(WarChest.Line).ToArray());
            CollectionAssert.AreEqual(new[] { "$2.97", "$1.98", "$1.98", "$1.98", "$1.98", "$1.98", "$1.98" },
                WarChest.Items.Select(item => WarChest.Value(item, "$0.99")).ToArray());
            Assert.AreEqual("TOTAL VALUE $14.85", WarChest.TotalValue("$0.99"));
            Assert.AreEqual("YOURS FOR $4.99", WarChest.YoursFor("$4.99"));
            Assert.AreEqual("GET THE WAR CHEST: $4.99", WarChest.BuyButton("$4.99"));
            Assert.AreEqual(string.Empty, WarChest.Value(WarChest.Items[0], "1,09 €"));
            Assert.AreEqual("2X THE GEAR FOR THE PRICE", WarChest.TotalValue("1,09 €"), "never rounds its worth up");
        }

        [Test(Description = "D-047-AC-04: the War Chest opens only over a lost mission's screen, once a session, and NOT NOW and Back wait until every item has landed")]
        public void OfferedAfterALossOnceASession()
        {
            var results = Script("ManagerScore/ManagerScore.cs");
            StringAssert.Contains("if (FailureObj != null && FailureObj.activeSelf)\n            TreadShredWarChestOffer.ShowAfterLoss(", results);
            var offer = Script("Game/TreadShredWarChestOffer.cs");
            StringAssert.Contains("if (ShownThisSession || !TreadShredStore.Ensure().WarChestOnSale", offer);
            StringAssert.Contains("ShownThisSession = true;", offer);
            Assert.Less(offer.IndexOf("notNow.SetActive(true);", StringComparison.Ordinal), offer.IndexOf("Revealed = true;", StringComparison.Ordinal));
            StringAssert.Contains("if (Revealed)\n            Close();", offer);
            StringAssert.Contains("GameEvents.OfferShown(\"war_chest\", store.Wallet.Balance)", offer);
            var store = Script("Store/TreadShredStore.cs");
            Assert.Less(store.IndexOf("Progression.EquipWarChest();", StringComparison.Ordinal),
                store.IndexOf("storeController.ConfirmPurchase(order);", StringComparison.Ordinal), "equipped before the order is confirmed");
        }
    }
}
