using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-032-AC-02: the in-game leaderboard screen, opened from the start screen
/// and every mission result screen. It shows the platform leaderboard's top 25
/// and the player's own rank, and BACK returns straight to the screen beneath.
/// </summary>
public sealed class TreadShredLeaderboardPanel : MonoBehaviour
{
    private const int RowsPerColumn = 13;
    private const float RowHeight = 46f;
    private const float FirstRowY = 300f;
    private const float ColumnX = 360f;

    private static TreadShredLeaderboardPanel open;
    private static Font waitingFont;

    private Font font;
    private RectTransform root;
    private Text status;

    /// <summary>Opens the screen, or Apple's sign-in first when the player is signed out and iOS offers it.</summary>
    public static void OpenFrom(Font font)
    {
        if (open != null)
            return;
        var result = TreadShredLeaderboard.Open();
        if (result == LeaderboardOpenResult.SignInShown)
        {
            // Opens on its own once Apple's sign-in finishes.
            waitingFont = font;
            TreadShredLeaderboard.SignedInNow -= OpenAfterSignIn;
            TreadShredLeaderboard.SignedInNow += OpenAfterSignIn;
            return;
        }
        var panel = Create(font);
        if (result == LeaderboardOpenResult.Ready)
            panel.Load();
        else
            panel.ShowStatus(LeaderboardRules.SignInFromSettings);
    }

    /// <summary>Shows a page directly (store capture mode layout shots).</summary>
    public static void OpenWithPage(Font font, LeaderboardPage page)
    {
        if (open == null)
            Create(font).ShowPage(page);
    }

    private static void OpenAfterSignIn()
    {
        TreadShredLeaderboard.SignedInNow -= OpenAfterSignIn;
        OpenFrom(waitingFont);
    }

    private static TreadShredLeaderboardPanel Create(Font font)
    {
        var overlay = TreadShredUi.CreateOverlay("TreadShredLeaderboard", 125);
        open = overlay.gameObject.AddComponent<TreadShredLeaderboardPanel>();
        open.font = font;
        open.Build(overlay);
        return open;
    }

    private void Build(RectTransform overlay)
    {
        root = TreadShredUi.CreateBackdrop(overlay, 0.96f);
        TreadShredUi.CreateText(LeaderboardRules.Title, root, new Vector2(0f, 455f), new Vector2(1200f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText(LeaderboardRules.Subtitle, root, new Vector2(0f, 392f), new Vector2(1200f, 40f), 28, font)
            .color = TreadShredUi.DetailColor;
        status = TreadShredUi.CreateText(string.Empty, root, new Vector2(0f, 40f), new Vector2(1300f, 60f), 36, font);
        status.color = Color.white;
        TreadShredUi.CreateButton(LeaderboardRules.Back, root, new Vector2(0f, -445f), new Vector2(420f, 100f), 40, font,
            Close);
    }

    private void Load()
    {
        ShowStatus(LeaderboardRules.Loading);
        TreadShredLeaderboard.RequestTop(page =>
        {
            if (this == null)
                return;
            if (page == null)
                ShowStatus(LeaderboardRules.Unavailable);
            else if (page.Top.Count == 0)
                ShowStatus(LeaderboardRules.Empty);
            else
                ShowPage(page);
        });
    }

    private void ShowStatus(string message)
    {
        status.text = message;
    }

    private void ShowPage(LeaderboardPage page)
    {
        status.text = string.Empty;
        for (var i = 0; i < page.Top.Count && i < RowsPerColumn * 2; i++)
        {
            var column = i / RowsPerColumn == 0 ? -ColumnX : ColumnX;
            AddRow(page.Top[i], column, FirstRowY - (i % RowsPerColumn) * RowHeight, 0f);
        }
        if (page.PlayerBelowTop)
            // Ranks below the top 25 run to several digits, so this row's rank gets more room.
            AddRow(page.Player.Value, 0f, FirstRowY - RowsPerColumn * RowHeight - 28f, 140f);
    }

    // Rank, platform nickname and score; the player's own row is gold.
    private void AddRow(LeaderboardEntry entry, float x, float y, float extraRankWidth)
    {
        var color = entry.IsPlayer ? TreadShredUi.TitleColor : Color.white;
        // Each column spans x +/- 350 so two columns fit a 4:3 iPad (1440 units wide).
        Cell(LeaderboardPage.RankText(entry), x - 370f - extraRankWidth, y, 130f + extraRankWidth,
            TextAnchor.MiddleRight, color);
        Cell(LeaderboardPage.NameText(entry), x - 215f, y, 340f, TextAnchor.MiddleLeft, color);
        Cell(LeaderboardPage.ScoreText(entry), x + 130f, y, 210f, TextAnchor.MiddleRight, color);
    }

    private void Cell(string value, float left, float y, float width, TextAnchor alignment, Color color)
    {
        var text = TreadShredUi.CreateText(value, root, new Vector2(left + width / 2f, y), new Vector2(width, RowHeight), 28,
            font);
        text.alignment = alignment;
        text.color = color;
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (open == this)
            open = null;
    }
}
