using System;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-037-AC-04: the result screen's STARTER PACK callout while the pack is on sale, in the free War Chest's slot (the
/// War Chest is never on sale during the Starter Pack's window). It shows the live countdown and pulses, its fill
/// brightening and its size swelling, faster and further as the window runs down. Tapping it opens the Starter Pack
/// interstitial with the free test drive; it leaves at once when the pack is bought or the window ends (D-037-AC-07).
/// </summary>
public sealed class TreadShredStarterCallout : MonoBehaviour
{
    private const float Swell = 0.06f;

    private Button button;
    private Text label;
    private Outline outline;
    private float phase;

    public static void ResultScreen(RectTransform screen, Font font, bool won)
    {
        if (!TreadShredStore.Ensure().StarterPackOnSale)
            return;
        var button = TreadShredUi.CreateButton(string.Empty, screen, new Vector2(-530f, -92f), new Vector2(280f, 64f), 22, font,
            () => TreadShredStarterOffer.OpenFromCallout(font, won));
        TreadShredUi.StylePrimary(button);
        var callout = button.gameObject.AddComponent<TreadShredStarterCallout>();
        callout.button = button;
        callout.label = button.GetComponentInChildren<Text>();
        callout.outline = button.GetComponent<Outline>();
        callout.Pulse();
    }

    // Unscaled time: the result screen stops the game's clock.
    private void Update() => Pulse();

    private void Pulse()
    {
        var left = TreadShredStore.Ensure().StarterPackTimeLeft;
        if (left <= TimeSpan.Zero)
        {
            Destroy(gameObject);
            return;
        }
        label.text = StarterPackPitch.CalloutLabel(left);
        // The phase advances at the current rate, so a quickening pulse never jumps.
        phase += Time.unscaledDeltaTime * StarterPackPitch.PulseRate(left);
        var swing = StarterPackPitch.PulseStrength(left) * (0.5f + 0.5f * Mathf.Sin(phase * 2f * Mathf.PI));
        button.targetGraphic.color = Color.Lerp(TreadShredUi.PrimaryFill, Color.white, swing);
        outline.effectColor = Color.Lerp(TreadShredUi.TitleColor, Color.white, swing);
        transform.localScale = Vector3.one * (1f + Swell * swing);
    }
}
