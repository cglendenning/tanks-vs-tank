using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builders for the panels Tread Shred creates at runtime (result-screen
/// offers, settings), so they share one look.
/// </summary>
public static class TreadShredUi
{
    public static readonly Color PanelColor = new Color(0.015f, 0.035f, 0.045f, 0.94f);
    public static readonly Color AccentColor = new Color(1f, 0.36f, 0.08f, 1f);
    public static readonly Color TitleColor = new Color(1f, 0.72f, 0.28f, 1f);
    public static readonly Color DetailColor = new Color(0.78f, 0.86f, 0.88f, 1f);
    private static readonly Color ButtonColor = new Color(0.08f, 0.14f, 0.16f, 0.98f);

    // The first font used by existing text under root, so new labels match.
    // A full-screen landscape canvas above the scene, scaled by height so
    // layouts fit phones and iPads. Without taps it never blocks input.
    public static RectTransform CreateOverlay(string name, int sortingOrder, bool receivesTaps = true,
        Transform parent = null)
    {
        var root = receivesTaps
            ? new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            : new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        if (parent != null)
            root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        return (RectTransform)root.transform;
    }

    // Dims everything behind a dialog and swallows taps meant for it.
    public static RectTransform CreateBackdrop(RectTransform root, float opacity)
    {
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        backdrop.SetParent(root, false);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, opacity);
        return backdrop;
    }

    public static Font FindFont(GameObject root)
    {
        if (root == null)
            return null;

        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.font != null)
                return text.font;
        }
        return null;
    }

    public static RectTransform CreateRect(string name, RectTransform parent, Vector2 position, Vector2 size,
        params System.Type[] components)
    {
        var gameObject = new GameObject(name, components);
        var rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Text CreateText(string value, RectTransform parent, Vector2 position, Vector2 size, int fontSize, Font font)
    {
        var text = CreateRect("Label", parent, position, size, typeof(RectTransform)).gameObject.AddComponent<Text>();
        text.text = value;
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static void StylePanel(GameObject panel)
    {
        var image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;

        var outline = panel.GetComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(3f, 3f);
        outline.useGraphicAlpha = true;
    }

    public static Button CreateButton(string label, RectTransform parent, Vector2 position, Vector2 size, int fontSize,
        Font font, UnityAction action)
    {
        var rect = CreateRect(label, parent, position, size,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));

        var image = rect.GetComponent<Image>();
        image.color = ButtonColor;
        var outline = rect.GetComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(3f, 3f);

        var button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.18f, 0.32f, 0.35f, 1f);
        colors.pressedColor = new Color(0.4f, 0.16f, 0.06f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        var text = CreateText(label, rect, Vector2.zero, size - new Vector2(18f, 10f), fontSize, font);
        text.color = Color.white;
        return button;
    }

    public static void SetLabel(Button button, string value)
    {
        button.GetComponentInChildren<Text>().text = value;
    }
}
