using System;
using System.Collections.Generic;

namespace TreadShred.Analytics
{
    /// <summary>
    /// D-021-AC-04: events wait in a bounded queue until the analytics SDK is
    /// ready, and a failing SDK is logged, never thrown into gameplay.
    /// </summary>
    public sealed class AnalyticsDispatcher
    {
        public const int MaxQueued = 100;

        private readonly Queue<AnalyticsEvent> queued = new Queue<AnalyticsEvent>();
        private readonly Action<AnalyticsEvent> send;
        private readonly Action<string> warn;

        public AnalyticsDispatcher(Action<AnalyticsEvent> send, Action<string> warn)
        {
            this.send = send ?? throw new ArgumentNullException(nameof(send));
            this.warn = warn ?? throw new ArgumentNullException(nameof(warn));
        }

        public bool Ready { get; private set; }
        public int Queued => queued.Count;

        public void Log(AnalyticsEvent analyticsEvent)
        {
            if (!Ready)
            {
                if (queued.Count < MaxQueued)
                    queued.Enqueue(analyticsEvent);
                return;
            }
            Send(analyticsEvent);
        }

        /// <summary>The SDK is up: send everything that waited, oldest first.</summary>
        public void MarkReady()
        {
            Ready = true;
            while (queued.Count > 0)
                Send(queued.Dequeue());
        }

        private void Send(AnalyticsEvent analyticsEvent)
        {
            try
            {
                send(analyticsEvent);
            }
            catch (Exception error)
            {
                warn("[Analytics] " + analyticsEvent.Name + " failed: " + error.Message);
            }
        }
    }
}
