using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-035-AC-03: a first play's opening cues. GET READY in block letters on a dimmed card just behind the mission's own
/// 3-2-1 countdown, lifted when the countdown ends, and MOVE WITH YOUR THUMB above the joystick until the player moves.
/// </summary>
public sealed class TreadShredFirstMinuteCues : MonoBehaviour
{
    private GameObject card;
    private Text moveHint;
    private RectTransform root;
    private Joystick joystick;

    public static void Show(Font font, Joystick joystick)
    {
        var hud = GameObject.Find("Canvas");
        var hudOrder = hud != null && hud.GetComponent<Canvas>() != null ? hud.GetComponent<Canvas>().sortingOrder : 0;
        // Just behind the mission HUD, so the countdown numerals draw over the card. Taps pass through both overlays.
        var cardRoot = TreadShredUi.CreateOverlay("TreadShredGetReady", hudOrder - 1, receivesTaps: false);
        TreadShredUi.CreateBackdrop(cardRoot, 0.72f);
        TreadShredUi.CreateText(FirstMinute.GetReady, cardRoot, new Vector2(0f, -250f), new Vector2(1500f, 170f), 140, font)
            .color = TreadShredUi.TitleColor;

        var root = TreadShredUi.CreateOverlay("TreadShredFirstMinuteCues", hudOrder + 1, receivesTaps: false);
        var cues = root.gameObject.AddComponent<TreadShredFirstMinuteCues>();
        cues.root = root;
        cues.card = cardRoot.gameObject;
        cues.joystick = joystick;
        cues.moveHint = TreadShredUi.CreateText(FirstMinute.MoveHint, root, Vector2.zero, new Vector2(480f, 50f), 30, font);
        cues.moveHint.color = TreadShredUi.TitleColor;
        if (joystick == null)
            cues.moveHint.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // The countdown freezes game time; ENGAGE restarts it, and the card lifts.
        if (card != null && Time.timeScale > 0f)
        {
            Destroy(card);
            card = null;
        }
        if (moveHint != null)
        {
            if (joystick == null || Mathf.Abs(joystick.Horizontal()) + Mathf.Abs(joystick.Vertical()) > 0.1f)
            {
                Destroy(moveHint.gameObject);
                moveHint = null;
            }
            else
            {
                PlaceMoveHint();
            }
        }
        if (card == null && moveHint == null)
            Destroy(gameObject);
    }

    // Centred above the joystick's top edge, kept clear of the screen's left edge.
    private void PlaceMoveHint()
    {
        var pad = (RectTransform)joystick.transform;
        var canvas = pad.GetComponentInParent<Canvas>();
        var view = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var corners = new Vector3[4];
        pad.GetWorldCorners(corners);
        var top = RectTransformUtility.WorldToScreenPoint(view, (corners[1] + corners[2]) / 2f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, top, null, out var local);
        var half = moveHint.rectTransform.sizeDelta.x / 2f;
        moveHint.rectTransform.anchoredPosition = new Vector2(Mathf.Max(local.x, -root.rect.width / 2f + half + 16f), local.y + 36f);
    }
}
