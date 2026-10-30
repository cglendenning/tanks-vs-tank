using System.Collections.Generic;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

namespace TreadShred.Save.Tests
{
    /// <summary>
    /// The iCloud key-value store as Apple documents NSUbiquitousKeyValueStore: one server
    /// value shared by the player's devices; each install keeps a local copy; writes made before
    /// an install's first sync are discarded; that first sync delivers the server value with a
    /// change notification; offline writes wait and upload when the device is back online; other
    /// devices' changes arrive as notifications. Values travel as JSON, as on the device.
    /// </summary>
    public sealed class FakeICloud
    {
        public string ServerJson;
        public readonly List<Device> Devices = new List<Device>();

        public SaveSnapshot Server => string.IsNullOrEmpty(ServerJson) ? null : JsonUtility.FromJson<SaveSnapshot>(ServerJson);

        public Device NewInstall(bool firstSyncDone, bool online = true)
        {
            var device = new Device(this) { FirstSyncDone = firstSyncDone, Online = online };
            if (firstSyncDone)
                device.LocalJson = ServerJson;
            Devices.Add(device);
            return device;
        }

        public sealed class Device : ICloudSave
        {
            private readonly FakeICloud cloud;
            private string pendingUpload;

            public Device(FakeICloud cloud)
            {
                this.cloud = cloud;
            }

            public string LocalJson;
            public bool FirstSyncDone;
            public bool Online;
            public int DiscardedWrites;
            public System.Action Changed;

            public SaveSnapshot Read() =>
                string.IsNullOrEmpty(LocalJson) ? null : JsonUtility.FromJson<SaveSnapshot>(LocalJson);

            public void Write(SaveSnapshot snapshot)
            {
                if (!FirstSyncDone)
                {
                    DiscardedWrites++;
                    return;
                }
                LocalJson = JsonUtility.ToJson(snapshot);
                if (Online)
                    Upload(LocalJson);
                else
                    pendingUpload = LocalJson;
            }

            /// <summary>The install's first sync: the server value arrives with a change notification.</summary>
            public void CompleteFirstSync()
            {
                FirstSyncDone = true;
                LocalJson = cloud.ServerJson;
                Changed?.Invoke();
            }

            public void GoOffline() => Online = false;

            public void GoOnline()
            {
                Online = true;
                if (pendingUpload == null)
                    return;
                Upload(pendingUpload);
                pendingUpload = null;
            }

            private void Upload(string json)
            {
                cloud.ServerJson = json;
                foreach (var other in cloud.Devices)
                {
                    if (other == this || !other.Online || !other.FirstSyncDone)
                        continue;
                    other.LocalJson = json;
                    other.Changed?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// One installation of the game wired the way the game wires it: the wallet and progression
    /// save to the local save and call CloudSync.LocalSaved, launch restores before the starter
    /// grant, and a cloud change replaces the live wallet and progression.
    /// </summary>
    public sealed class Install : ILocalSave
    {
        private readonly FakeICloud.Device device;
        private readonly CloudSync sync;
        private SaveSnapshot local = new SaveSnapshot();

        public Install(FakeICloud.Device device, System.Func<long> clock)
        {
            this.device = device;
            sync = new CloudSync(this, device, clock);
            device.Changed = () => sync.CloudChanged(snapshot =>
            {
                Wallet.Replace(snapshot.wallet);
                Progression.Replace(snapshot.progression);
            });
        }

        public GemWallet Wallet { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public bool Synced { get; private set; }

        public Install Launch()
        {
            sync.RestoreAtLaunch();
            Wallet = new GemWallet(new WalletStore(this));
            Wallet.GrantStarterIfNeeded();
            Progression = new PlayerProgression(new ProgressionStore(this), Wallet);
            return this;
        }

        public SaveSnapshot Load() => Copy(local);

        public void Write(SaveSnapshot snapshot) => local = Copy(snapshot);

        public void Stamp(long writtenAtTicks) => local.writtenAtTicks = writtenAtTicks;

        public void MarkSynced() => Synced = true;

        private static SaveSnapshot Copy(SaveSnapshot snapshot) =>
            JsonUtility.FromJson<SaveSnapshot>(JsonUtility.ToJson(snapshot));

        private sealed class WalletStore : IWalletStore
        {
            private readonly Install install;
            public WalletStore(Install install) => this.install = install;
            public WalletState Load() => install.Load().wallet;

            public void Save(WalletState state)
            {
                install.local.wallet = JsonUtility.FromJson<WalletState>(JsonUtility.ToJson(state));
                install.sync.LocalSaved();
            }
        }

        private sealed class ProgressionStore : IProgressionStore
        {
            private readonly Install install;
            public ProgressionStore(Install install) => this.install = install;
            public ProgressionState Load() => install.Load().progression;

            public void Save(ProgressionState state)
            {
                install.local.progression = JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(state));
                install.sync.LocalSaved();
            }
        }
    }
}
