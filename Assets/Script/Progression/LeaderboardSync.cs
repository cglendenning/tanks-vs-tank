using System;
using System.Collections.Generic;

namespace TreadShred.Progression
{
    public enum LeaderboardOpenResult
    {
        // Signed in: show the in-game leaderboard screen.
        Ready,
        SignInShown,
        SignInFromSettings
    }

    /// <summary>The platform leaderboard service as the game sees it (Game Center on iOS). Loads answer later.</summary>
    public interface IGameCenter
    {
        bool SignedIn { get; }

        /// <summary>Shows the platform's own sign-in if it offered one; false means the player must sign in from Settings.</summary>
        bool OfferSignIn();

        void Submit(long score, Action<bool> accepted);

        /// <summary>The player's own entry as (rank, score); (0, 0) when the service holds none or cannot load the board.</summary>
        void LoadPlayerEntry(Action<int, long> entry);

        /// <summary>The top entries and the player's own; null when the board cannot be loaded.</summary>
        void LoadTop(int count, Action<LeaderboardPage> page);
    }

    /// <summary>
    /// D-032: when HIGH SCORE is submitted and how the board is read back. Submissions are judged
    /// against the service's own record of the player (never a local marker), reads made during a
    /// submission wait for it, and reads re-try until the board shows a score accepted this session.
    /// </summary>
    public sealed class LeaderboardSync
    {
        private readonly IGameCenter gameCenter;
        private readonly Func<int> highScore;
        private readonly Action<float, Action> after;
        private readonly Action<int> restoreHighScore;
        private readonly List<Action> afterSubmission = new List<Action>();
        private long? onGameCenter;
        private int acceptedThisSession;
        private bool submitting;

        /// <param name="after">Runs an action after a delay in seconds (a coroutine in the game).</param>
        /// <param name="restoreHighScore">Raises HIGH SCORE to the service's record of the player when that is higher.</param>
        public LeaderboardSync(IGameCenter gameCenter, Func<int> highScore, Action<float, Action> after,
            Action<int> restoreHighScore = null)
        {
            this.gameCenter = gameCenter ?? throw new ArgumentNullException(nameof(gameCenter));
            this.highScore = highScore ?? throw new ArgumentNullException(nameof(highScore));
            this.after = after ?? throw new ArgumentNullException(nameof(after));
            this.restoreHighScore = restoreHighScore;
        }

        /// <summary>After sign-in: learn what the service holds for the player, then submit if HIGH SCORE beats it.</summary>
        public void SignedIn()
        {
            gameCenter.LoadPlayerEntry((rank, score) =>
            {
                onGameCenter = score;
                // HIGH SCORE lives on the device, so a reinstall or a new phone starts it at zero while the service
                // still holds the player's best; without this the next run would read as a NEW HIGH SCORE below it.
                if (score > highScore() && score <= int.MaxValue)
                    restoreHighScore?.Invoke((int)score);
                SubmitIfRaised();
            });
        }

        /// <summary>D-032-AC-01: submits HIGH SCORE when it beats what the service holds for the player.</summary>
        public void SubmitIfRaised()
        {
            if (!gameCenter.SignedIn || submitting)
                return;
            var baseline = LeaderboardRules.Baseline(onGameCenter, acceptedThisSession);
            var best = highScore();
            if (!baseline.HasValue || !LeaderboardRules.ShouldSubmit(best, baseline.Value))
                return;
            submitting = true;
            gameCenter.Submit(best, accepted =>
            {
                submitting = false;
                if (accepted)
                {
                    acceptedThisSession = Math.Max(acceptedThisSession, best);
                    // HIGH SCORE may have risen while this one was in flight; reads wait for that one too.
                    SubmitIfRaised();
                    if (submitting)
                        return;
                }
                // A rejected score is retried at the next mission end or launch, never in a loop.
                var waiting = afterSubmission.ToArray();
                afterSubmission.Clear();
                foreach (var read in waiting)
                    read();
            });
        }

        /// <summary>D-032-AC-02: the player's rank, once the board shows their latest accepted score.</summary>
        public void RequestRank(Action<int> onRank)
        {
            if (!gameCenter.SignedIn)
                return;
            WhenSubmitted(() => CatchUpRank(onRank, 0));
        }

        /// <summary>D-032-AC-02: the top of the board and the player's entry; null when the board cannot be loaded.</summary>
        public void RequestTop(Action<LeaderboardPage> onPage)
        {
            if (!gameCenter.SignedIn)
            {
                onPage(null);
                return;
            }
            WhenSubmitted(() => CatchUpTop(onPage, 0));
        }

        /// <summary>D-032-AC-03: ready when signed in; otherwise offers the platform's sign-in, never forcing it.</summary>
        public LeaderboardOpenResult Open()
        {
            if (gameCenter.SignedIn)
                return LeaderboardOpenResult.Ready;
            return gameCenter.OfferSignIn() ? LeaderboardOpenResult.SignInShown : LeaderboardOpenResult.SignInFromSettings;
        }

        private void WhenSubmitted(Action read)
        {
            if (submitting)
                afterSubmission.Add(read);
            else
                read();
        }

        private void CatchUpTop(Action<LeaderboardPage> onPage, int reads)
        {
            gameCenter.LoadTop(LeaderboardPage.TopCount, page =>
            {
                var onBoard = page != null && page.Player.HasValue ? page.Player.Value.Score : 0;
                if (page != null && LeaderboardRules.BoardBehind(onBoard, acceptedThisSession, reads + 1))
                    after(LeaderboardRules.CatchUpSeconds, () => CatchUpTop(onPage, reads + 1));
                else
                    onPage(page);
            });
        }

        private void CatchUpRank(Action<int> onRank, int reads)
        {
            gameCenter.LoadPlayerEntry((rank, score) =>
            {
                onGameCenter = Math.Max(onGameCenter ?? 0, score);
                if (LeaderboardRules.BoardBehind(score, acceptedThisSession, reads + 1))
                    after(LeaderboardRules.CatchUpSeconds, () => CatchUpRank(onRank, reads + 1));
                else if (rank > 0)
                    onRank(rank);
            });
        }
    }
}
