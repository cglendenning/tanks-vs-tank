using System.Collections;
using System.Collections.Generic;
using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-047-AC-04, D-037-AC-04: a one-time pack's reveal, opened as an interstitial: the War Chest (below) and the Starter
/// Pack (TreadShredStarterOffer). Each item lands one at a time with its worth, then the total, the price and the
/// buttons; NOT NOW (and Back) wait until the last item has landed. Closes itself once the pack is bought or off sale.
/// Over a mission it can lead with the free test drive (D-047-AC-05, D-037-AC-08), and when a drive ends it opens again
/// to sell what the player just used.
/// </summary>
public abstract class TreadShredStackOffer : MonoBehaviour
{
    /// <summary>Every item, the total and both buttons have landed.</summary>
    public bool Revealed { get; private set; }

    private readonly List<GameObject> reveal = new List<GameObject>();
    private GameObject notNow;
    private Text statusText;
    private Button buyButton;
    private System.Action<string> showStatus;
    private Font font;

    /// <summary>The line under the title, which a pack with a countdown rewrites each frame.</summary>
    protected Text SubtitleText { get; private set; }

    // After a test drive: what closing it does (replay the mission without the gear, or go on).
    protected System.Action onClosed;

    protected abstract SupplyStack Stack { get; }

    /// <summary>The pack can still be bought; once it cannot, the reveal closes.</summary>
    protected abstract bool OnSale { get; }

    /// <summary>Records the offer being shown (D-021).</summary>
    protected abstract void Shown(TreadShredStore store);

    /// <summary>Starts the free test drive of the mission on screen.</summary>
    protected abstract void TestDrive(Font driveFont);

    /// <summary>Runs as the reveal opens and each frame while it is open.</summary>
    protected virtual void Tick()
    {
    }

