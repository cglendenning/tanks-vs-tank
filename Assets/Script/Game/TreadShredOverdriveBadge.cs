using TreadShred.Economy;
using UnityEngine;

/// <summary>
/// D-009-AC-03: while Overdrive is active, the mission HUD says so. Hidden while paused and once the mission
/// ends, where the result screen takes over.
/// </summary>
public static class TreadShredOverdriveBadge
{
    private static GameObject badge;

    public static void Show(Font font)
    {
        Hide();
        var root = TreadShredUi.CreateOverlay("TreadShredOverdriveBadge", 5, receivesTaps: false);
        TreadShredUi.CreateText(OfferLabels.OverdriveActive, root, new Vector2(0f, 490f), new Vector2(520f, 56f), 32, font)
            .color = TreadShredUi.AccentColor;
        badge = root.gameObject;
    }

    public static void SetVisible(bool visible)
    {
        if (badge != null)
            badge.SetActive(visible);
    }

    public static void Hide()
    {
        if (badge != null)
            Object.Destroy(badge);
        badge = null;
    }
}
