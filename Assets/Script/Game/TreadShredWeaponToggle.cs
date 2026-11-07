using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-038, D-043: the weapon button under the pause button names the weapon in hand, with a running rental's missions
/// left. Tapping it pauses the game and slides out the drawer: the cannon, the mission's own special weapon and every
/// weapon the player has used, each available, rented or LOCKED with its price. A tap puts a weapon in hand and slides
/// the drawer back; a locked one opens its rental offer. A cannon-only mission shows nothing before any special weapon
/// has been used. The HUD's touches never fire (PlayerMoverment skips touches over UI).
/// </summary>
public sealed class TreadShredWeaponToggle : MonoBehaviour
{
    private const float ButtonWidth = 300f;
    private const float RowWidth = 400f;
    private const float RowHeight = 78f;
    private const float RowGap = 8f;
    private const float SlideSeconds = 0.18f;
    private const float ClosedX = -RowWidth - 60f;

    private static TreadShredWeaponToggle current;

    private PlayerMoverment player;
    private Weapon missionWeapon;
    private Font font;
    private Button weaponButton;
    private RectTransform barBack;
    private RectTransform barFill;
    private Text left;
    private Text hint;
    private GameObject blocker;
    private RectTransform drawer;
    private float shownAt = -1f;
    private bool open;
    private float slide;

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
            new Vector2(RowWidth, 150f), typeof(RectTransform));
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -170f);
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
        weaponButton = TreadShredUi.CreateButton(string.Empty, root, Vector2.zero, new Vector2(ButtonWidth, 70f), 28, font, Toggle);
        Place((RectTransform)weaponButton.transform, 0f, 0f);
        barBack = Bar(root, new Color(1f, 1f, 1f, 0.25f));
        Place(barBack, 0f, -78f);
        barFill = Bar(barBack, TreadShredUi.AccentColor);
        left = TreadShredUi.CreateText(string.Empty, root, Vector2.zero, new Vector2(140f, 30f), 22, font);
        left.alignment = TextAnchor.MiddleLeft;
        Place(left.rectTransform, ButtonWidth * 0.72f, -70f);
        hint = TreadShredUi.CreateText(WeaponRules.SwitchHint, root, Vector2.zero, new Vector2(ButtonWidth, 30f), 22, font);
        hint.color = TreadShredUi.DetailColor;
        Place(hint.rectTransform, 0f, -100f);

        // While the drawer is open, a clear layer over the whole HUD takes stray taps and closes it.
        blocker = TreadShredUi.CreateRect("TreadShredWeaponDrawerBlocker", canvas, Vector2.zero, Vector2.zero,
            typeof(RectTransform), typeof(Image), typeof(Button)).gameObject;
        var blockerRect = (RectTransform)blocker.transform;
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        blocker.GetComponent<Button>().onClick.AddListener(() => Close(true));
        // The drawer sits beside the blocker, not inside it, so the blocker holds no buttons of its own.
        drawer = TreadShredUi.CreateRect("TreadShredWeaponDrawer", canvas, Vector2.zero, new Vector2(RowWidth, 10f),
            typeof(RectTransform));
        drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(0f, 1f);
        blocker.SetActive(false);
        drawer.gameObject.SetActive(false);

        // The hint's seconds start when play does, after the countdown (time stopped until then).
        shownAt = -1f;
        Redraw();
    }

    private static RectTransform Bar(RectTransform parent, Color color)
    {
        var bar = TreadShredUi.CreateRect("RentalBar", parent, Vector2.zero, new Vector2(ButtonWidth * 0.68f, 10f),
            typeof(RectTransform), typeof(Image));
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

    private int RentalLeft(Weapon weapon) => TreadShredProgress.Ensure().Progression.RentalLeft(weapon);

    private void Redraw()
    {
        var inHand = InHand;
        TreadShredUi.SetLabel(weaponButton, WeaponRules.Name(inHand));
        TreadShredUi.StylePrimary(weaponButton);
        // D-043-AC-06: the rental's missions left under the button while its weapon is in hand.
        var rental = inHand != missionWeapon ? RentalLeft(inHand) : 0;
        barBack.gameObject.SetActive(rental > 0);
        left.gameObject.SetActive(rental > 0);
        if (rental > 0)
        {
            barFill.anchorMin = barFill.anchorMax = barFill.pivot = new Vector2(0f, 0.5f);
            barFill.sizeDelta = new Vector2(barBack.sizeDelta.x * WeaponRental.BarFill(rental), barBack.sizeDelta.y);
            barFill.anchoredPosition = Vector2.zero;
            left.text = WeaponRental.Left(rental);
        }
        if (open)
            BuildRows();
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
        blocker.transform.SetAsLastSibling();
        drawer.SetAsLastSibling();
        BuildRows();
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

    // D-038-AC-02, D-043-AC-02: one row per weapon in the drawer.
    private void BuildRows()
    {
        foreach (Transform row in drawer)
            Destroy(row.gameObject);
        var entries = TreadShredProgress.Ensure().Progression.Drawer(missionWeapon);
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(WeaponRental.CostGems);
        var price = plan.CanSpendNow ? WeaponRental.CostGems + " GEMS" : store.PriceFor(plan.PackToBuy) ?? WeaponRental.CostGems + " GEMS";
        var top = (RectTransform)transform;
        var y = top.anchoredPosition.y - 120f;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var row = TreadShredUi.CreateButton(WeaponRules.Name(entry.Weapon), drawer, Vector2.zero, new Vector2(RowWidth, RowHeight),
                28, font, () => Choose(entry));
            Place((RectTransform)row.transform, 0f, -i * (RowHeight + RowGap));
            var label = row.GetComponentInChildren<Text>();
            label.rectTransform.offsetMin = new Vector2(16f, 26f);
            label.rectTransform.offsetMax = new Vector2(-16f, -4f);
            var status = TreadShredUi.CreateText(Status(entry, price), (RectTransform)row.transform, Vector2.zero,
                new Vector2(RowWidth - 32f, 26f), 20, font);
            status.rectTransform.anchorMin = status.rectTransform.anchorMax = status.rectTransform.pivot = new Vector2(0.5f, 0f);
            status.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            status.color = entry.State == DrawerState.Locked ? TreadShredUi.TitleColor : TreadShredUi.DetailColor;
            status.raycastTarget = false;
            if (entry.Weapon == InHand && entry.State != DrawerState.Locked)
                TreadShredUi.StylePrimary(row);
            else
                TreadShredUi.StyleStandard(row);
        }
        drawer.anchoredPosition = new Vector2(Mathf.Lerp(ClosedX, 24f, slide), y);
    }

    private string Status(DrawerEntry entry, string price)
    {
        if (entry.State == DrawerState.Locked)
            return WeaponRental.LockedLabel + " · " + price;
        if (entry.State == DrawerState.Rented)
            return WeaponRental.Left(entry.MissionsLeft);
        return entry.Weapon == InHand ? "IN HAND" : string.Empty;
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
        if (shownAt < 0f && Time.timeScale > 0f)
            shownAt = Time.unscaledTime;
        // The open drawer replaces the hint.
        hint.gameObject.SetActive(!open && missionWeapon != Weapon.Cannon
            && (shownAt < 0f || Time.unscaledTime - shownAt < WeaponRules.SwitchHintSeconds));
        // D-038-AC-02: the drawer slides out from the left edge and back.
        var target = open ? 1f : 0f;
        if (Mathf.Approximately(slide, target))
            return;
        slide = Mathf.MoveTowards(slide, target, Time.unscaledDeltaTime / SlideSeconds);
        drawer.anchoredPosition = new Vector2(Mathf.Lerp(ClosedX, 24f, slide), drawer.anchoredPosition.y);
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
