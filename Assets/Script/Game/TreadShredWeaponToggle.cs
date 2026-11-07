using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-038: on a mission whose tank carries a special weapon, a two-way toggle under the pause button names the cannon
/// and that weapon, gives the one in hand the solid accent style and switches on a tap. A mission with only the cannon
/// shows no control. The HUD's touches never fire (PlayerMoverment skips touches over UI).
/// </summary>
public sealed class TreadShredWeaponToggle : MonoBehaviour
{
    private static TreadShredWeaponToggle current;

    private PlayerMoverment player;
    private Weapon missionWeapon;
    private Button cannonButton;
    private Button specialButton;
    private Text hint;
    private float shownAt;
    private bool locked;

    /// <summary>Puts the toggle on the HUD for the mission's weapon; returns null when the mission has only the cannon.</summary>
    public static TreadShredWeaponToggle Attach(PlayerMoverment player, Weapon missionWeapon, Font font)
    {
        Hide();
        if (!WeaponRules.CanSwitch(missionWeapon))
            return null;
        return Create(player, missionWeapon, font, false);
    }

    /// <summary>D-043-AC-02: on a cannon-only mission, CANNON beside a weapon the player has used, marked LOCKED in text
    /// until it is rented; a tap on it opens the rental offer.</summary>
    public static TreadShredWeaponToggle AttachLocked(PlayerMoverment player, Weapon weapon, Font font)
    {
        Hide();
        return Create(player, weapon, font, true);
    }

    private static TreadShredWeaponToggle Create(PlayerMoverment player, Weapon missionWeapon, Font font, bool locked)
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Weapon] HUD Canvas not found; the mission plays on its own weapon with no toggle.");
            return null;
        }

        var root = TreadShredUi.CreateRect("TreadShredWeaponToggle", (RectTransform)canvas.transform, Vector2.zero,
            new Vector2(420f, 130f), typeof(RectTransform));
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -170f);
        current = root.gameObject.AddComponent<TreadShredWeaponToggle>();
        current.Build(root, player, missionWeapon, font, locked);
        return current;
    }

    /// <summary>D-038-AC-04: hidden while the game is paused.</summary>
    public static void SetVisible(bool visible)
    {
        if (current != null)
            current.gameObject.SetActive(visible);
    }

    /// <summary>D-038-AC-04: gone once the mission ends.</summary>
    public static void Hide()
    {
        if (current != null)
            Destroy(current.gameObject);
        current = null;
    }

    private void Build(RectTransform root, PlayerMoverment owner, Weapon weapon, Font font, bool rentable)
    {
        player = owner;
        missionWeapon = weapon;
        locked = rentable;
        cannonButton = TreadShredUi.CreateButton(WeaponRules.Name(Weapon.Cannon), root, new Vector2(-105f, 30f),
            new Vector2(200f, 70f), 26, font, () => Select(Weapon.Cannon));
        specialButton = TreadShredUi.CreateButton(WeaponRules.Name(missionWeapon), root, new Vector2(105f, 30f),
            new Vector2(200f, 70f), 26, font, () =>
            {
                if (locked)
                    TreadShredRentalOffer.Show(player, missionWeapon, font);
                else
                    Select(missionWeapon);
            });
        hint = locked
            ? TreadShredUi.CreateText(WeaponRental.LockedLabel, root, new Vector2(105f, -32f), new Vector2(200f, 34f), 22, font)
            : TreadShredUi.CreateText(WeaponRules.SwitchHint, root, new Vector2(0f, -32f), new Vector2(420f, 34f), 22, font);
        hint.color = TreadShredUi.DetailColor;
        shownAt = Time.unscaledTime;
        Refresh();
    }

    private void Select(Weapon weapon)
    {
        if (player == null)
            return;
        player.SelectWeapon(weapon);
        Refresh();
    }

    private void Refresh()
    {
        var inHand = player != null ? player.WeaponInHand : missionWeapon;
        if (inHand == Weapon.Cannon)
        {
            TreadShredUi.StylePrimary(cannonButton);
            TreadShredUi.StyleStandard(specialButton);
        }
        else
        {
            TreadShredUi.StylePrimary(specialButton);
            TreadShredUi.StyleStandard(cannonButton);
        }
    }

    private void Update()
    {
        hint.gameObject.SetActive(locked || Time.unscaledTime - shownAt < WeaponRules.SwitchHintSeconds);
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
