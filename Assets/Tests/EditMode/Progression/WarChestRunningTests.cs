using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // D-047-AC-02 (owner direction 2026-11-27): once bought, the War Chest is not offered again until everything in it
    // has been used up; then it can be bought again.
    public sealed class WarChestRunningTests
    {
        // Round-trip through JsonUtility, as PlayerPrefs and the cloud snapshot do.
        private sealed class JsonWalletStore : IWalletStore
        {
            private string saved;
            public WalletState Load() => saved == null ? null : JsonUtility.FromJson<WalletState>(saved);
            public void Save(WalletState state) => saved = JsonUtility.ToJson(state);
        }

        private sealed class JsonProgressionStore : IProgressionStore
        {
            public string Saved;
            public ProgressionState Load() => Saved == null ? null : JsonUtility.FromJson<ProgressionState>(Saved);
            public void Save(ProgressionState state) => Saved = JsonUtility.ToJson(state);
        }

        private static bool Empty(PlayerProgression progression)
        {
            foreach (var item in WarChest.Items)
            {
                if (progression.Supply(item.Track) > 0)
                    return false;
            }
            return true;
        }

        [Test(Description = "D-047-AC-02: a bought War Chest runs until every supply track it filled is used up, and only then is it back on sale")]
        public void RunsUntilEverythingInItIsUsedUp()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            var progression = new PlayerProgression(new JsonProgressionStore(), wallet);
            Assert.IsFalse(progression.WarChestRunning, "a new player has none running");

            wallet.CreditPurchase(GemCatalog.WarChest.ProductId, "order-1");
            progression.EquipWarChest();
            Assert.IsTrue(progression.WarChestRunning, "running the moment it is bought");
            Assert.IsFalse(WarChest.HasRunOut(CurrentSupply(progression)), "its gear is equipped");

            var wins = 0;
            while (!Empty(progression) && wins < 1000)
            {
                Assert.IsTrue(progression.WarChestRunning, $"still running after {wins} wins while its gear lasts");
                progression.SupplyMissionWon();
                wins++;
            }
            Assert.IsTrue(Empty(progression), "its gear runs out");
            Assert.IsFalse(progression.WarChestRunning, "and it can be bought again");
        }

        [Test(Description = "D-047-AC-02: a running War Chest is saved and restored with the progression")]
        public void RunningIsSavedAndRestored()
        {
            var wallet = new GemWallet(new JsonWalletStore());
            var store = new JsonProgressionStore();
            var progression = new PlayerProgression(store, wallet);
            wallet.CreditPurchase(GemCatalog.WarChest.ProductId, "order-1");
            progression.EquipWarChest();
            Assert.IsTrue(new PlayerProgression(store, wallet).WarChestRunning, "saved");

            var restored = new PlayerProgression(new JsonProgressionStore(), new GemWallet(new JsonWalletStore()));
            restored.Replace(JsonUtility.FromJson<ProgressionState>(store.Saved));
            Assert.IsTrue(restored.WarChestRunning, "restored from a cloud save (D-025)");
        }

        [Test(Description = "D-047-AC-02: a War Chest bought before this rule counts as running while its tracks hold supply, and supply bought after it has run out never restarts it")]
        public void AChestBoughtBeforeTheRuleCountsWhileItsGearLasts()
        {
            var walletStore = new JsonWalletStore();
            var wallet = new GemWallet(walletStore);
            wallet.CreditPurchase(GemCatalog.WarChest.ProductId, "order-1");
            var store = new JsonProgressionStore();
            var before = new ProgressionState();
            before.supply[(int)SupplyTrack.Overdrive] = 2;
            store.Save(before);

            var progression = new PlayerProgression(store, wallet);
            Assert.IsTrue(progression.WarChestRunning, "its gear is still in use");

            progression.Replace(new ProgressionState { warChestChecked = true });
            Assert.IsFalse(progression.WarChestRunning, "run out");
            progression.AddSupply(SupplyTrack.Overdrive);
            Assert.IsFalse(new PlayerProgression(store, new GemWallet(walletStore)).WarChestRunning, "other supply never restarts it");
        }

        private static int[] CurrentSupply(PlayerProgression progression)
        {
            var supply = new int[SupplyRules.TrackCount];
            for (var track = 0; track < supply.Length; track++)
                supply[track] = progression.Supply((SupplyTrack)track);
            return supply;
        }
    }
}
