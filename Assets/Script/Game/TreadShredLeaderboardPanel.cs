using System.Collections.Generic;
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
    private Button back;
    private Text hint;
    // What one board drew, cleared when it reloads.
    private readonly List<GameObject> pageItems = new List<GameObject>();
    // Set while the player is in Play Games' settings; the board reloads when they come back.
    private bool awaitingProfile;

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
        back = TreadShredUi.CreateButton(LeaderboardRules.Back, root, new Vector2(0f, -445f), new Vector2(420f, 100f), 40, font,
            Close);
    }

    private void Load()
    {
        foreach (var item in pageItems)
            Destroy(item);
        pageItems.Clear();
        ((RectTransform)back.transform).anchoredPosition = new Vector2(0f, -445f);
        ShowStatus(LeaderboardRules.Loading);
        TreadShredLeaderboard.RequestTop(page =>
        {
            if (this == null)
                return;
            if (page == null)
                ShowStatus(LeaderboardRules.Unavailable);
            else if (page.Top.Count == 0 && !page.ShowsPlayerRow)
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
        if (page.ShowsPlayerRow)
            // Ranks below the top 25 run to several digits, so this row's rank gets more room.
            AddRow(page.Player.Value, 0f, FirstRowY - RowsPerColumn * RowHeight - 28f, 140f);
        if (page.PlayerHidden && Application.platform == RuntimePlatform.Android)
            ShowPrivateProfileHelp();
    }

    // D-032-AC-02: Play Games makes new profiles private, which keeps the player off everyone else's board. The screen
    // says so and offers the Play Games settings that change it, beside BACK, framed as the way to compete.
    private void ShowPrivateProfileHelp()
    {
        var note = Track(TreadShredUi.CreateText(LeaderboardRules.PrivateProfile, root,
            new Vector2(0f, FirstRowY - RowsPerColumn * RowHeight - 72f), new Vector2(1400f, 34f), 22, font));
        note.color = TreadShredUi.DetailColor;
        ((RectTransform)back.transform).anchoredPosition = new Vector2(-280f, -445f);
        Track(TreadShredUi.CreateButton(LeaderboardRules.MakePublicButton, root, new Vector2(270f, -445f),
            new Vector2(560f, 100f), 34, font, OpenProfileSettings));
        hint = Track(TreadShredUi.CreateText(LeaderboardRules.MakePublicHint, root, new Vector2(0f, -514f),
            new Vector2(1400f, 30f), 20, font));
        hint.color = TreadShredUi.DetailColor;
    }

    private void OpenProfileSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (TreadShredPlayGames.OpenProfileSettings())
        {
            awaitingProfile = true;
            return;
        }
#endif
        // Neither Play Games screen opened: the steps to do it by hand instead.
        hint.text = LeaderboardRules.MakeProfilePublic;
    }

    // Back from Play Games' settings: reload, so a player who made the profile public sees their name on the board.
    private void OnApplicationFocus(bool focused)
    {
        if (!focused || !awaitingProfile)
            return;
        awaitingProfile = false;
        Load();
    }

    private T Track<T>(T item) where T : Component
    {
        pageItems.Add(item.gameObject);
        return item;
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
        var text = Track(TreadShredUi.CreateText(value, root, new Vector2(left + width / 2f, y), new Vector2(width, RowHeight), 28,
            font));
        text.alignment = alignment;
        text.color = color;
        // Nicknames run to 18 characters of any width; a cell never spills into the next column.
        TreadShredUi.ShrinkToFit(text, 14);
    }

    private void Start() => TreadShredBack.Opened(this, Close);

    private void Close()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        if (open == this)
            open = null;
    }
}
