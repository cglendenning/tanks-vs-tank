using System;
using System.Collections.Generic;
using System.Linq;

namespace TreadShred.Progression.Tests
{
    /// <summary>Simulated time: callbacks run in order when the test advances the clock, never inline.</summary>
    public sealed class FakeClock
    {
        private readonly List<(float at, int order, Action run)> due = new List<(float, int, Action)>();
        private int order;

        public float Now { get; private set; }

        public void After(float seconds, Action run) => due.Add((Now + seconds, order++, run));

        public void Advance(float seconds)
        {
            var until = Now + seconds;
            while (true)
            {
                var next = due.Where(d => d.at <= until).OrderBy(d => d.at).ThenBy(d => d.order).ToList();
                if (next.Count == 0)
                    break;
                var item = next[0];
                due.Remove(item);
                Now = Math.Max(Now, item.at);
                item.run();
            }
            Now = until;
        }

        public bool Idle => due.Count == 0;
    }

    /// <summary>
    /// Game Center as GameKit behaves: every call answers later (a submission more slowly than a
    /// read, the order that exposes reads racing a submission); an accepted score is queued and
    /// only shows on the board a few seconds afterwards; the board keeps a player's best score,
    /// never a lower one; offline submissions fail; loads can fail.
    /// </summary>
    public sealed class FakeGameCenter : IGameCenter
    {
        private readonly FakeClock clock;

        public FakeGameCenter(FakeClock clock)
        {
            this.clock = clock;
        }

        public const float AnswerSeconds = 0.3f;
        public const float SubmitSeconds = 1f;
        public const float UploadSeconds = 3f;

        /// <summary>Long enough for any submission, its upload and the catch-up reads to finish.</summary>
        public const float SettleSeconds = 12f;

        public bool SignedIn { get; set; } = true;
        public bool CanOfferSignIn = true;
        public bool Online = true;
        public bool LoadsFail;
        public float Upload = UploadSeconds;

        /// <summary>The player's score as the board shows it.</summary>
        public long OnBoard;
        public readonly List<(string name, long score)> Others = new List<(string, long)>();
        public readonly List<long> Submissions = new List<long>();
        public int Loads;
        public int SignInOffers;

        public bool OfferSignIn()
        {
            SignInOffers++;
            return CanOfferSignIn;
        }

        public void Submit(long score, Action<bool> accepted)
        {
            Submissions.Add(score);
            clock.After(SubmitSeconds, () =>
            {
                if (!Online)
                {
                    accepted(false);
                    return;
                }
                accepted(true);
                clock.After(Upload, () => OnBoard = Math.Max(OnBoard, score));
            });
        }

        public void LoadPlayerEntry(Action<int, long> entry)
        {
            Loads++;
            clock.After(AnswerSeconds, () =>
            {
                if (LoadsFail)
                    entry(0, 0);
                else
                    entry(OnBoard > 0 ? RankOf(OnBoard) : 0, OnBoard);
            });
        }

        public void LoadTop(int count, Action<LeaderboardPage> page)
        {
            Loads++;
            clock.After(AnswerSeconds, () =>
            {
                if (LoadsFail)
                {
                    page(null);
                    return;
                }
                var rows = Others.Select(o => (o.name, o.score, isPlayer: false)).ToList();
                if (OnBoard > 0)
                    rows.Add(("PLAYER", OnBoard, true));
                var ranked = rows.OrderByDescending(r => r.score)
                    .Select((r, i) => new LeaderboardEntry(i + 1, r.Item1, r.Item2, r.isPlayer)).ToList();
                LeaderboardEntry? player = ranked.Where(r => r.IsPlayer).Cast<LeaderboardEntry?>().FirstOrDefault();
                page(new LeaderboardPage(ranked.Take(count).ToList(), player));
            });
        }

        public int RankOf(long score) => Others.Count(o => o.score > score) + 1;
    }

    /// <summary>The game's side, wired as ManagerScore and the leaderboard screen use it.</summary>
    public sealed class LeaderboardGame
    {
        public readonly FakeClock Clock = new FakeClock();
        public readonly FakeGameCenter GameCenter;
        public readonly LeaderboardSync Sync;
        public readonly ScoreRun Run = new ScoreRun();
        public int HighScore;

        public LeaderboardGame(bool signedIn = true, long onBoard = 0)
        {
            GameCenter = new FakeGameCenter(Clock) { SignedIn = signedIn, OnBoard = onBoard };
            GameCenter.Others.AddRange(new[] { ("ACE", 9000L), ("BRAVO", 4000L), ("COBRA", 1500L), ("DELTA", 600L) });
            Sync = new LeaderboardSync(GameCenter, () => HighScore, Clock.After);
        }

        /// <summary>Game Center reports sign-in (the plugin's OnSignIn "1").</summary>
        public LeaderboardGame SignIn()
        {
            GameCenter.SignedIn = true;
            Sync.SignedIn();
            return this;
        }

        /// <summary>A mission ends: SCORE is banked into HIGH SCORE and submitted, as ManagerScore does.</summary>
        public MissionScore EndMission(bool won, int kills, DifficultyTier tier = DifficultyTier.Recruit)
        {
            var mission = Run.Complete(won, kills, tier);
            HighScore = Math.Max(HighScore, mission.RunScore);
            Sync.SubmitIfRaised();
            return mission;
        }

        /// <summary>Asks for the leaderboard screen's page and runs the clock until it answers or 30 seconds pass.</summary>
        public (bool answered, LeaderboardPage page) OpenScreen()
        {
            var answered = false;
            LeaderboardPage shown = null;
            Sync.RequestTop(page =>
            {
                answered = true;
                shown = page;
            });
            for (var i = 0; i < 300 && !answered; i++)
                Clock.Advance(0.1f);
            return (answered, shown);
        }

        /// <summary>The result screen's rank line: the rank it shows, or 0 if it never filled in.</summary>
        public int ResultRank()
        {
            var shown = 0;
            Sync.RequestRank(rank => shown = rank);
            for (var i = 0; i < 300 && shown == 0 && !Clock.Idle; i++)
                Clock.Advance(0.1f);
            return shown;
        }
    }
}
