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
        coinsText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(-260f, 330f), new Vector2(560f, 40f), 30, font);
        gemsText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(260f, 330f), new Vector2(560f, 40f), 30, font);
        gemsText.color = coinsText.color = TreadShredUi.DetailColor;

        for (var i = 0; i < Tracks.Length; i++)
        {
            var track = Tracks[i];
            var name = TreadShredUi.CreateText(ProgressionLabels.Track(track), backdrop, new Vector2(-480f, RowY[i] + 20f),
                new Vector2(360f, 60f), 42, font);
            name.alignment = TextAnchor.MiddleLeft;
            levelTexts[track] = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(-480f, RowY[i] - 34f),
                new Vector2(360f, 36f), 24, font);
            levelTexts[track].alignment = TextAnchor.MiddleLeft;
            levelTexts[track].color = TreadShredUi.DetailColor;
            effectTexts[track] = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(-70f, RowY[i]),
                new Vector2(420f, 60f), 26, font);
            // Columns stay within +/-700 reference units so a 4:3 iPad shows all of them.
            buttons[track] = TreadShredUi.CreateButton(string.Empty, backdrop, new Vector2(420f, RowY[i]),
                new Vector2(540f, 100f), 26, font, () => Upgrade(track));
        }

        statusText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(0f, -240f), new Vector2(1200f, 36f), 26, font);
        statusText.color = TreadShredUi.DetailColor;
        TreadShredUi.CreateButton("BACK", backdrop, new Vector2(0f, -390f), new Vector2(440f, 120f), 52, font,
            () => Destroy(gameObject));

        Progression.Changed += Refresh;
        TreadShredStore.Ensure().Wallet.BalanceChanged += OnBalanceChanged;
        Refresh();
    }

    private void Upgrade(UpgradeTrack track)
    {
        statusText.text = string.Empty;
        var quote = Progression.Quote(track);
        var result = Progression.TryUpgrade(track);
        if (result == UpgradeResult.Upgraded)
        {
            TreadShredAnalytics.Log(GameEvents.UpgradePurchased(track.ToString().ToLowerInvariant(), Progression.Level(track),
                quote.CostCoins - quote.CoinShortfall, quote.AffordableWithCoins ? 0 : quote.GemsNeeded));
            statusText.text = ProgressionLabels.Track(track) + " UPGRADED TO LEVEL " + Progression.Level(track);
            return;
        }
        if (result == UpgradeResult.AlreadyMaxed)
            return;

        // Not enough gems for the coin shortfall: buy the smallest covering
        // pack, then upgrade, in the same tap (D-010, D-018).
        var store = TreadShredStore.Ensure();
        var gemsShort = Progression.Quote(track).GemsNeeded - store.Wallet.Balance;
        store.BuyPack(GemCatalog.SmallestPackCovering(gemsShort), () => Upgrade(track), status => statusText.text = status);
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
            buttons[track].interactable = !maxed;
        }
    }

    private void OnBalanceChanged(int balance)
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (TreadShredProgress.Instance != null)
            TreadShredProgress.Instance.Progression.Changed -= Refresh;
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Wallet.BalanceChanged -= OnBalanceChanged;
    }
}
