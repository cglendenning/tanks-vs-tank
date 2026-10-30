using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;

/// <summary>Best mission scores per tier, as ManagerScore saves them (D-019, D-032).</summary>
public static class TreadShredScores
{
    public static int TotalScore()
    {
        return LeaderboardRules.TotalScore(BestMissionScores());
    }

    public static float Best(DifficultyTier tier, int mission)
    {
        return PlayerPrefs.GetFloat(LeaderboardRules.BestScoreKey(tier, mission));
    }

    public static void SaveIfBest(DifficultyTier tier, int mission, int score)
    {
        if (score <= Best(tier, mission))
            return;
        PlayerPrefs.SetFloat(LeaderboardRules.BestScoreKey(tier, mission), score);
        PlayerPrefs.Save();
    }

    private static IEnumerable<int> BestMissionScores()
    {
        for (var tier = 0; tier < LeaderboardRules.TierCount; tier++)
        for (var mission = 1; mission <= ProgressionRules.MissionCount; mission++)
            yield return (int)Best((DifficultyTier)tier, mission);
    }
}
