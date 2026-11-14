using System.Collections;
using System.Collections.Generic;
using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-047-AC-04: the War Chest interstitial, opened over the failure screen once a session while the chest is on sale.
/// Each item lands one at a time with its worth, then the total, the price and the buttons; NOT NOW (and Back) wait
/// until the last item has landed. Closes itself once the chest is bought. D-047-AC-05: after a loss it also offers
/// the free test drive, and when a drive ends it opens again to sell what the player just used.
/// </summary>
public sealed class TreadShredWarChestOffer : MonoBehaviour
{
    /// <summary>D-047-AC-04: the chest has been offered this session; it is offered at most once per launch.</summary>
    public static bool ShownThisSession { get; set; }

    /// <summary>Every item, the total and both buttons have landed.</summary>
    public bool Revealed { get; private set; }

    private readonly List<GameObject> reveal = new List<GameObject>();
    private GameObject notNow;
    private Text statusText;
    private Button buyButton;
    private System.Action<string> showStatus;
    private Font font;

    // After a test drive: what closing it does (replay the mission without the gear).
    private System.Action onClosed;

    /// <summary>Opens the interstitial after a lost mission when the War Chest is on sale and it has not been offered this session.</summary>
    public static bool ShowAfterLoss(Font font)
    {
        if (ShownThisSession || !TreadShredStore.Ensure().WarChestOnSale || FindFirstObjectByType<TreadShredWarChestOffer>() != null)
            return false;
        ShownThisSession = true;
        var root = TreadShredUi.CreateOverlay("TreadShredWarChestOffer", 110);
        root.gameObject.AddComponent<TreadShredWarChestOffer>().Build(root, font, false);
        return true;
    }

    /// <summary>D-047-AC-05: the reveal once a test drive ends; closing it (or buying) runs <paramref name="closed"/>.</summary>
    public static void ShowAfterTestDrive(Font font, System.Action closed)
    {
        if (!TreadShredStore.Ensure().WarChestOnSale)
        {
            closed();
            return;
        }
        var root = TreadShredUi.CreateOverlay("TreadShredWarChestOffer", 110);
        var offer = root.gameObject.AddComponent<TreadShredWarChestOffer>();
        offer.onClosed = closed;
        offer.Build(root, font, true);
    }

