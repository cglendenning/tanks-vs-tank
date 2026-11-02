using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // The upgrade screen asks before spending and can give an upgrade back until the player leaves it.
    public sealed class UpgradeUndoTests
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
            public bool TryStartPurchase(string productId) => true;
        }

        private GemWallet wallet;
        private PlayerProgression progression;
        private PurchaseFlow flow;
        private UpgradePurchase shop;

        private void Start(int coins, int gems)
        {
            wallet = new GemWallet(new MemoryWalletStore());
            if (gems > 0)
                wallet.CreditReward(gems);
            progression = new PlayerProgression(new MemoryProgressionStore(), wallet);
            progression.Replace(new ProgressionState { coins = coins });
            flow = new PurchaseFlow(wallet, new FakeGateway());
            shop = new UpgradePurchase(progression, wallet, flow, (_, __) => { }, _ => { });
        }

        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-017-AC-05: the upgrade confirmation names the track, the level and everything it spends, and offers a pack when gems fall short")]
        public void ConfirmationNamesWhatIsSpent()
        {
            var coins = ProgressionLabels.UpgradeConfirmation(UpgradeTrack.Armor, 1, new UpgradeQuote(200, 0), 0);
            Assert.AreEqual(("UPGRADE ARMOR?", "LEVEL 1 FOR 200 COINS", "UPGRADE", "CANCEL"),
                (coins.Title, coins.Detail, coins.ConfirmButton, coins.CancelButton));
            var mixed = ProgressionLabels.UpgradeConfirmation(UpgradeTrack.Firepower, 4, new UpgradeQuote(800, 767), 50);
            Assert.AreEqual(("UPGRADE FIREPOWER?", "LEVEL 4 FOR 33 COINS + 39 GEMS", "UPGRADE"),
                (mixed.Title, mixed.Detail, mixed.ConfirmButton));
            var gemsOnly = ProgressionLabels.UpgradeConfirmation(UpgradeTrack.Speed, 1, new UpgradeQuote(200, 200), 10);
            Assert.AreEqual("LEVEL 1 FOR 10 GEMS", gemsOnly.Detail);
        }

        [Test(Description = "D-036-AC-03: only spending gems the player holds waits for the confirmation; a coins upgrade and a store purchase go ahead on the tap")]
        public void OnlyHeldGemsWaitForConfirmation()
        {
            var panel = Script("Game/TreadShredUpgradePanel.cs");
            var path = panel.IndexOf("if (UpgradePurchase.Checkout(quote, balance) != UpgradeCheckout.ConfirmGems)", System.StringComparison.Ordinal);
            var ask = panel.IndexOf("TreadShredConfirmDialog.Show(confirmation, font, () =>", System.StringComparison.Ordinal);
            Assert.Greater(path, 0, "the tap decides its path first");
            Assert.Greater(ask, path, "the confirmation comes only after that check, for gems the player holds");
            Assert.AreEqual(2, panel.Split(new[] { "shop.Buy(" }, System.StringSplitOptions.None).Length - 1,
                "one buy on the tap, one from the confirmation; no other path buys");
        }

        [Test(Description = "D-017-AC-06: undo gives back exactly what an upgrade took, including coins converted from gems")]
        public void UndoGivesBackExactlyWhatWasTaken()
        {
            Start(coins: 33, gems: 20);
            shop.Buy(UpgradeTrack.Armor);
            Assert.AreEqual((1, 13, 11), (progression.Level(UpgradeTrack.Armor), progression.Coins, wallet.Balance));

            var receipt = shop.LastUpgrade.Value;
            Assert.IsTrue(shop.Undo());

            Assert.AreEqual((0, 33, 20), (progression.Level(UpgradeTrack.Armor), progression.Coins, wallet.Balance));
            Assert.IsFalse(shop.CanUndo);
            Assert.AreEqual("ARMOR BACK TO LEVEL 0. 20 COINS AND 9 GEMS RETURNED", ProgressionLabels.Undone(receipt));
        }

        [Test(Description = "D-017-AC-06: undo goes latest first, one upgrade per tap, and the screen's upgrades are final once it closes")]
        public void UndoIsLatestFirstUntilTheScreenCloses()
        {
            Start(coins: 1000, gems: 0);
            shop.Buy(UpgradeTrack.Armor);
            shop.Buy(UpgradeTrack.Firepower);
            shop.Buy(UpgradeTrack.Armor);
            Assert.AreEqual(200, progression.Coins);
            CollectionAssert.AreEqual(new[] { (UpgradeTrack.Armor, 1), (UpgradeTrack.Firepower, 1), (UpgradeTrack.Armor, 2) },
                new List<UpgradeReceipt>(shop.Kept).ConvertAll(r => (r.Track, r.Level)));

            Assert.IsTrue(shop.Undo());
            Assert.AreEqual((1, 600), (progression.Level(UpgradeTrack.Armor), progression.Coins));
            Assert.IsTrue(shop.Undo());
            Assert.AreEqual((0, 800), (progression.Level(UpgradeTrack.Firepower), progression.Coins));

            shop.Release();
            Assert.IsFalse(shop.CanUndo);
            Assert.IsFalse(shop.Undo(), "upgrades are final once the screen closes");
            Assert.AreEqual((1, 800), (progression.Level(UpgradeTrack.Armor), progression.Coins));
        }

        [Test(Description = "D-017-AC-06: gems bought from the store for an upgrade stay in the wallet when it is undone")]
        public void BoughtGemsStayAfterUndo()
        {
            Start(coins: 0, gems: 0);
            var pack = GemCatalog.Packs[0];
            shop.Buy(UpgradeTrack.Armor);
            flow.OrderDelivered(pack.ProductId, "txn-undo", out _);
            Assert.AreEqual(1, progression.Level(UpgradeTrack.Armor));

            Assert.IsTrue(shop.Undo());

            Assert.AreEqual((0, 0, pack.Gems), (progression.Level(UpgradeTrack.Armor), progression.Coins, wallet.Balance));
        }

        [Test(Description = "D-017-AC-06: an upgrade changed elsewhere since (a cloud restore) is never given back")]
        public void ChangedProgressIsNotGivenBack()
        {
            Start(coins: 1000, gems: 0);
            shop.Buy(UpgradeTrack.Speed);
            progression.Replace(new ProgressionState { coins = 5, upgradeLevels = new[] { 0, 0, 4 } });

            Assert.IsFalse(shop.Undo());
            Assert.IsFalse(shop.CanUndo);
            Assert.AreEqual((4, 5), (progression.Level(UpgradeTrack.Speed), progression.Coins));
        }

        [Test(Description = "D-017-AC-06: only upgrades still standing when the screen closes are reported as purchases")]
        public void OnlyKeptUpgradesAreReported()
        {
            var panel = Script("Game/TreadShredUpgradePanel.cs");
            StringAssert.Contains("foreach (var receipt in shop.Kept)\n                LogPurchase(receipt);\n        shop?.Release();", panel);
            Assert.AreEqual(1, panel.Split(new[] { "GameEvents.UpgradePurchased(" }, System.StringSplitOptions.None).Length - 1,
                "the purchase is reported in one place, when the screen closes");
        }
    }
}
