using System;
using System.Globalization;

namespace TreadShred.Progression
{
    /// <summary>R-REVIEW-PROMPT (D-033): when the native store review prompt may be requested.</summary>
    public static class ReviewRules
    {
        // Owner decision 2026-11-05: six wins alone; any day, any play time.
        public const int MinPlayDays = 1;
        public const int MinMissionsWon = 6;
        public const int MinActivePlayMinutes = 0;
        public const float ResultScreenDelaySeconds = 1.5f;
        public const int MaxRequestsPerAppVersion = 1;
        public const int MinDaysBetweenRequests = 120;
        public const string IosWriteReviewUrl = "https://apps.apple.com/app/id1403763343?action=write-review";
        public const string AndroidStoreUrl = "https://play.google.com/store/apps/details?id=com.tgts.tankkvstank";
        public const string RateButton = "RATE TREAD SHRED";
    }

    /// <summary>What the review prompt remembers; device only, never cloud saved.</summary>
    [Serializable]
    public sealed class ReviewPromptState
    {
        public string lastPlayDay = string.Empty;
        public int playDays;
        public int missionsWon;
        public double activeSeconds;
        public long lastRequestUtcTicks;
        public string lastRequestVersion = string.Empty;
        public int requestsThisVersion;
    }

    public interface IReviewPromptStore
    {
        ReviewPromptState Load();
        void Save(ReviewPromptState state);
    }

    /// <summary>
    /// D-033: counts real play (local calendar days with a mission, missions won, active mission time) and decides
    /// whether a victory screen may send the platform's review request. The stores never say whether their
    /// prompt appeared, so a request counts as made once it is sent.
    /// </summary>
    public sealed class ReviewPrompt
    {
        private readonly IReviewPromptStore store;
        private readonly ReviewPromptState state;

        public ReviewPrompt(IReviewPromptStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            state = store.Load() ?? new ReviewPromptState();
        }

        public int PlayDays => state.playDays;
        public int MissionsWon => state.missionsWon;
        public double ActiveMinutes => state.activeSeconds / 60.0;

        public bool PlayedEnough =>
            state.playDays >= ReviewRules.MinPlayDays &&
            state.missionsWon >= ReviewRules.MinMissionsWon &&
            state.activeSeconds >= ReviewRules.MinActivePlayMinutes * 60.0;

        /// <summary>A mission started on this local calendar day; each day counts once.</summary>
        public void MissionPlayed(DateTime localNow)
        {
            var day = localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (day == state.lastPlayDay)
                return;
            state.lastPlayDay = day;
            state.playDays++;
            store.Save(state);
        }

        /// <summary>Time in a mission, unpaused and in the foreground; saved when the mission ends.</summary>
        public void AddActiveTime(double seconds)
        {
            if (seconds > 0)
                state.activeSeconds += seconds;
        }

        public void MissionEnded(bool won)
        {
            if (won)
                state.missionsWon++;
            store.Save(state);
        }

        /// <summary>May a victory screen send the review request now (D-033-AC-02, AC-03, AC-04)?</summary>
        public bool MayRequest(DateTime utcNow, string appVersion, bool purchaseInProgress)
        {
            if (purchaseInProgress || !PlayedEnough)
                return false;
            if (state.lastRequestVersion == appVersion && state.requestsThisVersion >= ReviewRules.MaxRequestsPerAppVersion)
                return false;
            return state.lastRequestUtcTicks == 0 ||
                   (utcNow - new DateTime(state.lastRequestUtcTicks, DateTimeKind.Utc)).TotalDays >= ReviewRules.MinDaysBetweenRequests;
        }

        public void RequestSent(DateTime utcNow, string appVersion)
        {
            state.requestsThisVersion = state.lastRequestVersion == appVersion ? state.requestsThisVersion + 1 : 1;
            state.lastRequestVersion = appVersion;
            state.lastRequestUtcTicks = utcNow.Ticks;
            store.Save(state);
        }
    }
}
