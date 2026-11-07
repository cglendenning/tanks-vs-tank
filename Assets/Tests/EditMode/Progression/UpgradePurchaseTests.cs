using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // The upgrade screen's purchase path, including the defect where leaving the screen while the store sheet
    // was still opening let a later delivery spend coins and gems on an upgrade the player had walked away from.
    public sealed class UpgradePurchaseTests
    {
        private sealed class MemoryProgressionStore : IProgressionStore
        {
            private ProgressionState saved;
            public ProgressionState Load() => saved;
            public void Save(ProgressionState state) => saved = state;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            private WalletState saved;
            public WalletState Load() => saved;
            public void Save(WalletState state) => saved = state;
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

        private GemWallet wallet;
        private PlayerProgression progression;
        private PurchaseFlow flow;
        private FakeGateway gateway;
        private List<string> statuses;
        private List<UpgradeTrack> upgraded;
        private UpgradePurchase shop;

        // No coins and no gems: the first armor level (200 coins) needs 10 gems, so a tap opens the store for
        // the smallest pack.
        [SetUp]
        public void Broke()
        {
            wallet = new GemWallet(new MemoryWalletStore());
            progression = new PlayerProgression(new MemoryProgressionStore(), wallet);
            gateway = new FakeGateway();
            flow = new PurchaseFlow(wallet, gateway);
            statuses = new List<string>();
            upgraded = new List<UpgradeTrack>();
            shop = new UpgradePurchase(progression, wallet, flow, (track, _) => upgraded.Add(track), statuses.Add);
        }

        private static GemPack SmallPack => GemCatalog.Packs[0];

        private void AssertNothingChanged(int gems)
        {
            Assert.AreEqual(0, progression.Coins, "coins");
            Assert.AreEqual(gems, wallet.Balance, "gems");
            Assert.AreEqual(0, progression.Level(UpgradeTrack.Armor), "armor level");
            CollectionAssert.IsEmpty(upgraded);
        }

        [Test(Description = "D-011-AC-04: cancelling the gem purchase behind an upgrade leaves coins, gems and the level unchanged")]
        public void CancelledPurchaseChangesNothing()
        {
            shop.Buy(UpgradeTrack.Armor);
            Assert.AreEqual(new[] { SmallPack.ProductId }, gateway.Started);
            Assert.AreEqual(new[] { PurchaseMessages.Opening }, statuses, "the screen reacts at once while the sheet opens");

            flow.PurchaseFailed(cancelledByPlayer: true);

            Assert.AreEqual(new[] { PurchaseMessages.Opening, PurchaseMessages.Cancelled }, statuses);
            AssertNothingChanged(0);
            Assert.IsFalse(flow.InProgress, "the player can try again");
        }

        [Test(Description = "D-011-AC-04: leaving the upgrade screen while the sheet opens detaches the purchase, so cancelling it changes nothing and reaches no closed screen")]
        public void LeavingThenCancellingChangesNothing()
        {
            shop.Buy(UpgradeTrack.Armor);
            shop.Release();
            flow.PurchaseFailed(cancelledByPlayer: true);

            Assert.AreEqual(new[] { PurchaseMessages.Opening }, statuses, "no status reaches the closed screen");
            AssertNothingChanged(0);
            Assert.IsFalse(flow.InProgress);
        }

        [Test(Description = "D-011-AC-04: a purchase completed after the player left the upgrade screen credits its gems and spends nothing on an upgrade")]
        public void LeavingThenPayingCreditsGemsOnly()
        {
            shop.Buy(UpgradeTrack.Armor);
            shop.Release();

            Assert.AreEqual(OrderOutcome.Confirm, flow.OrderDelivered(SmallPack.ProductId, "txn-after-leaving", out var result));

            Assert.AreEqual(CreditResult.Credited, result);
            AssertNothingChanged(SmallPack.Gems);
        }

        [Test(Description = "D-010-AC-02: the pack bought for an upgrade completes it when delivered, with no second tap")]
        public void DeliveredPackCompletesTheUpgrade()
        {
            shop.Buy(UpgradeTrack.Armor);
            flow.OrderDelivered(SmallPack.ProductId, "txn-upgrade", out _);

            Assert.AreEqual(new[] { UpgradeTrack.Armor }, upgraded);
            Assert.AreEqual(1, progression.Supply(SupplyTrack.Armor), "a unit of armor supply (D-045)");
            Assert.AreEqual(0, progression.Coins);
            Assert.AreEqual(SmallPack.Gems - SupplyRules.UnitGems, wallet.Balance);
            Assert.AreEqual(1, gateway.Started.Count, "one tap, one store sheet");
        }

        [Test(Description = "D-011-AC-03: an order replayed while an upgrade purchase waits is credited once and neither completes nor repeats that purchase")]
        public void ReplayedOrderIsNotTheWaitingPurchase()
        {
            wallet.CreditPurchase(SmallPack.ProductId, "txn-earlier");
            var gems = wallet.Balance;
            wallet.TrySpend(gems);

            shop.Buy(UpgradeTrack.Armor);
            flow.OrderDelivered(SmallPack.ProductId, "txn-earlier", out var replay);
            flow.OrderDelivered(GemCatalog.Packs[2].ProductId, "txn-other-pack", out var other);

            Assert.AreEqual(CreditResult.Duplicate, replay);
            Assert.AreEqual(CreditResult.Credited, other);
            CollectionAssert.IsEmpty(upgraded, "neither order is the purchase this tap opened");
            Assert.IsTrue(flow.InProgress, "the purchase still waits for its own outcome");
            Assert.AreEqual(1, gateway.Started.Count, "no second store sheet");
            Assert.AreEqual(GemCatalog.Packs[2].Gems, wallet.Balance);
        }

        [Test(Description = "D-011-AC-04: every screen that buys gems locks its buy buttons while the sheet is open and releases its purchase when it closes")]
        public void ScreensReleasePurchasesWhenClosed()
        {
            string Script(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));
            var upgrades = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("shop?.Release();", upgrades);
            StringAssert.Contains("!TreadShredStore.Ensure().PurchaseInProgress", upgrades);
            var revive = Script("Game/TreadShredRevivePanel.cs");
            StringAssert.Contains("TreadShredStore.Instance.Release(showStatus);", revive);
            StringAssert.Contains("reviveButton.interactable = !store.PurchaseInProgress;", revive);
            StringAssert.Contains("TreadShredStore.Instance.Release(showStatus);", Script("TreadShredSettingsPanel.cs"));
            StringAssert.Contains("rewardButton.interactable = !store.PurchaseInProgress;", Script("ManagerScore/ManagerScore.cs"));
        }
    }
}
