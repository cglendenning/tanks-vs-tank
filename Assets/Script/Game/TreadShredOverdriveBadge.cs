using System.Collections.Generic;
using TreadShred.Economy;
using UnityEngine;

/// <summary>
/// D-009-AC-03: while Overdrive is active, the mission HUD says so. D-017-AC-10: in the top right corner, under the HUD
/// bar and the shields, a short stack names each upgraded track and its level, so a player sees what they bought at
/// work. Hidden while paused and once the mission ends, where the result screen takes over.
/// </summary>
public static class TreadShredOverdriveBadge
{
    private const float TopY = 490f;
    // The corner stack starts below the HUD bar on every screen shape (this overlay scales to the screen's height).
    private const float CornerInset = 30f;
    private const float StackTop = 130f;
    private const float StackLine = 36f;

    private static GameObject badge;
    private static GameObject upgrades;

    public static void Show(Font font)
    {
        Clear(ref badge);
        var root = TreadShredUi.CreateOverlay("TreadShredOverdriveBadge", 5, receivesTaps: false);
        TreadShredUi.CreateText(OfferLabels.OverdriveActive, root, new Vector2(0f, TopY), new Vector2(520f, 56f), 32, font)
            .color = TreadShredUi.AccentColor;
        badge = root.gameObject;
    }

    /// <summary>D-017-AC-10: the upgraded tracks, one line each in the top right corner. Nothing with no upgrades.</summary>
    public static void ShowUpgrades(Font font, IReadOnlyList<string> lines)
    {
        Clear(ref upgrades);
        if (lines.Count == 0)
            return;
        var root = TreadShredUi.CreateOverlay("TreadShredUpgradeBadge", 5, receivesTaps: false);
        for (var i = 0; i < lines.Count; i++)
        {
            var text = TreadShredUi.CreateText(lines[i], root, Vector2.zero, new Vector2(420f, 34f), 24, font);
            text.alignment = TextAnchor.MiddleRight;
            text.color = Color.white;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-CornerInset, -StackTop - i * StackLine);
        }
        upgrades = root.gameObject;
    }

    public static void SetVisible(bool visible)
    {
        if (badge != null)
            badge.SetActive(visible);
        if (upgrades != null)
            upgrades.SetActive(visible);
    }

    public static void Hide()
    {
        Clear(ref badge);
        Clear(ref upgrades);
    }

    private static void Clear(ref GameObject line)
    {
        if (line != null)
            Object.Destroy(line);
        line = null;
    }
}
