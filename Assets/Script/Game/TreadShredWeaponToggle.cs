using System.Linq;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-038, D-043: the weapon button under the pause button names the weapon in hand, with (TAP TO CHANGE) inside it
/// and a running rental's missions left under it. Tapping it pauses the game and slides the other weapons out in one
/// row beside it, just under the HUD bar, so the battlefield stays clear: the cannon, the mission's own special weapon
/// and every weapon the player has used, each available, rented or LOCKED with its price. A tap puts a weapon in hand
/// and slides the row back; a locked one opens its rental offer. A cannon-only mission shows nothing before any
/// special weapon has been used. The HUD's touches never fire (PlayerMoverment skips touches over UI).
/// </summary>
public sealed class TreadShredWeaponToggle : MonoBehaviour
{
    private const float ButtonWidth = 320f;
    private const float ButtonHeight = 96f;
    private const float CellWidth = 290f;
    private const float CellGap = 10f;
    private const float Left = 24f;
    private const float Top = -170f;
    private const float SlideSeconds = 0.18f;

    private static TreadShredWeaponToggle current;

    private PlayerMoverment player;
    private Weapon missionWeapon;
    private Font font;
    private Button weaponButton;
    private RectTransform barBack;
    private RectTransform barFill;
    private Text missionsLeft;
    private GameObject blocker;
    private RectTransform drawer;
    private bool open;
    private float slide;
    private bool started;

