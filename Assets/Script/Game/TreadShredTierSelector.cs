using System.Collections.Generic;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-019: difficulty tier buttons on mission select. Locked tiers show what unlocks them.
/// </summary>
public sealed class TreadShredTierSelector : MonoBehaviour
{
    private static readonly DifficultyTier[] Tiers = { DifficultyTier.Recruit, DifficultyTier.Veteran, DifficultyTier.Elite };

    private readonly Dictionary<DifficultyTier, Button> buttons = new Dictionary<DifficultyTier, Button>();

    public static void Attach(GameObject missionSelect, Font font)
    {
        if (missionSelect == null || missionSelect.GetComponentInChildren<TreadShredTierSelector>(true) != null)
            return;

        var parent = (RectTransform)missionSelect.transform;
        // Bottom-right corner, clear of the mission list and the play button.
        var strip = TreadShredUi.CreateRect("TreadShredTiers", parent, Vector2.zero, new Vector2(840f, 84f),
            typeof(RectTransform));
        strip.anchorMin = strip.anchorMax = new Vector2(1f, 0f);
        strip.pivot = new Vector2(1f, 0f);
        strip.anchoredPosition = new Vector2(-30f, 24f);

        var selector = strip.gameObject.AddComponent<TreadShredTierSelector>();
        for (var i = 0; i < Tiers.Length; i++)
        {
            var tier = Tiers[i];
            selector.buttons[tier] = TreadShredUi.CreateButton(ProgressionLabels.Tier(tier), strip,
                new Vector2((i - 1) * 280f, 0f), new Vector2(260f, 76f), 26, font, () => selector.Select(tier));
            // Opaque grey when locked, so the orange outline cannot show through.
            var colors = selector.buttons[tier].colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            selector.buttons[tier].colors = colors;
        }
        selector.Refresh();
    }

    private void OnEnable()
    {
        if (buttons.Count > 0)
            Refresh();
    }

    private void Select(DifficultyTier tier)
    {
        TreadShredProgress.Ensure().SelectTier(tier);
        Refresh();
    }

    private void Refresh()
    {
        var progress = TreadShredProgress.Ensure();
        foreach (var entry in buttons)
        {
            var unlocked = progress.Progression.IsUnlocked(entry.Key);
            var selected = progress.SelectedTier == entry.Key;
            TreadShredUi.SetLabel(entry.Value, unlocked
                ? (selected ? "> " : string.Empty) + ProgressionLabels.Tier(entry.Key)
                : ProgressionLabels.Tier(entry.Key) + " LOCKED");
            entry.Value.interactable = unlocked;
            entry.Value.GetComponentInChildren<Text>().color = selected ? TreadShredUi.TitleColor : Color.white;
        }
    }
}
