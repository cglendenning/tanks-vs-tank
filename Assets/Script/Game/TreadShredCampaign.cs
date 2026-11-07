using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// D-019: plays campaign missions. A mission number sets its tier and loads its map; the first mission of a tier the
/// player has just been promoted into plays the promotion first (D-019-AC-05). After Mission 120 the mission list says
/// the campaign is cleared (D-019-AC-04).
/// </summary>
public static class TreadShredCampaign
{
    internal const string ClearedKey = "TreadShredCampaignClearedPending";

    public static void Launch(int number)
    {
        var progress = TreadShredProgress.Ensure();
        var tier = Campaign.TierOf(number);
        progress.SelectTier(tier);
        if (progress.PendingPromotion == tier && Campaign.MapOf(number) == 1)
        {
            progress.PromotionPlayed();
            TreadShredPromotion.Show(tier, TreadShredUi.FindFont(GameObject.Find("Canvas")), () => Load(number));
            return;
        }
        Load(number);
    }

    /// <summary>NEXT MISSION: the following mission number, or the mission list after the last one.</summary>
    public static void Next()
    {
        var number = TreadShredProgress.Ensure().CurrentNumber();
        var next = number == 0 ? null : Campaign.Next(number);
        if (next is int following)
        {
            Launch(following);
            return;
        }
        PlayerPrefs.SetInt(ClearedKey, 1);
        PlayerPrefs.Save();
        ManagerScore.EndRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Start");
    }

    public static void ShowClearedIfDue(Font font)
    {
        if (PlayerPrefs.GetInt(ClearedKey, 0) == 0)
            return;
        PlayerPrefs.DeleteKey(ClearedKey);
        PlayerPrefs.Save();
        TreadShredNotePanel.Show(Campaign.Cleared, new List<string> { "REPLAY ANY MISSION TO RAISE YOUR HIGH SCORE" }, font);
    }

    private static void Load(int number)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lv" + Campaign.MapOf(number));
    }
}
