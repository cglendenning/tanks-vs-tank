using System;
using System.IO;
using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // D-047 (owner direction 2026-11-27): the War Chest can be bought again any time, and once seen it stays one tap away.
    public sealed class WarChestAgainTests
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
            public bool TryStartPurchase(string productId) => true;
        }

        private static readonly DateTime Launch = new DateTime(2026, 11, 27, 9, 0, 0, DateTimeKind.Utc);

        private static GemPack Chest => GemCatalog.WarChest;

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-047-AC-02: buying the War Chest never takes it off sale, and every further paid order credits it in full")]
        public void BuyingItKeepsItOnSale()
        {
            var clock = Launch.AddMinutes(16);
            var wallet = new GemWallet(new JsonWalletStore());
            wallet.OpenStarterPackWindow(Launch);
            var flow = new PurchaseFlow(wallet, new FakeGateway(), () => true, () => clock, () => true);
            Assert.IsTrue(flow.WarChestOnSale);
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Chest.ProductId, "order-1"));
            Assert.IsTrue(flow.WarChestOnSale, "still on sale once bought");
            Assert.AreEqual(CreditResult.Credited, wallet.CreditPurchase(Chest.ProductId, "order-2"));
            Assert.AreEqual(2 * Chest.Gems, wallet.Balance, "a second War Chest credits in full");
            Assert.IsTrue(flow.WarChestOnSale, "and a third is on sale");
        }

        [Test(Description = "D-047-AC-04: the War Chest's header no longer says ONE TIME ONLY, and neither store's product copy promises one time")]
        public void NothingSaysOneTime()
        {
            Assert.AreEqual("ALL OF THIS, ALL AT ONCE", WarChest.Subtitle);
            var apple = Read("../scripts/app-store-connect.py");
            var appleChest = apple.Substring(apple.IndexOf("WAR_CHEST = {", StringComparison.Ordinal));
            appleChest = appleChest.Substring(0, appleChest.IndexOf("}", StringComparison.Ordinal));
            StringAssert.DoesNotContain("one time", appleChest.ToLowerInvariant());
            StringAssert.DoesNotContain("one-time", appleChest.ToLowerInvariant());
            var google = Read("../scripts/play-console.py");
            var googleChest = google.Substring(google.IndexOf("\"title\": \"WAR CHEST\"", StringComparison.Ordinal));
            googleChest = googleChest.Substring(0, googleChest.IndexOf("}", StringComparison.Ordinal));
            StringAssert.DoesNotContain("ONE TIME", googleChest);
        }

        [Test(Description = "D-047-AC-08: having seen the War Chest is saved with the wallet once and restored from a cloud save")]
        public void SeenIsSavedAndRestored()
        {
            var store = new JsonWalletStore();
            var wallet = new GemWallet(store);
            Assert.IsFalse(wallet.WarChestSeen, "a new player has not seen it");
            wallet.MarkWarChestSeen();
            wallet.MarkWarChestSeen();
            Assert.IsTrue(new GemWallet(store).WarChestSeen, "saved with the wallet");
            var restored = new GemWallet(new JsonWalletStore());
            restored.Replace(JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(new WalletState { warChestSeen = true })));
            Assert.IsTrue(restored.WarChestSeen, "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-047-AC-08: a result screen offers the War Chest only once the player has seen it, while it is on sale and outside the ask-free start")]
        public void EveryResultScreenOffersItOnceSeen()
        {
            // On real result screens, opened by a reveal the player saw: PlayMode WarChestResultButtonTests.
            foreach (var askFree in new[] { false, true })
                foreach (var onSale in new[] { false, true })
                    foreach (var seen in new[] { false, true })
                        Assert.AreEqual(!askFree && onSale && seen, WarChest.ShowsOnResultScreen(askFree, onSale, seen),
                            $"ask-free {askFree}, on sale {onSale}, seen {seen}");
        }
    }
}
