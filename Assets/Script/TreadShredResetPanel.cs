using TreadShred.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// D-044: RESET GAME, opened from Settings. It names what is erased, and ERASE EVERYTHING stays off until the player
/// types RESET, so a stray tap can never erase a game. Erasing starts the game over from its start screen, as at a
/// fresh install's first launch.
/// </summary>
public sealed class TreadShredResetPanel : MonoBehaviour
{
    private const float FieldWidth = 520f;

    private InputField typed;
    private Button erase;

    public static TreadShredResetPanel Show(Font font)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredReset", 110);
        var panel = root.gameObject.AddComponent<TreadShredResetPanel>();
        panel.Build(root, font);
        return panel;
    }

    private void Build(RectTransform root, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.96f);
        TreadShredUi.CreateText(GameReset.Title, backdrop, new Vector2(0f, 420f), new Vector2(900f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;
        Wrapped(TreadShredUi.CreateText(GameReset.Erases, backdrop, new Vector2(0f, 315f), new Vector2(1300f, 100f), 34, font),
            Color.white);
        Wrapped(TreadShredUi.CreateText(GameReset.Reach(Application.platform == RuntimePlatform.Android), backdrop,
            new Vector2(0f, 215f), new Vector2(1300f, 80f), 30, font), TreadShredUi.AccentColor);
        TreadShredUi.CreateText(GameReset.Prompt, backdrop, new Vector2(0f, 135f), new Vector2(900f, 44f), 34, font)
            .color = TreadShredUi.DetailColor;
        typed = Field(backdrop, font, new Vector2(0f, 45f));
        typed.onValueChanged.AddListener(value =>
        {
            // The game's text is all capitals; text set by code is raised too, then this runs again.
            var upper = value.ToUpperInvariant();
            if (upper != value)
            {
                typed.text = upper;
                return;
            }
            Refresh();
        });

        // The buttons sit above the middle of the screen so the keyboard leaves them in view.
        TreadShredUi.CreateButton(GameReset.Keep, backdrop, new Vector2(-300f, -90f), new Vector2(520f, 110f), 40, font, Close);
        erase = TreadShredUi.CreateButton(GameReset.Erase, backdrop, new Vector2(300f, -90f), new Vector2(520f, 110f), 40, font, Erase);
        Refresh();
    }

    // Long lines wrap inside their width on every screen shape.
    private static void Wrapped(Text text, Color color)
    {
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.color = color;
    }

    // A single-line field for the confirmation word, outlined like the game's buttons.
    private static InputField Field(RectTransform parent, Font font, Vector2 position)
    {
        var box = TreadShredUi.CreateRect("TreadShredResetTyped", parent, position, new Vector2(FieldWidth, 100f),
            typeof(RectTransform), typeof(Image), typeof(Outline), typeof(InputField));
        var background = box.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);
        box.GetComponent<Outline>().effectColor = TreadShredUi.TitleColor;
        box.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        var text = TreadShredUi.CreateText(string.Empty, box, Vector2.zero, new Vector2(FieldWidth - 40f, 80f), 48, font);
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        var placeholder = TreadShredUi.CreateText("TYPE HERE", box, Vector2.zero, new Vector2(FieldWidth - 40f, 80f), 40, font);
        placeholder.color = new Color(1f, 1f, 1f, 0.3f);
        var field = box.GetComponent<InputField>();
        field.targetGraphic = background;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = 12;
        field.keyboardType = TouchScreenKeyboardType.Default;
        // Typed letters show as capitals, like every word in the game.
        field.onValidateInput = (current, index, added) => char.ToUpperInvariant(added);
        return field;
    }

    // D-044-AC-02: ERASE EVERYTHING works only once RESET is typed.
    private void Refresh()
    {
        var confirmed = GameReset.Confirms(typed.text);
        erase.interactable = confirmed;
        if (confirmed)
            TreadShredUi.StylePrimary(erase);
        else
            TreadShredUi.StyleStandard(erase);
        erase.GetComponentInChildren<Text>().color = confirmed ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    private void Erase()
    {
        if (!GameReset.Confirms(typed.text))
            return;
        TreadShredGameReset.EraseEverything();
        // D-044-AC-04: start over from the start screen, as at a fresh install's first launch.
        Time.timeScale = 1f;
        SceneManager.LoadScene("Start");
    }

    private void Start() => TreadShredBack.Opened(this, Close);

    private void Close() => Destroy(gameObject);

    private void OnDestroy() => TreadShredBack.Closed(this);
}
