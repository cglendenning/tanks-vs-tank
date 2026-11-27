using System;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    // A new player's first five missions ask for nothing (D-050).
    public sealed class AskFreeStartTests
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

        private static PlayerProgression Fresh() =>
            new PlayerProgression(new MemoryProgressionStore(), new GemWallet(new MemoryWalletStore()));

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [Test(Description = "D-050-AC-01: the start is ask-free until mission 5 is first cleared on Recruit")]
        public void AskFreeUntilMissionFiveIsFirstCleared()
        {
            var progression = Fresh();
            Assert.IsTrue(AskFreeStart.InProgress(progression), "a first install");
            for (var mission = 1; mission < AskFreeStart.LastMission; mission++)
            {
                progression.CompleteMission(DifficultyTier.Recruit, mission, won: true);
                Assert.IsTrue(AskFreeStart.InProgress(progression), "after mission " + mission);
            }
            progression.CompleteMission(DifficultyTier.Recruit, AskFreeStart.LastMission, won: false);
            Assert.IsTrue(AskFreeStart.InProgress(progression), "losing mission 5 does not end it");
            progression.CompleteMission(DifficultyTier.Recruit, AskFreeStart.LastMission, won: true);
            Assert.IsFalse(AskFreeStart.InProgress(progression), "mission 5's first clear ends it");
            Assert.AreEqual(5, AskFreeStart.LastMission);
        }

        [Test(Description = "D-050-AC-02: once mission 5 is cleared, replays of missions 1 to 5 are not ask-free")]
        public void ReplaysAfterTheStartAskAsForAnyPlayer()
        {
            var progression = Fresh();
            for (var mission = 1; mission <= AskFreeStart.LastMission; mission++)
                progression.CompleteMission(DifficultyTier.Recruit, mission, won: true);
            progression.CompleteMission(DifficultyTier.Recruit, 1, won: true);
            progression.CompleteMission(DifficultyTier.Recruit, 3, won: false);
            Assert.IsFalse(AskFreeStart.InProgress(progression), "replaying mission 1 or losing mission 3 never reopens it");
        }

        [Test(Description = "D-050-AC-03: read from progress alone: restored progress past mission 5 never gets it, and a reset brings it back")]
        public void ReadFromProgressAlone()
        {
            var progression = Fresh();
            var restored = new ProgressionState();
            restored.clearedMissions[(int)DifficultyTier.Recruit] = (1L << AskFreeStart.LastMission) - 1;
            progression.Replace(restored);
            Assert.IsTrue(progression.IsCleared(DifficultyTier.Recruit, AskFreeStart.LastMission), "missions 1 to 5 restored from a cloud save");
            Assert.IsFalse(AskFreeStart.InProgress(progression), "a returning player is not ask-free");
            progression.Replace(new ProgressionState());
            Assert.IsTrue(AskFreeStart.InProgress(progression), "a reset (D-044) is a fresh start, ask-free again");
        }

        [Test(Description = "D-050-AC-05: the editor's tests never get the ask-free start unless a PlayMode test turns it on")]
        public void TheEditorSkipsTheAskFreeStart()
        {
            Assert.IsFalse((bool)TreadShredAskFreeStart.InProgress, "the screen tests expect today's offers");
            Assert.IsFalse((bool)TreadShredAskFreeStart.MissionAskFree);
            Assert.IsNotNull(GameType("TreadShredAskFreeStart").GetField("editorPreview", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
                "a PlayMode test can turn it on");
        }
    }
}
