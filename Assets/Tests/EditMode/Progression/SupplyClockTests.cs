using NUnit.Framework;
using TreadShred.Economy;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    // Every held track of supply runs its own clock and loses a unit every 5 missions won, all at once (D-045-AC-02,
    // owner decision 2026-11-15), so a one-time pack lasts what its screen says.
    public sealed class SupplyClockTests
    {
        private sealed class MemoryProgressionStore : IProgressionStore
        {
            public ProgressionState Saved;
            public ProgressionState Load() => Saved;
            public void Save(ProgressionState state) => Saved = state;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            public WalletState Saved;
            public WalletState Load() => Saved;
            public void Save(WalletState state) => Saved = state;
        }

        private static PlayerProgression Fresh(GemWallet wallet = null) =>
            new PlayerProgression(new MemoryProgressionStore(), wallet ?? new GemWallet(new MemoryWalletStore()));

        private static void Win(PlayerProgression progression, int missions)
        {
            for (var mission = 0; mission < missions; mission++)
                progression.SupplyMissionWon();
        }

        [Test(Description = "D-045-AC-02: every track held runs down at once: armor x2, firepower x1 and Overdrive x1 last 10, 5 and 5 missions won")]
        public void HeldTracksRunDownTogether()
        {
            var progression = Fresh();
            progression.AddSupply(SupplyTrack.Armor);
            progression.AddSupply(SupplyTrack.Armor);
            progression.AddSupply(SupplyTrack.Firepower);
            progression.AddSupply(SupplyTrack.Overdrive);
            Assert.AreEqual((10, 5, 5), (progression.SupplyMissionsLeft(SupplyTrack.Armor),
                progression.SupplyMissionsLeft(SupplyTrack.Firepower), progression.SupplyMissionsLeft(SupplyTrack.Overdrive)));
            Win(progression, 4);
            Assert.AreEqual((2, 1, 1), (progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Firepower),
                progression.Supply(SupplyTrack.Overdrive)), "nothing used before the fifth");
            Win(progression, 1);
            Assert.AreEqual((1, 0, 0), (progression.Supply(SupplyTrack.Armor), progression.Supply(SupplyTrack.Firepower),
                progression.Supply(SupplyTrack.Overdrive)), "the fifth uses a unit of every track held");
            Win(progression, 5);
            Assert.AreEqual(0, progression.Supply(SupplyTrack.Armor), "all used up after 10");
        }

        [Test(Description = "D-045-AC-02: a unit bought part-way through another track's count still lasts its full 5 missions won; a unit added to a track held adds 5")]
        public void EveryUnitLastsItsFullFive()
        {
            var progression = Fresh();
            progression.AddSupply(SupplyTrack.Armor);
            Win(progression, 3);
            progression.AddSupply(SupplyTrack.Seeker);
            Assert.AreEqual((2, 5), (progression.SupplyMissionsLeft(SupplyTrack.Armor), progression.SupplyMissionsLeft(SupplyTrack.Seeker)));
            progression.AddSupply(SupplyTrack.Armor);
            Assert.AreEqual(7, progression.SupplyMissionsLeft(SupplyTrack.Armor), "5 more on top of the 2 left");
            Win(progression, 5);
            Assert.AreEqual((2, 0), (progression.SupplyMissionsLeft(SupplyTrack.Armor), progression.Supply(SupplyTrack.Seeker)));
            Win(progression, 2);
            Assert.AreEqual(0, progression.Supply(SupplyTrack.Armor));
            progression.AddSupply(SupplyTrack.Armor);
            Assert.AreEqual(5, progression.SupplyMissionsLeft(SupplyTrack.Armor), "a track held empty starts its clock over");
        }

        [Test(Description = "D-045-AC-02: the War Chest lasts what its screen says, 10 missions won per item and 15 for its armor, and the Starter Pack 5, 10 for its armor")]
        public void OneTimePacksLastWhatTheySay()
        {
            var chestWallet = new GemWallet(new MemoryWalletStore());
            chestWallet.Replace(new WalletState { balance = WarChest.Units * SupplyRules.UnitGems, starterGranted = true });
            var chest = Fresh(chestWallet);
            chest.EquipWarChest();
            Assert.AreEqual(15, chest.SupplyMissionsLeft(SupplyTrack.Armor));
            foreach (var item in WarChest.Items)
                if (item.Track != SupplyTrack.Armor)
                    Assert.AreEqual(10, chest.SupplyMissionsLeft(item.Track), item.Track.ToString());
            Win(chest, 10);
            Assert.AreEqual((1, 0), (chest.Supply(SupplyTrack.Armor), chest.Supply(SupplyTrack.Seeker)));
            Assert.AreEqual(0, chest.RentalLeft(Weapon.ArcShot), "the weapons relock after 10");

            var starterWallet = new GemWallet(new MemoryWalletStore());
            starterWallet.Replace(new WalletState { balance = StarterPackPitch.Stack.Units * SupplyRules.UnitGems, starterGranted = true });
            var starter = Fresh(starterWallet);
            starter.EquipStarterPack();
            Assert.AreEqual((10, 5, 5, 5, 5), (starter.SupplyMissionsLeft(SupplyTrack.Armor), starter.SupplyMissionsLeft(SupplyTrack.Firepower),
                starter.SupplyMissionsLeft(SupplyTrack.Speed), starter.SupplyMissionsLeft(SupplyTrack.Overdrive),
                starter.SupplyMissionsLeft(SupplyTrack.Seeker)));
        }

        [Test(Description = "D-045-AC-06: a save from before per-track clocks loads with every clock at zero, and the clocks come back with a restored save")]
        public void ClocksSurviveSavesAndOldSavesLoad()
        {
            var old = JsonUtility.FromJson<ProgressionState>("{\"supply\":[1,0,0,2,0,0,0],\"supplyWins\":3,\"supplyNext\":3}");
            var progression = Fresh();
            progression.Replace(old);
            Assert.AreEqual((5, 10), (progression.SupplyMissionsLeft(SupplyTrack.Armor), progression.SupplyMissionsLeft(SupplyTrack.Overdrive)));

            Win(progression, 2);
            var store = new MemoryProgressionStore();
            var restored = new PlayerProgression(store, new GemWallet(new MemoryWalletStore()));
            restored.Replace(JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(GetState(progression))));
            Assert.AreEqual((3, 8), (restored.SupplyMissionsLeft(SupplyTrack.Armor), restored.SupplyMissionsLeft(SupplyTrack.Overdrive)));
        }

        private static ProgressionState GetState(PlayerProgression progression) =>
            progression.state;
    }
}
