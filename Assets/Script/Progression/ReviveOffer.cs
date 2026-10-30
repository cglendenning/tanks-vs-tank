using System;

namespace TreadShred.Progression
{
    public enum ReviveOutcome
    {
        Pending,
        Revived,
        Declined
    }

    /// <summary>
    /// D-020: one revive offer after the tank is destroyed. It stays up for
    /// R-REVIVE offer_seconds, the clock waits while the store's purchase sheet
    /// is open, and running out counts as declining.
    /// </summary>
    public sealed class ReviveOffer
    {
        public ReviveOffer(int revivesUsed)
        {
            if (!ReviveRules.CanOffer(revivesUsed))
                throw new InvalidOperationException("No revive left in this mission.");
            Cost = ReviveRules.CostGems(revivesUsed);
            Remaining = ProgressionRules.ReviveOfferSeconds;
        }

        public int Cost { get; }
        public float Remaining { get; private set; }
        public ReviveOutcome Outcome { get; private set; } = ReviveOutcome.Pending;

        public int SecondsShown => (int)Math.Ceiling(Math.Max(0f, Remaining));

        /// <summary>Advances the clock; returns true on the tick the offer runs out.</summary>
        public bool Tick(float seconds, bool purchaseOpen)
        {
            if (Outcome != ReviveOutcome.Pending)
                return false;
            if (!purchaseOpen)
                Remaining -= seconds;
            if (Remaining > 0f)
                return false;
            Outcome = ReviveOutcome.Declined;
            return true;
        }

        /// <summary>The revive was paid for; returns false if the offer had already closed.</summary>
        public bool Accept() => Close(ReviveOutcome.Revived);

        public bool Decline() => Close(ReviveOutcome.Declined);

        private bool Close(ReviveOutcome outcome)
        {
            if (Outcome != ReviveOutcome.Pending)
                return false;
            Outcome = outcome;
            return true;
        }
    }
}
