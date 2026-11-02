using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A short explanatory note over the current screen, closed by BACK or the system Back.</summary>
public sealed class TreadShredNotePanel : MonoBehaviour
{
    public static TreadShredNotePanel Show(string title, IReadOnlyList<string> lines, Font font)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredNote", 130);
        var note = root.gameObject.AddComponent<TreadShredNotePanel>();
        note.Build(root, title, lines, font);
        return note;
    }

    private void Build(RectTransform root, string title, IReadOnlyList<string> lines, Font font)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);
        var panel = TreadShredUi.CreateRect("NotePanel", backdrop, Vector2.zero, new Vector2(1240f, 640f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);
        var heading = TreadShredUi.CreateText(title, panel, new Vector2(0f, 245f), new Vector2(1180f, 70f), 52, font);
        heading.color = TreadShredUi.TitleColor;
        TreadShredUi.ShrinkToFit(heading, 32);
        // The lines share the room between the heading and BACK, centred, so any number of them fits (the COINS AND GEMS
        // note has four, D-036-AC-04).
        var step = Mathf.Min(105f, 360f / lines.Count);
        var first = 10f + step * (lines.Count - 1) / 2f;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = TreadShredUi.CreateText(lines[i], panel, new Vector2(0f, first - i * step), new Vector2(1180f, step - 13f), 30, font);
            line.color = i == lines.Count - 1 ? TreadShredUi.DetailColor : Color.white;
            TreadShredUi.ShrinkToFit(line, 20);
        }
        TreadShredUi.CreateButton("BACK", panel, new Vector2(0f, -235f), new Vector2(360f, 100f), 40, font, Close);
    }

    private void Close() => Destroy(gameObject);

    private void Start() => TreadShredBack.Opened(this, Close);

    private void OnDestroy() => TreadShredBack.Closed(this);
}
