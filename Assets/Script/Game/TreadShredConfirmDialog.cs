using System;
using TreadShred.Economy;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-009: asks before gems already held are spent on a boost. Nothing
/// behind the dialog takes taps while it is open.
/// </summary>
public sealed class TreadShredConfirmDialog : MonoBehaviour
{
    private Action onConfirm;

    public static TreadShredConfirmDialog Show(GemConfirmation confirmation, Font font, Action onConfirm)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredConfirm", 130);
        var dialog = root.gameObject.AddComponent<TreadShredConfirmDialog>();
        dialog.onConfirm = onConfirm;
        dialog.Build(root, confirmation, font);
        return dialog;
    }

    private void Build(RectTransform root, GemConfirmation confirmation, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);
        var panel = TreadShredUi.CreateRect("ConfirmPanel", backdrop, Vector2.zero, new Vector2(900f, 400f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);

        TreadShredUi.CreateText(confirmation.Title, panel, new Vector2(0f, 120f), new Vector2(860f, 70f), 52, font)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText(confirmation.Detail, panel, new Vector2(0f, 45f), new Vector2(860f, 44f), 30, font)
            .color = Color.white;
        TreadShredUi.CreateButton(confirmation.ConfirmButton, panel, new Vector2(-200f, -95f), new Vector2(360f, 100f),
            32, font, Confirm);
        TreadShredUi.CreateButton(confirmation.CancelButton, panel, new Vector2(200f, -95f), new Vector2(360f, 100f),
            32, font, Cancel);
    }

    public void Confirm()
    {
        Destroy(gameObject);
        onConfirm();
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }
}
