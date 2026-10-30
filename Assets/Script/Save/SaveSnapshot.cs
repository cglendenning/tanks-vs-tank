using System;
using TreadShred.Economy;
using TreadShred.Progression;

namespace TreadShred.Save
{
    /// <summary>
    /// D-025-AC-01: everything a player paid for or earned, saved and restored as
    /// one piece so the wallet and progression can never come from different saves.
    /// </summary>
    [Serializable]
    public sealed class SaveSnapshot
    {
        public long writtenAtTicks;
        public WalletState wallet = new WalletState();
        public ProgressionState progression = new ProgressionState();
    }

    public enum SaveSource
    {
        Local,
        Cloud
    }

    public static class SaveRules
    {
        /// <summary>
        /// D-025-AC-02/03: the newer save wins as a whole and balances are never added. A local
        /// save with nothing earned yet (a fresh install, at most the starter gems) gives way to a
        /// cloud save with progress, whenever it arrives, so the starter grant can never stack.
        /// </summary>
        public static SaveSource Choose(SaveSnapshot local, SaveSnapshot cloud)
        {
            if (!HasProgress(cloud))
                return SaveSource.Local;
            if (!HasProgress(local))
                return SaveSource.Cloud;
            return cloud.writtenAtTicks > local.writtenAtTicks ? SaveSource.Cloud : SaveSource.Local;
        }

        /// <summary>True once the player has earned, bought or spent anything beyond the starter grant.</summary>
        public static bool HasProgress(SaveSnapshot snapshot)
        {
            if (snapshot == null)
                return false;
            var wallet = snapshot.wallet;
            if (wallet != null)
            {
                if (wallet.creditedTransactions != null && wallet.creditedTransactions.Count > 0)
                    return true;
                var untouched = wallet.starterGranted ? GemCatalog.StarterGrant : 0;
                if (wallet.balance != untouched)
                    return true;
            }
            var progression = snapshot.progression;
            if (progression == null)
                return false;
            if (progression.coins > 0 || progression.highestUnlockedTier > 0)
                return true;
            if (progression.claimedMilestones != null && progression.claimedMilestones.Count > 0)
                return true;
            foreach (var level in progression.upgradeLevels ?? new int[0])
            {
                if (level > 0)
                    return true;
            }
            foreach (var cleared in progression.clearedMissions ?? new long[0])
            {
                if (cleared != 0)
                    return true;
            }
            return false;
        }
    }
}
