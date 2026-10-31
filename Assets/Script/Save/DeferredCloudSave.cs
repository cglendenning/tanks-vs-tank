using System;

namespace TreadShred.Save
{
    /// <summary>
    /// D-025 on Android: Play Games saved games answer only after sign-in and a network round trip. Until the
    /// first load reports, the cloud reads as empty and writes are held, so a fresh install never overwrites a
    /// saved game it has not seen yet (D-025-AC-02); after it reports, writes go straight up (D-025-AC-04).
    /// </summary>
    public sealed class DeferredCloudSave : ICloudSave
    {
        private readonly Action<SaveSnapshot> send;
        private SaveSnapshot seen;
        private bool holding;

        public DeferredCloudSave(Action<SaveSnapshot> send)
        {
            this.send = send ?? throw new ArgumentNullException(nameof(send));
        }

        public bool Loaded { get; private set; }

        public SaveSnapshot Read() => seen;

        public void Write(SaveSnapshot snapshot)
        {
            if (!Loaded)
            {
                holding = true;
                return;
            }
            seen = snapshot;
            send(snapshot);
        }

        /// <summary>The saved game loaded: null when the player has none yet. Call CloudSync.CloudChanged next.</summary>
        public void Arrived(SaveSnapshot fromCloud)
        {
            Loaded = true;
            seen = fromCloud;
        }

        /// <summary>
        /// After CloudSync.CloudChanged: true when a local save was held and the cloud did not replace it, so it
        /// should go up now (call CloudSync.LocalSaved). Nothing stays held either way.
        /// </summary>
        public bool ReleaseHeld(bool cloudApplied)
        {
            var upload = holding && !cloudApplied;
            holding = false;
            return upload;
        }
    }
}
