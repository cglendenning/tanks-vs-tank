using System.Collections.Generic;
using System.Globalization;

namespace TreadShred.Progression
{
    public readonly struct LeaderboardEntry
    {
        public LeaderboardEntry(int rank, string name, long score, bool isPlayer)
        {
            Rank = rank;
            Name = name;
            Score = score;
            IsPlayer = isPlayer;
        }

        public int Rank { get; }
        public string Name { get; }
        public long Score { get; }
        public bool IsPlayer { get; }
    }

    /// <summary>D-032-AC-02: the top of the platform leaderboard and the player's own entry.</summary>
    public sealed class LeaderboardPage
    {
        public const int TopCount = 25;
        public const int MaxNameLength = 18;

        public LeaderboardPage(IReadOnlyList<LeaderboardEntry> top, LeaderboardEntry? player)
        {
            Top = top ?? new LeaderboardEntry[0];
            Player = player;
        }

        public IReadOnlyList<LeaderboardEntry> Top { get; }
        public LeaderboardEntry? Player { get; }

        /// <summary>The player has a rank but is not among the rows shown, so their row goes underneath.</summary>
        public bool PlayerBelowTop
        {
            get
            {
                if (!Player.HasValue || Player.Value.Rank <= 0)
                    return false;
                foreach (var entry in Top)
                {
                    if (entry.IsPlayer)
                        return false;
                }
                return true;
            }
        }

        public static string RankText(LeaderboardEntry entry)
        {
            return "#" + entry.Rank.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string ScoreText(LeaderboardEntry entry)
        {
            return entry.Score.ToString("N0", CultureInfo.InvariantCulture);
        }

        // Platform nicknames in the game's all-caps style, cut to fit a row.
        public static string NameText(LeaderboardEntry entry)
        {
            var name = (entry.Name ?? string.Empty).Trim().ToUpperInvariant();
            if (name.Length == 0)
                return "UNKNOWN";
            return name.Length <= MaxNameLength ? name : name.Substring(0, MaxNameLength);
        }
    }
}
