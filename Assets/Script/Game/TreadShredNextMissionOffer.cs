using System;
using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-040: tapping NEXT MISSION is the strongest "more" a player gives, so an offer opens before the mission loads:
/// Overdrive for the next mission when it is not running, otherwise the cheapest next upgrade. NO THANKS, PLAY (or a
/// finished purchase) goes on to the mission; Back returns to the victory screen. When the offer's own price is covered by
/// coins or gems the player holds, a gem pack is added as the main button (D-040-AC-05), so every offer sells something.
/// </summary>
public sealed class TreadShredNextMissionOffer : MonoBehaviour
{
    private Action proceed;
    private Font font;
    private UpgradeTrack? track;
    private UpgradePurchase shop;
    private Text titleText;
    private Text balanceText;
    private Text statusText;
    private Button buyButton;
    private Button skipButton;
    private Button undoButton;
    private Button packButton;
    private Text packLine;
    private GemPack pack;
    private Action<string> showStatus;

    // How far the top and bottom of the panel move apart to make room for the pack.
    private const float PackLift = 80f;
    private bool upgraded;
    private bool leaving;

    /// <summary>Opens the offer and returns true, or returns false when there is nothing left to offer.</summary>
    public static bool ShowBeforeNext(Font font, Action proceed)
    {
        if (FindFirstObjectByType<TreadShredNextMissionOffer>() != null)
            return true;
        UpgradeTrack? track = null;
        if (TreadShredOverdrive.Active)
        {
            track = TreadShredProgress.Ensure().Progression.CheapestNextUpgrade();
            if (track == null)
                return false;
        }

        var root = TreadShredUi.CreateOverlay("TreadShredNextMissionOffer", 115);
        var offer = root.gameObject.AddComponent<TreadShredNextMissionOffer>();
        offer.font = font;
        offer.proceed = proceed;
        offer.track = track;
        offer.Build(root);
        return true;
    }

    private static PlayerProgression Progression => TreadShredProgress.Ensure().Progression;

    private void Build(RectTransform root)
    {
        var store = TreadShredStore.Ensure();
        pack = GemCatalog.PackAlongside(CoveredByHeldCurrency(), store.StarterPackOnSale);
        var lift = pack != null ? PackLift : 0f;
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);
        var panel = TreadShredUi.CreateRect("NextMissionOfferPanel", backdrop, Vector2.zero, new Vector2(1000f, 640f + 2f * lift),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);

