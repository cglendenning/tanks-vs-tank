using TreadShred.Economy;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-020-AC-06: after a paid revive the game waits on READY?. GO plays the mission's 3-2-1 and play resumes where it
/// stopped; NOT YET (and the back gesture) opens the pause menu, whose resume plays the 3-2-1 too.
/// </summary>
public sealed class TreadShredReadyPanel : MonoBehaviour
{
    public static TreadShredReadyPanel Show(Font font)
    {
        Time.timeScale = 0f;
        var root = TreadShredUi.CreateOverlay("TreadShredReady", 120);
        var panel = root.gameObject.AddComponent<TreadShredReadyPanel>();
        panel.Build(root, font);
        return panel;
    }

    private void Build(RectTransform root, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.55f);
        TreadShredUi.CreateText(OfferLabels.Ready, backdrop, new Vector2(0f, 110f), new Vector2(900f, 130f), 110, font)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateButton(OfferLabels.NotYet, backdrop, new Vector2(-250f, -90f), new Vector2(420f, 120f), 44, font, NotYet);
        var go = TreadShredUi.CreateButton(OfferLabels.Go, backdrop, new Vector2(250f, -90f), new Vector2(420f, 120f), 56, font, Go);
        TreadShredUi.StylePrimary(go);
    }

    private void Go()
    {
        Destroy(gameObject);
        if (Count.Replay())
            return;
        Debug.LogWarning("[Ready] no countdown in this scene; play resumes at once");
        Time.timeScale = 1f;
    }

    private void NotYet()
    {
        Destroy(gameObject);
        var buttons = FindFirstObjectByType<ManagerButton>();
        if (buttons != null)
        {
            buttons.PauseGame();
            return;
        }
        Debug.LogWarning("[Ready] no pause menu in this scene; starting the countdown instead");
        Go();
    }

    private void Start() => TreadShredBack.Opened(this, NotYet);

    private void OnDestroy() => TreadShredBack.Closed(this);
}
