using TreadShred.Progression;
using UnityEngine;

/// <summary>D-032-AC-01: SCORE (the current run) and HIGH SCORE (the best SCORE ever).</summary>
public static class TreadShredScores
{
    private const string HighScoreKey = "TreadShredHighScore";

    public static ScoreRun Run { get; } = new ScoreRun();

    public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

    /// <summary>Saves a run score as HIGH SCORE when it beats it; true when it did.</summary>
    public static bool RecordScore(int score)
    {
        if (score <= HighScore)
            return false;
        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save();
        return true;
    }
}
