using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-019-AC-03: the mission number and its tier ("MISSION 41 · VETERAN") in the centre of the HUD bar for the whole
/// mission; hidden while paused and once the mission ends.
/// </summary>
public static class TreadShredMissionLabel
{
    private const float TopY = 500f;

    private static GameObject label;

    public static void Show(Font font)
    {
        Hide();
        var number = TreadShredProgress.Ensure().CurrentNumber();
        if (number == 0)
            return;
        var root = TreadShredUi.CreateOverlay("TreadShredMissionLabel", 5, receivesTaps: false);
        TreadShredUi.CreateText(Campaign.HudLabel(number), root, new Vector2(0f, TopY), new Vector2(560f, 46f), 30, font)
            .color = TreadShredUi.TitleColor;
        label = root.gameObject;
    }

    public static void SetVisible(bool visible)
    {
        if (label != null)
            label.SetActive(visible);
    }

    public static void Hide()
    {
        if (label != null)
            Object.Destroy(label);
        label = null;
    }
}
