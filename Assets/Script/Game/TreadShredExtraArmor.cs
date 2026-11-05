using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-017-AC-10: the mission HUD draws three shields, so armor beyond them (armor upgrades, Armored Redeploy) shows as +N
/// just left of the shields, counting down as hits use it.
/// </summary>
public static class TreadShredExtraArmor
{
    private const float Gap = 12f;
    private const float Width = 120f;

    public static Text Attach(IList<GameObject> shields, Font font)
    {
        RectTransform leftmost = null;
        foreach (var shield in shields)
        {
            if (shield == null)
                continue;
            var rect = (RectTransform)shield.transform;
            if (leftmost == null || rect.position.x < leftmost.position.x)
                leftmost = rect;
        }
        if (leftmost == null)
        {
            Debug.LogError("[HUD] No shields to place the extra armor beside.");
            return null;
        }

        var scale = leftmost.localScale;
        var height = leftmost.rect.height * scale.y;
        var label = TreadShredUi.CreateText(string.Empty, (RectTransform)leftmost.parent, Vector2.zero, new Vector2(Width, height),
            Mathf.RoundToInt(height * 0.6f), font);
        label.name = "ExtraArmor";
        label.alignment = TextAnchor.MiddleRight;
        label.color = TreadShredUi.TitleColor;
        var rectLabel = label.rectTransform;
        rectLabel.anchorMin = leftmost.anchorMin;
        rectLabel.anchorMax = leftmost.anchorMax;
        rectLabel.pivot = new Vector2(1f, 0.5f);
        rectLabel.anchoredPosition = leftmost.anchoredPosition + new Vector2(-leftmost.rect.width * scale.x * leftmost.pivot.x - Gap,
            (0.5f - leftmost.pivot.y) * height);
        TreadShredUi.KeepUpright(label.transform);
        return label;
    }
}