    /// <summary>Puts the weapon button on the HUD; returns null when there is nothing to choose (D-038-AC-01).</summary>
    public static TreadShredWeaponToggle Attach(PlayerMoverment player, Weapon missionWeapon, Font font)
    {
        Hide();
        if (TreadShredProgress.Ensure().Progression.Drawer(missionWeapon).Count < 2)
            return null;
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Weapon] HUD Canvas not found; the mission plays on its own weapon with no drawer.");
            return null;
        }

        var root = TreadShredUi.CreateRect("TreadShredWeaponToggle", (RectTransform)canvas.transform, Vector2.zero,
            new Vector2(ButtonWidth, 130f), typeof(RectTransform));
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(Left, Top);
        current = root.gameObject.AddComponent<TreadShredWeaponToggle>();
        current.Build(root, (RectTransform)canvas.transform, player, missionWeapon, font);
        return current;
    }

    /// <summary>Redraws after the weapon in hand or a rental changed.</summary>
    public static void Refresh()
    {
        if (current != null)
            current.Redraw();
    }

    /// <summary>D-038-AC-04: hidden while the game is paused.</summary>
    public static void SetVisible(bool visible)
    {
        if (current == null)
            return;
        if (!visible)
        {
            current.Close(false);
            current.slide = 0f;
            current.blocker.SetActive(false);
            current.drawer.gameObject.SetActive(false);
        }
        current.gameObject.SetActive(visible);
    }

    /// <summary>D-038-AC-04: gone once the mission ends.</summary>
    public static void Hide()
    {
        if (current == null)
            return;
        if (current.blocker != null)
            Destroy(current.blocker);
        if (current.drawer != null)
            Destroy(current.drawer.gameObject);
        Destroy(current.gameObject);
        current = null;
    }

    private void Build(RectTransform root, RectTransform canvas, PlayerMoverment owner, Weapon weapon, Font hudFont)
    {
        player = owner;
        missionWeapon = weapon;
        font = hudFont;
        weaponButton = Cell(root, ButtonWidth, WeaponRules.SwitchHint, Toggle, out _);
        Place((RectTransform)weaponButton.transform, 0f, 0f);
        barBack = Bar(root, ButtonWidth * 0.62f, new Color(1f, 1f, 1f, 0.25f));
        Place(barBack, 0f, -ButtonHeight - 10f);
        barFill = Bar(barBack, ButtonWidth * 0.62f, TreadShredUi.AccentColor);
        missionsLeft = TreadShredUi.CreateText(string.Empty, root, Vector2.zero, new Vector2(ButtonWidth * 0.36f, 30f), 22, font);
        missionsLeft.alignment = TextAnchor.MiddleLeft;
        Place(missionsLeft.rectTransform, ButtonWidth * 0.66f, -ButtonHeight);

        // While the row is out, a clear layer over the rest of the HUD takes stray taps and closes it; the row sits
        // beside that layer, not inside it, and slides out from behind the button.
        blocker = TreadShredUi.CreateRect("TreadShredWeaponDrawerBlocker", canvas, Vector2.zero, Vector2.zero,
            typeof(RectTransform), typeof(Image), typeof(Button)).gameObject;
        var blockerRect = (RectTransform)blocker.transform;
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        blocker.GetComponent<Button>().onClick.AddListener(() => Close(true));
        drawer = TreadShredUi.CreateRect("TreadShredWeaponDrawer", canvas, Vector2.zero, new Vector2(10f, ButtonHeight),
            typeof(RectTransform));
        drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(0f, 1f);
        blocker.SetActive(false);
        drawer.gameObject.SetActive(false);
        // The countdown already holds the game still; the button appears when play starts, so the row never opens
        // over the countdown's title.
        weaponButton.gameObject.SetActive(false);
        Redraw();
    }

    // A weapon button or cell: the name on top, a second line under it (the instruction, or the rental's status).
    private Button Cell(RectTransform parent, float width, string second, UnityEngine.Events.UnityAction tap, out Text secondLine)
    {
        var button = TreadShredUi.CreateButton(string.Empty, parent, Vector2.zero, new Vector2(width, ButtonHeight), 30, font, tap);
        var name = button.GetComponentInChildren<Text>();
        name.rectTransform.sizeDelta = new Vector2(width - 18f, 46f);
        name.rectTransform.anchoredPosition = new Vector2(0f, 17f);
        secondLine = TreadShredUi.CreateText(second, (RectTransform)button.transform, new Vector2(0f, -24f),
            new Vector2(width - 18f, 32f), 24, font);
        secondLine.raycastTarget = false;
        return button;
    }

    private static RectTransform Bar(RectTransform parent, float width, Color color)
    {
        var bar = TreadShredUi.CreateRect("RentalBar", parent, Vector2.zero, new Vector2(width, 10f), typeof(RectTransform),
            typeof(Image));
        bar.GetComponent<Image>().color = color;
        bar.GetComponent<Image>().raycastTarget = false;
        return bar;
    }

    private static void Place(RectTransform rect, float x, float y)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private Weapon InHand => player != null ? player.WeaponInHand : missionWeapon;

    private void Redraw()
    {
        var inHand = InHand;
        TreadShredUi.SetLabel(weaponButton, WeaponRules.Name(inHand));
        TreadShredUi.StylePrimary(weaponButton);
        // D-043-AC-06: the rental's missions left under the button while its weapon is in hand.
        var rental = inHand != missionWeapon ? TreadShredProgress.Ensure().Progression.RentalLeft(inHand) : 0;
        barBack.gameObject.SetActive(started && rental > 0);
        missionsLeft.gameObject.SetActive(started && rental > 0);
        if (rental > 0)
        {
            ShowFill(barFill, barBack.sizeDelta.x, rental);
            missionsLeft.text = WeaponRental.Left(rental);
        }
        if (open)
            BuildRow();
    }

    private static void ShowFill(RectTransform fill, float width, int missions)
    {
        fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
        fill.sizeDelta = new Vector2(width * WeaponRental.BarFill(missions), fill.sizeDelta.y);
        fill.anchoredPosition = Vector2.zero;
    }

    private void Toggle()
    {
        if (open)
            Close(true);
        else
            Open();
    }

    private void Open()
    {
        open = true;
        Time.timeScale = 0f;
        blocker.SetActive(true);
        drawer.gameObject.SetActive(true);
        // Under the button and above the rest of the HUD: the clear layer, then the row.
        var index = transform.GetSiblingIndex();
        blocker.transform.SetSiblingIndex(index);
        drawer.SetSiblingIndex(index + 1);
        BuildRow();
        TreadShredBack.Opened(this, () => Close(true));
    }

    private void Close(bool resume)
    {
        if (!open)
            return;
        open = false;
        TreadShredBack.Closed(this);
        if (resume)
            Time.timeScale = 1f;
    }

    // D-038-AC-02, D-043-AC-02: one cell per weapon other than the one in hand, in one row.
    private void BuildRow()
    {
        foreach (Transform cell in drawer)
            Destroy(cell.gameObject);
        var entries = TreadShredProgress.Ensure().Progression.Drawer(missionWeapon).Where(e => e.Weapon != InHand).ToList();
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(WeaponRental.CostGems);
        var price = plan.CanSpendNow ? WeaponRental.CostGems + " GEMS" : store.PriceFor(plan.PackToBuy) ?? WeaponRental.CostGems + " GEMS";
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var cell = Cell(drawer, CellWidth, Status(entry, price), () => Choose(entry), out var status);
            TreadShredUi.SetLabel(cell, WeaponRules.Name(entry.Weapon));
            Place((RectTransform)cell.transform, i * (CellWidth + CellGap), 0f);
            status.color = entry.State == DrawerState.Locked ? TreadShredUi.TitleColor : TreadShredUi.DetailColor;
            TreadShredUi.StyleStandard(cell);
            if (entry.State == DrawerState.Rented)
            {
                var back = Bar((RectTransform)cell.transform, CellWidth - 40f, new Color(1f, 1f, 1f, 0.25f));
                back.anchorMin = back.anchorMax = back.pivot = new Vector2(0.5f, 0f);
                back.anchoredPosition = new Vector2(0f, 4f);
                back.sizeDelta = new Vector2(CellWidth - 40f, 5f);
                var fill = Bar(back, CellWidth - 40f, TreadShredUi.AccentColor);
                fill.sizeDelta = new Vector2(fill.sizeDelta.x, 5f);
                ShowFill(fill, CellWidth - 40f, entry.MissionsLeft);
            }
        }
        drawer.sizeDelta = new Vector2(Mathf.Max(10f, entries.Count * (CellWidth + CellGap)), ButtonHeight);
        drawer.anchoredPosition = new Vector2(SlidX(), Top);
    }

    // Out: beside the button; in: tucked behind it.
    private float SlidX() => Mathf.Lerp(Left, Left + ButtonWidth + CellGap, slide);

    private static string Status(DrawerEntry entry, string price)
    {
        if (entry.State == DrawerState.Locked)
            return WeaponRental.LockedLabel + " · " + price;
        return entry.State == DrawerState.Rented ? WeaponRental.Left(entry.MissionsLeft) : string.Empty;
    }

    private void Choose(DrawerEntry entry)
    {
        if (entry.State == DrawerState.Locked)
        {
            // The offer keeps the game paused; renting or NOT NOW resumes it (D-043-AC-03).
            Close(false);
            TreadShredRentalOffer.Show(player, entry.Weapon, font);
            return;
        }
        if (player != null)
            player.SelectWeapon(entry.Weapon);
        Close(true);
        Redraw();
    }

    private void Update()
    {
        if (!started && Time.timeScale > 0f)
        {
            started = true;
            weaponButton.gameObject.SetActive(true);
            Redraw();
        }
        // D-038-AC-02: the row slides out from behind the button and back.
        var target = open ? 1f : 0f;
        if (Mathf.Approximately(slide, target))
            return;
        slide = Mathf.MoveTowards(slide, target, Time.unscaledDeltaTime / SlideSeconds);
        drawer.anchoredPosition = new Vector2(SlidX(), Top);
        if (!open && Mathf.Approximately(slide, 0f))
        {
            blocker.SetActive(false);
            drawer.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        if (blocker != null)
            Destroy(blocker);
        if (drawer != null)
            Destroy(drawer.gameObject);
        if (current == this)
            current = null;
    }
}
