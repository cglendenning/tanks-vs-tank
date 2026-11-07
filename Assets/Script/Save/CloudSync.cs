using System;

namespace TreadShred.Save
{
    /// <summary>The device's own save: the wallet and progression stores plus when they were last written.</summary>
    public interface ILocalSave
    {
        SaveSnapshot Load();

        /// <summary>Replaces the local save with a whole snapshot, its time included.</summary>
        void Write(SaveSnapshot snapshot);

        /// <summary>Records when the local save was last changed by play.</summary>
        void Stamp(long writtenAtTicks);

        /// <summary>True once this install has seen the cloud (read a save from it, or had a write accepted).</summary>
        bool Synced { get; }

        void MarkSynced();
    }

    /// <summary>The platform cloud as the game sees it (iCloud key-value storage on iOS).</summary>
    public interface ICloudSave
    {
        /// <summary>The save this device currently sees in the cloud; null when there is none or it cannot be read.</summary>
        SaveSnapshot Read();

        void Write(SaveSnapshot snapshot);
    }

    /// <summary>
    /// D-025: keeps the local save and the platform cloud in step. Restores the newer
    /// save before the stores load, pushes every change that holds progress, and lets
    /// a newer save that arrives later replace what is in play as a whole.
    /// </summary>
    public sealed class CloudSync
    {
        private readonly ILocalSave local;
        private readonly ICloudSave cloud;
        private readonly Func<long> utcTicks;
        private bool applying;

        public CloudSync(ILocalSave local, ICloudSave cloud, Func<long> utcTicks)
        {
            this.local = local ?? throw new ArgumentNullException(nameof(local));
            this.cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
            this.utcTicks = utcTicks ?? throw new ArgumentNullException(nameof(utcTicks));
        }

        /// <summary>D-025-AC-02: before the wallet and progression load (and the starter grant is considered).</summary>
        public bool RestoreAtLaunch()
        {
            var fromCloud = cloud.Read();
            var restore = SaveRules.Choose(local.Load(), fromCloud, local.Synced) == SaveSource.Cloud;
            if (restore)
                local.Write(fromCloud);
            if (fromCloud != null)
                local.MarkSynced();
            return restore;
        }

        /// <summary>D-025-AC-01: called after every local save. A save with nothing earned yet never overwrites the cloud.</summary>
        public void LocalSaved()
        {
            if (applying)
                return;
            local.Stamp(utcTicks());
            var snapshot = local.Load();
            if (!SaveRules.HasProgress(snapshot))
                return;
            cloud.Write(snapshot);
            // iCloud discards writes made before an install's first sync; one that sticks means the device is synced.
            if (!local.Synced && cloud.Read() != null)
                local.MarkSynced();
        }

        /// <summary>
        /// D-044, D-025-AC-05: the player reset the game. The empty save, stamped as a reset, replaces the local save
        /// and goes to the cloud even though it holds no progress, so every device signed in to the account takes it.
        /// </summary>
        public void Reset()
        {
            var now = utcTicks();
            var fresh = new SaveSnapshot { writtenAtTicks = now, resetAtTicks = now };
            applying = true;
            try
            {
                local.Write(fresh);
            }
            finally
            {
                applying = false;
            }
            cloud.Write(fresh);
        }

        /// <summary>
        /// D-025-AC-02/03: the cloud delivered a save written elsewhere (another device, or the first sync
        /// after a reinstall). When it wins, applyInPlay replaces the live wallet and progression and the
        /// local save takes it whole. Returns true when it was applied.
        /// </summary>
        public bool CloudChanged(Action<SaveSnapshot> applyInPlay)
        {
            var fromCloud = cloud.Read();
            var apply = SaveRules.Choose(local.Load(), fromCloud, local.Synced) == SaveSource.Cloud;
            if (fromCloud != null)
                local.MarkSynced();
            if (!apply)
                return false;
            applying = true;
            try
            {
                applyInPlay?.Invoke(fromCloud);
                local.Write(fromCloud);
            }
            finally
            {
                applying = false;
            }
            return true;
        }
    }
}
