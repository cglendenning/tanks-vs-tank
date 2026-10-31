using System.Collections.Generic;
using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-017/D-018: full-screen upgrade shop for armor, firepower and speed.
/// </summary>
public sealed class TreadShredUpgradePanel : MonoBehaviour
{
    private static readonly UpgradeTrack[] Tracks = { UpgradeTrack.Armor, UpgradeTrack.Firepower, UpgradeTrack.Speed };
    private static readonly float[] RowY = { 200f, 40f, -120f };

    private Font font;
    private Text coinsText;
    private Text gemsText;
    private Text statusText;
    private readonly Dictionary<UpgradeTrack, Text> levelTexts = new Dictionary<UpgradeTrack, Text>();
    private readonly Dictionary<UpgradeTrack, Text> effectTexts = new Dictionary<UpgradeTrack, Text>();
    private readonly Dictionary<UpgradeTrack, Button> buttons = new Dictionary<UpgradeTrack, Button>();
    private UpgradePurchase shop;
    private Button undoButton;

    public static TreadShredUpgradePanel Show(Font font)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredUpgrades", 100);
        var panel = root.gameObject.AddComponent<TreadShredUpgradePanel>();
        panel.font = font;
        panel.Build(root);
        return panel;
    }

    private static PlayerProgression Progression => TreadShredProgress.Ensure().Progression;

    private void Build(RectTransform root)
    {
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        backdrop.SetParent(root, false);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.95f);

        TreadShredUi.CreateText("UPGRADES", backdrop, new Vector2(0f, 420f), new Vector2(900f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;
        // D-018: what coins and gems are, one tap away.
        var note = TreadShredUi.CreateButton(ProgressionLabels.CurrencyNoteTitle, backdrop, new Vector2(575f, 420f),
            new Vector2(230f, 70f), 24, font,
            () => TreadShredNotePanel.Show(ProgressionLabels.CurrencyNoteTitle, ProgressionLabels.CurrencyNote(), font));
        TreadShredUi.ShrinkToFit(note.GetComponentInChildren<Text>(), 16);
        coinsText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(-260f, 330f), new Vector2(560f, 40f), 30, font);
        gemsText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(260f, 330f), new Vector2(560f, 40f), 30, font);
        gemsText.color = coinsText.color = TreadShredUi.DetailColor;

        for (var i = 0; i < Tracks.Length; i++)
        {
            var track = Tracks[i];
            // D-034: the upgrade working, looping at the start of its row; tapping it opens the sales screen.
            TreadShredSalesClip.Create(backdrop, new Vector2(-595f, RowY[i]), new Vector2(190f, 107f),
                SalesPitches.ForTrack(track), font, () => OpenSalesScreen(track));
            var name = TreadShredUi.CreateText(ProgressionLabels.Track(track), backdrop, new Vector2(-335f, RowY[i] + 20f),
                new Vector2(300f, 60f), 42, font);
            name.alignment = TextAnchor.MiddleLeft;
            levelTexts[track] = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(-335f, RowY[i] - 34f),
                new Vector2(300f, 36f), 24, font);
            levelTexts[track].alignment = TextAnchor.MiddleLeft;
            levelTexts[track].color = TreadShredUi.DetailColor;
            effectTexts[track] = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(25f, RowY[i]),
                new Vector2(400f, 60f), 26, font);
            TreadShredUi.ShrinkToFit(effectTexts[track], 18);
            // Columns stay within +/-700 reference units so a 4:3 iPad shows all of them.
            buttons[track] = TreadShredUi.CreateButton(string.Empty, backdrop, new Vector2(465f, RowY[i]),
                new Vector2(450f, 100f), 26, font, () => Upgrade(track));
            TreadShredUi.ShrinkToFit(buttons[track].GetComponentInChildren<Text>(), 18);
        }

        statusText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(0f, -240f), new Vector2(1200f, 36f), 26, font);
        statusText.color = TreadShredUi.DetailColor;
        TreadShredUi.CreateButton("DONE", backdrop, new Vector2(0f, -390f), new Vector2(440f, 120f), 52, font,
            () => Destroy(gameObject));
        // D-017-AC-06: shown while an upgrade made on this screen can still be given back.
        undoButton = TreadShredUi.CreateButton(ProgressionLabels.Undo, backdrop, new Vector2(-470f, -390f),
            new Vector2(300f, 100f), 30, font, Undo);
        TreadShredUi.ShrinkToFit(undoButton.GetComponentInChildren<Text>(), 18);

        var store = TreadShredStore.Ensure();
        shop = new UpgradePurchase(Progression, store.Wallet, store.Purchases, OnUpgraded, ShowStatus);
        Progression.Changed += Refresh;
        store.Wallet.BalanceChanged += OnBalanceChanged;
        Refresh();
    }

    // D-017-AC-05: nothing is spent until the player confirms what the upgrade costs.
    private void Upgrade(UpgradeTrack track)
    {
        if (Progression.IsMaxed(track))
            return;
        var confirmation = ProgressionLabels.UpgradeConfirmation(track, Progression.Level(track) + 1,
            Progression.Quote(track), TreadShredStore.Ensure().Wallet.Balance);
        TreadShredConfirmDialog.Show(confirmation, font, () =>
        {
            statusText.text = string.Empty;
            shop.Buy(track);
        });
    }

    private void Undo()
    {
        var receipt = shop.LastUpgrade;
        if (receipt.HasValue && shop.Undo())
            statusText.text = ProgressionLabels.Undone(receipt.Value);
        Refresh();
    }

    // D-034: the sales screen's button is the row's own upgrade, with the same label and purchase.
    private void OpenSalesScreen(UpgradeTrack track)
    {
        var maxed = Progression.IsMaxed(track);
        TreadShredSalesScreen.Show(SalesPitches.ForTrack(track), font,
            ProgressionLabels.UpgradeButton(Progression.Quote(track), maxed),
            ProgressionLabels.Preview(track, Progression.Level(track)),
            !maxed && !TreadShredStore.Ensure().PurchaseInProgress, () => Upgrade(track));
    }

    private void OnUpgraded(UpgradeTrack track, UpgradeQuote quote)
    {
        statusText.text = ProgressionLabels.Track(track) + " UPGRADED TO LEVEL " + Progression.Level(track);
        // Progress redraws the screen before the upgrade is recorded for undo; redraw once it is.
        Refresh();
    }

    // Upgrades become final when the screen closes; only those are reported, so undone ones never count.
    private static void LogPurchase(UpgradeReceipt receipt)
    {
        var quote = receipt.Quote;
        TreadShredAnalytics.Log(GameEvents.UpgradePurchased(receipt.Track.ToString().ToLowerInvariant(), receipt.Level,
            quote.CostCoins - quote.CoinShortfall, receipt.GemsSpent));
    }

    private void ShowStatus(string status)
    {
        statusText.text = status;
        Refresh();
    }

    private void Refresh()
    {
        coinsText.text = ProgressionLabels.Coins(Progression.Coins);
        gemsText.text = OfferLabels.Owned(TreadShredStore.Ensure().Wallet.Balance);
        foreach (var track in Tracks)
        {
            var level = Progression.Level(track);
            var maxed = Progression.IsMaxed(track);
            levelTexts[track].text = "LEVEL " + level + " / " + ProgressionRules.LevelsPerTrack;
            effectTexts[track].text = ProgressionLabels.Preview(track, level);
            TreadShredUi.SetLabel(buttons[track], ProgressionLabels.UpgradeButton(Progression.Quote(track), maxed));
            // Locked while the store sheet is open, so a slow sheet never invites a second purchase.
            buttons[track].interactable = !maxed && !TreadShredStore.Ensure().PurchaseInProgress;
        }
        undoButton.gameObject.SetActive(shop != null && shop.CanUndo);
    }

    private void OnBalanceChanged(int balance)
    {
        Refresh();
    }

    private void Start() => TreadShredBack.Opened(this, () => Destroy(gameObject));

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        if (shop != null)
            foreach (var receipt in shop.Kept)
                LogPurchase(receipt);
        shop?.Release();
        if (TreadShredProgress.Instance != null)
            TreadShredProgress.Instance.Progression.Changed -= Refresh;
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Wallet.BalanceChanged -= OnBalanceChanged;
    }
}