    /// <param name="offerDrive">Over a mission the free test drive leads; everywhere else the purchase stands alone.</param>
    protected void Build(RectTransform root, Font font, string subtitle, bool offerDrive)
    {
        this.font = font;
        var store = TreadShredStore.Ensure();
        var unitPrice = store.PriceFor(GemCatalog.Packs[0]);
        var price = store.PriceFor(Stack.Pack);
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.9f);
        var panel = TreadShredUi.CreateRect("StackOfferPanel", backdrop, Vector2.zero, new Vector2(1520f, 980f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);
        TreadShredUi.CreateText(Stack.Title, panel, new Vector2(0f, 420f), new Vector2(1400f, 100f), 84, font).color =
            TreadShredUi.TitleColor;
        SubtitleText = TreadShredUi.CreateText(subtitle, panel, new Vector2(0f, 352f),
            new Vector2(1400f, 40f), 30, font);
        SubtitleText.color = TreadShredUi.AccentColor;

        // Down the left, then down the right, in the order they land.
        var perColumn = (Stack.Items.Count + 1) / 2;
        for (var index = 0; index < Stack.Items.Count; index++)
        {
            var item = Stack.Items[index];
            var position = new Vector2(index < perColumn ? -370f : 370f, 260f - 85f * (index % perColumn));
            var row = TreadShredUi.CreateRect("StackItem" + index, panel, position, new Vector2(700f, 74f),
                typeof(RectTransform), typeof(Image));
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            var line = TreadShredUi.CreateText(SupplyStack.Line(item), row, new Vector2(-80f, 0f), new Vector2(520f, 70f), 36, font);
            line.alignment = TextAnchor.MiddleLeft;
            line.color = Color.white;
            TreadShredUi.ShrinkToFit(line, 24);
            var value = TreadShredUi.CreateText(SupplyStack.Value(item, unitPrice), row, new Vector2(270f, 0f), new Vector2(140f, 70f), 32,
                font);
            value.alignment = TextAnchor.MiddleRight;
            value.color = TreadShredUi.TitleColor;
            Hide(row.gameObject);
        }

        if (Stack.WeaponBits != 0)
        {
            var weapons = TreadShredUi.CreateText(Stack.WeaponsNote, panel, new Vector2(0f, -70f), new Vector2(1400f, 34f), 26, font);
            weapons.color = TreadShredUi.DetailColor;
            Hide(weapons.gameObject);
        }
        var total = TreadShredUi.CreateText(Stack.TotalValue(unitPrice), panel, new Vector2(0f, -132f), new Vector2(1400f, 56f), 44,
            font);
        total.color = Color.white;
        Hide(total.gameObject);
        var yours = TreadShredUi.CreateText(SupplyStack.YoursFor(price), panel, new Vector2(0f, -198f), new Vector2(1400f, 68f), 56, font);
        yours.color = TreadShredUi.AccentColor;
        Hide(yours.gameObject);
        if (!offerDrive)
        {
            buyButton = TreadShredUi.CreateButton(Stack.BuyButton(price), panel, new Vector2(0f, -300f), new Vector2(780f, 110f), 40,
                font, Buy);
            TreadShredUi.StylePrimary(buyButton);
            TreadShredUi.ShrinkToFit(buyButton.GetComponentInChildren<Text>(), 28);
            Hide(buyButton.gameObject);
        }
        else
        {
            // Experience first: the free drive leads, the purchase stands beside it.
            var drive = TreadShredUi.CreateButton(WarChest.TestDriveButton, panel, new Vector2(-345f, -300f), new Vector2(660f, 110f),
                40, font, StartDrive);
            TreadShredUi.StylePrimary(drive);
            Hide(drive.gameObject);
            buyButton = TreadShredUi.CreateButton(Stack.BuyButton(price), panel, new Vector2(345f, -300f), new Vector2(660f, 110f), 34,
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
        Tick();
        Shown(store);
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
            TreadShredHaptics.Pulse(index < Stack.Items.Count ? HapticPulse.Hit : HapticPulse.Kill, vibration);
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

    protected void Update()
    {
        // Its sale ended (the Starter Pack once bought or its window over). Buying closes it through BuyPack's callback,
        // which is how the War Chest closes: it stays on sale (D-047-AC-02).
        if (!OnSale)
        {
            Close();
            return;
        }
        buyButton.interactable = !TreadShredStore.Ensure().PurchaseInProgress;
        Tick();
    }

    private void StartDrive()
    {
        var driveFont = font;
        Destroy(gameObject);
        TestDrive(driveFont);
    }

    private void Buy()
    {
        statusText.text = string.Empty;
        TreadShredStore.Ensure().BuyPack(Stack.Pack, Close, showStatus);
    }

    // D-047-AC-04, D-037-AC-04: Back closes it only once everything has landed, like NOT NOW.
    protected void Start() => TreadShredBack.Opened(this, () =>
    {
        if (Revealed)
            Close();
    });

    protected void OnDestroy()
    {
        TreadShredBack.Closed(this);
        // Closed while the sheet was opening: a delivery still equips the pack (D-011).
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

/// <summary>
/// D-047-AC-04: the War Chest interstitial, opened over the failure screen once a session while the chest is on sale.
/// D-047-AC-05: after a loss it also offers the free test drive, and when a drive ends it opens again to sell what the
/// player just used.
/// </summary>
public sealed class TreadShredWarChestOffer : TreadShredStackOffer
{
    /// <summary>D-047-AC-04: the chest has been offered this session; it is offered at most once per launch.</summary>
    public static bool ShownThisSession { get; set; }

    /// <summary>Opens the interstitial after a lost mission when the War Chest is on sale and it has not been offered this session.</summary>
    public static bool ShowAfterLoss(Font font)
    {
        if (!WarChest.OpensAfterLoss(TreadShredAskFreeStart.MissionAskFree, ShownThisSession, TreadShredStore.Ensure().WarChestOnSale,
                FindFirstObjectByType<TreadShredWarChestOffer>() != null))
            return false;
        ShownThisSession = true;
        var root = TreadShredUi.CreateOverlay("TreadShredWarChestOffer", 110);
        root.gameObject.AddComponent<TreadShredWarChestOffer>().Build(root, font, WarChest.Subtitle, true);
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
        offer.Build(root, font, WarChest.AfterDriveSubtitle, false);
    }

    /// <summary>D-047-AC-06, D-047-AC-08: the reveal from the upgrades or result screen's button: the purchase alone, then
    /// back to that screen.</summary>
    public static void ShowToBuy(Font font)
    {
        if (!TreadShredStore.Ensure().WarChestOnSale || FindFirstObjectByType<TreadShredWarChestOffer>() != null)
            return;
        var root = TreadShredUi.CreateOverlay("TreadShredWarChestOffer", 110);
        root.gameObject.AddComponent<TreadShredWarChestOffer>().Build(root, font, WarChest.Subtitle, false);
    }

    /// <summary>D-047-AC-08: once the player has seen the War Chest, every result screen keeps WAR CHEST: price one tap
    /// away, under LEADERBOARD (the free War Chest and the Starter Pack callout share the slot under UPGRADES).</summary>
    public static void ResultScreen(RectTransform screen, Font font)
    {
        var store = TreadShredStore.Ensure();
        if (!WarChest.ShowsOnResultScreen(TreadShredAskFreeStart.MissionAskFree, store.WarChestOnSale, store.Wallet.WarChestSeen))
            return;
        var button = TreadShredUi.CreateButton(WarChest.StoreButton(store.PriceFor(GemCatalog.WarChest)), screen,
            new Vector2(530f, -92f), new Vector2(280f, 64f), 22, font, () => ShowToBuy(font));
        button.name = ResultButtonName;
        TreadShredUi.StylePrimary(button);
        TreadShredUi.ShrinkToFit(button.GetComponentInChildren<Text>(), 16);
    }

    public const string ResultButtonName = "TreadShredWarChestResultButton";

    protected override SupplyStack Stack => WarChest.Stack;

    protected override bool OnSale => TreadShredStore.Ensure().WarChestOnSale;

    protected override void Shown(TreadShredStore store)
    {
        store.Wallet.MarkWarChestSeen();
        TreadShredAnalytics.Log(GameEvents.OfferShown("war_chest", store.Wallet.Balance));
    }

    protected override void TestDrive(Font driveFont) => TreadShredTestDrive.Begin(driveFont);
}
