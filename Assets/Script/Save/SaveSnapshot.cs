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
        /// <summary>D-025-AC-05, D-044: when the player last reset the game (0 when never); the reset save carries it.</summary>
        public long resetAtTicks;
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
            return Choose(local, cloud, localSynced: true);
        }

        /// <summary>
        /// As Choose, but a device that has never synced with the cloud (a reinstall whose first iCloud
        /// sync has not arrived) does not hold an authoritative save yet: a cloud save with progress
        /// replaces it even if it is newer, so a few seconds of play cannot overwrite a player's history.
        /// </summary>
        public static SaveSource Choose(SaveSnapshot local, SaveSnapshot cloud, bool localSynced)
        {
            // D-025-AC-05: a reset is never undone by a save written before it, on either side.
            if (cloud != null && cloud.resetAtTicks > (local?.writtenAtTicks ?? 0L))
                return SaveSource.Cloud;
            if (cloud != null && local != null && local.resetAtTicks > 0L && cloud.writtenAtTicks <= local.resetAtTicks)
                return SaveSource.Local;
            if (!localSynced && HasProgress(cloud))
                return SaveSource.Cloud;
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