        var overdrive = track == null;
        var level = overdrive ? 0 : Progression.Level(track.Value);
        titleText = TreadShredUi.CreateText(overdrive ? OfferLabels.ArmOverdrive : "UPGRADE " + ProgressionLabels.Track(track.Value),
            panel, new Vector2(0f, 245f + lift), new Vector2(940f, 80f), 64, font);
        titleText.color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText(overdrive ? OfferLabels.ForNextMission : TreadShredUpgradePanel.PreviewLine(track.Value), panel,
            new Vector2(0f, 180f + lift), new Vector2(940f, 40f), 30, font).color = Color.white;
        var item = overdrive ? SalesPitches.ForOffer(GemOffer.Overdrive) : SalesPitches.ForTrack(track.Value);
        // D-034: the item working; tapping it opens its sales screen, whose button is this offer's own.
        TreadShredSalesClip.Create(panel, new Vector2(-240f, 45f + lift), new Vector2(380f, 214f), item, font,
            () => TreadShredSalesScreen.Show(item, font, buyButton.GetComponentInChildren<Text>().text, balanceText.text,
                !TreadShredStore.Ensure().PurchaseInProgress, Buy));
        // D-045: armor and a track at level 10 sell a unit of supply, which lasts at least 5 missions.
        var supply = !overdrive && Progression.BuysSupply(track.Value);
        TreadShredUi.CreateText(overdrive ? OfferLabels.OverdriveShots
            : supply ? ProgressionLabels.SupplyUnit(track.Value) + " SUPPLY" : "LEVEL " + (level + 1) + " OF " + ProgressionRules.LevelsPerTrack,
            panel, new Vector2(230f, 95f + lift), new Vector2(440f, 36f), 26, font).color = Color.white;
        TreadShredUi.CreateText(overdrive || supply ? OfferLabels.SupplyLasts : "YOURS FOR GOOD", panel, new Vector2(230f, 55f + lift),
            new Vector2(440f, 36f), 24, font).color = TreadShredUi.DetailColor;
        balanceText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(230f, 5f + lift), new Vector2(440f, 34f), 22, font);
        balanceText.color = TreadShredUi.DetailColor;

        buyButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -120f + lift), new Vector2(680f, 110f), 38, font, Buy);
        TreadShredUi.StylePrimary(buyButton);
        if (pack != null)
        {
            // D-040-AC-05: the pack is the main button here, as the Starter Pack is on the revive panel (D-037).
            packLine = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -115f), new Vector2(940f, 34f), 24, font);
            packLine.color = TreadShredUi.DetailColor;
            packButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -180f), new Vector2(680f, 96f), 34, font,
                BuyPack);
            TreadShredUi.StyleStandard(buyButton);
            TreadShredUi.StylePrimary(packButton);
        }
        statusText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -195f - lift), new Vector2(940f, 34f), 24, font);
        statusText.color = TreadShredUi.DetailColor;
        skipButton = TreadShredUi.CreateButton(OfferLabels.NoThanksPlay, panel, new Vector2(0f, -262f - lift), new Vector2(380f, 70f), 26,
            font, Continue);
        undoButton = TreadShredUi.CreateButton(ProgressionLabels.UndoShort, panel, new Vector2(330f, -262f - lift), new Vector2(200f, 70f),
            26, font, Undo);
        undoButton.gameObject.SetActive(false);

        showStatus = status => statusText.text = status;
        if (!overdrive)
            shop = new UpgradePurchase(Progression, store.Wallet, store.Purchases, OnUpgraded, showStatus);
        Refresh();
        TreadShredAnalytics.Log(GameEvents.OfferShown(overdrive ? "overdrive_next_mission" : "upgrade_next_mission",
            store.Wallet.Balance));
    }

    // D-040-AC-05: whether the offer's own button spends only coins or gems the player already holds.
    private bool CoveredByHeldCurrency()
    {
        var store = TreadShredStore.Ensure();
        return track == null
            ? store.PlanFor(GemOffer.Overdrive).CanSpendNow
            : store.PlanFor(Progression.Quote(track.Value).GemsNeeded).CanSpendNow;
    }

    private void Refresh()
    {
        var store = TreadShredStore.Ensure();
        if (packButton != null)
        {
            // The Starter Pack gives way to the smallest pack if its window closes while the offer is open.
            pack = GemCatalog.PackAlongside(true, store.StarterPackOnSale);
            var starter = GemCatalog.IsStarterPack(pack);
            TreadShredUi.SetLabel(packButton, starter
                ? OfferLabels.StarterPackStoreButton(store.PriceFor(pack))
                : OfferLabels.Pack(pack, store.PriceFor(pack)));
            packLine.text = starter
                ? OfferLabels.StarterPackValue + ". " + OfferLabels.StarterPackCountdown(store.StarterPackTimeLeft)
                : OfferLabels.StockUp;
        }
        if (upgraded)
            return;
        if (track == null)
        {
            TreadShredUi.SetLabel(buyButton, TreadShredOverdrive.ButtonLabel());
            balanceText.text = OfferLabels.Owned(store.Wallet.Balance);
        }
        else
        {
            TreadShredUi.SetLabel(buyButton, TreadShredUpgradePanel.PricedLabel(track.Value, false));
            balanceText.text = ProgressionLabels.Coins(Progression.Coins) + ", " + CountText.Format(store.Wallet.Balance) + " GEMS";
        }
    }

    private void Update()
    {
        buyButton.interactable = !TreadShredStore.Ensure().PurchaseInProgress;
        if (packButton != null)
            packButton.interactable = buyButton.interactable;
        Refresh();
    }

    private void Buy()
    {
        statusText.text = string.Empty;
        if (upgraded)
        {
            Continue();
            return;
        }
        if (track == null)
            TreadShredOverdrive.Activate(font, "overdrive_next_mission", Continue, showStatus);
        else
            TreadShredUpgradePanel.Checkout(shop, track.Value, font, () => statusText.text = string.Empty);
    }

    // D-040-AC-05: the pack beside a covered offer; the offer stays open so its gems can be spent on it.
    private void BuyPack()
    {
        statusText.text = string.Empty;
        var bought = pack;
        TreadShredStore.Ensure().BuyPack(bought, () =>
        {
            TreadShredAnalytics.Log(GameEvents.OfferAccepted("pack_next_mission", 0, true));
            if (this != null)
                statusText.text = OfferLabels.GemsAdded(bought);
        }, showStatus);
    }

    // D-036-AC-03: a bought upgrade stays reversible here: PLAY goes on, UNDO gives it back.
    private void OnUpgraded(UpgradeTrack bought, UpgradeQuote quote)
    {
        upgraded = true;
        titleText.text = ProgressionLabels.Track(bought) + " UPGRADED";
        statusText.text = ProgressionLabels.Track(bought) + " UPGRADED TO LEVEL " + Progression.Level(bought);
        TreadShredUi.SetLabel(buyButton, "PLAY");
        skipButton.gameObject.SetActive(false);
        undoButton.gameObject.SetActive(true);
    }

    private void Undo()
    {
        var receipt = shop?.LastUpgrade;
        if (!receipt.HasValue || !shop.Undo())
            return;
        upgraded = false;
        titleText.text = "UPGRADE " + ProgressionLabels.Track(track.Value);
        statusText.text = ProgressionLabels.Undone(receipt.Value);
        skipButton.gameObject.SetActive(true);
        undoButton.gameObject.SetActive(false);
        Refresh();
    }

    private void Continue()
    {
        if (leaving)
            return;
        leaving = true;
        Destroy(gameObject);
        proceed?.Invoke();
    }

    private void Start() => TreadShredBack.Opened(this, () => Destroy(gameObject));

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Release(showStatus);
        if (shop == null)
            return;
        foreach (var receipt in shop.Kept)
            TreadShredUpgradePanel.LogPurchase(receipt);
    }
}