    private void Build(RectTransform root, Font font, bool afterDrive)
    {
        this.font = font;
        var store = TreadShredStore.Ensure();
        var unitPrice = store.PriceFor(GemCatalog.Packs[0]);
        var price = store.PriceFor(GemCatalog.WarChest);
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.9f);
        var panel = TreadShredUi.CreateRect("WarChestPanel", backdrop, Vector2.zero, new Vector2(1520f, 980f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);
        TreadShredUi.CreateText(WarChest.Title, panel, new Vector2(0f, 420f), new Vector2(1400f, 100f), 84, font).color =
            TreadShredUi.TitleColor;
        TreadShredUi.CreateText(afterDrive ? WarChest.AfterDriveSubtitle : WarChest.Subtitle, panel, new Vector2(0f, 352f),
            new Vector2(1400f, 40f), 30, font).color =
            TreadShredUi.AccentColor;

        // Four items down the left, three down the right, in the order they land.
        for (var index = 0; index < WarChest.Items.Count; index++)
        {
            var item = WarChest.Items[index];
            var position = new Vector2(index < 4 ? -370f : 370f, 260f - 85f * (index % 4));
            var row = TreadShredUi.CreateRect("WarChestItem" + index, panel, position, new Vector2(700f, 74f),
                typeof(RectTransform), typeof(Image));
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            var line = TreadShredUi.CreateText(WarChest.Line(item), row, new Vector2(-80f, 0f), new Vector2(520f, 70f), 36, font);
            line.alignment = TextAnchor.MiddleLeft;
            line.color = Color.white;
            TreadShredUi.ShrinkToFit(line, 24);
            var value = TreadShredUi.CreateText(WarChest.Value(item, unitPrice), row, new Vector2(270f, 0f), new Vector2(140f, 70f), 32,
                font);
            value.alignment = TextAnchor.MiddleRight;
            value.color = TreadShredUi.TitleColor;
            Hide(row.gameObject);
        }

        var weapons = TreadShredUi.CreateText(WarChest.WeaponsNote, panel, new Vector2(0f, -70f), new Vector2(1400f, 34f), 26, font);
        weapons.color = TreadShredUi.DetailColor;
        Hide(weapons.gameObject);
        var total = TreadShredUi.CreateText(WarChest.TotalValue(unitPrice), panel, new Vector2(0f, -132f), new Vector2(1400f, 56f), 44,
            font);
        total.color = Color.white;
        Hide(total.gameObject);
        var yours = TreadShredUi.CreateText(WarChest.YoursFor(price), panel, new Vector2(0f, -198f), new Vector2(1400f, 68f), 56, font);
        yours.color = TreadShredUi.AccentColor;
        Hide(yours.gameObject);
        if (afterDrive)
        {
            buyButton = TreadShredUi.CreateButton(WarChest.BuyButton(price), panel, new Vector2(0f, -300f), new Vector2(780f, 110f), 40,
                font, Buy);
            TreadShredUi.StylePrimary(buyButton);
            Hide(buyButton.gameObject);
        }
        else
        {
            // Experience first: the free drive leads, the purchase stands beside it.
            var drive = TreadShredUi.CreateButton(WarChest.TestDriveButton, panel, new Vector2(-345f, -300f), new Vector2(660f, 110f),
                40, font, TestDrive);
            TreadShredUi.StylePrimary(drive);
            Hide(drive.gameObject);
            buyButton = TreadShredUi.CreateButton(WarChest.BuyButton(price), panel, new Vector2(345f, -300f), new Vector2(660f, 110f), 34,
                font, Buy);
            TreadShredUi.ShrinkToFit(buyButton.GetComponentInChildren<Text>(), 24);
            Hide(buyButton.gameObject);
        }
        statusText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -375f), new Vector2(1400f, 30f), 24, font);
        statusText.color = TreadShredUi.DetailColor;
        notNow = TreadShredUi.CreateButton("NOT NOW", panel, new Vector2(0f, -435f), new Vector2(360f, 70f), 28, font, Close)
            .gameObject;
        notNow.SetActive(false);
        showStatus = status => statusText.text = status;
        TreadShredAnalytics.Log(GameEvents.OfferShown("war_chest", store.Wallet.Balance));
        StartCoroutine(Reveal());
    }

    private void Hide(GameObject element)
    {
        element.SetActive(false);
        reveal.Add(element);
    }

    // Unscaled time: the result screen may have stopped the game's clock.
    private IEnumerator Reveal()
    {
        var vibration = PlayerPrefs.GetFloat("Vibrate") == 0; // LoadLv.VibrationEnabled's setting
        for (var index = 0; index < reveal.Count; index++)
        {
            yield return new WaitForSecondsRealtime(WarChest.RevealSeconds);
            reveal[index].SetActive(true);
            StartCoroutine(Land(reveal[index].transform));
            TreadShredHaptics.Pulse(index < WarChest.Items.Count ? HapticPulse.Hit : HapticPulse.Kill, vibration);
        }
        yield return new WaitForSecondsRealtime(WarChest.RevealSeconds);
        notNow.SetActive(true);
        Revealed = true;
    }

    // Each item slams in: oversized, then down to its size.
    private static IEnumerator Land(Transform element)
    {
        const float duration = 0.18f;
        for (var elapsed = 0f; elapsed < duration && element != null; elapsed += Time.unscaledDeltaTime)
        {
            element.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, elapsed / duration);
            yield return null;
        }
        if (element != null)
            element.localScale = Vector3.one;
    }

    private void Update()
    {
        // Bought (here or anywhere else): the chest is no longer on sale.
        if (!TreadShredStore.Ensure().WarChestOnSale)
        {
            Close();
            return;
        }
        buyButton.interactable = !TreadShredStore.Ensure().PurchaseInProgress;
    }

    private void TestDrive()
    {
        var driveFont = font;
        Destroy(gameObject);
        TreadShredTestDrive.Begin(driveFont);
    }

    private void Buy()
    {
        statusText.text = string.Empty;
        TreadShredStore.Ensure().BuyPack(GemCatalog.WarChest, Close, showStatus);
    }

    // D-047-AC-04: Back closes it only once everything has landed, like NOT NOW.
    private void Start() => TreadShredBack.Opened(this, () =>
    {
        if (Revealed)
            Close();
    });

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        // Closed while the sheet was opening: a delivery still equips the chest (D-011).
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Release(showStatus);
    }

    private void Close()
    {
        if (this == null)
            return;
        Destroy(gameObject);
        var closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }
}
