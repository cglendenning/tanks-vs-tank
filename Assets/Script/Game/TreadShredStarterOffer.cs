using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-037-AC-04: the Starter Pack interstitial, opened over each victory and failure screen and on reaching the start
/// screen while the pack is on sale, with its live countdown (D-037-AC-07). Closes itself once the pack is bought or
/// its window ends.
/// </summary>
public sealed class TreadShredStarterOffer : MonoBehaviour
{
    private Text countdownText;
    private Text valueText;
    private Text statusText;
    private Button buyButton;
    private System.Action<string> showStatus;

    /// <summary>Opens the interstitial when the Starter Pack is on sale and none is open; returns whether it opened.</summary>
    public static bool ShowIfOnSale(Font font)
    {
        if (!TreadShredStore.Ensure().StarterPackOnSale || FindFirstObjectByType<TreadShredStarterOffer>() != null)
            return false;
        var root = TreadShredUi.CreateOverlay("TreadShredStarterOffer", 110);
        root.gameObject.AddComponent<TreadShredStarterOffer>().Build(root, font);
        return true;
    }

    private void Build(RectTransform root, Font font)
    {
        var store = TreadShredStore.Ensure();
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);
        var panel = TreadShredUi.CreateRect("StarterOfferPanel", backdrop, Vector2.zero, new Vector2(1000f, 700f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);

        TreadShredUi.CreateText(OfferLabels.StarterPackTitle, panel, new Vector2(0f, 255f), new Vector2(940f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;
        countdownText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, 190f), new Vector2(940f, 42f), 32, font);
        countdownText.color = TreadShredUi.AccentColor;
        // D-037-AC-04: what the pack does in the game, not how many gems it is.
        var progress = TreadShredProgress.Ensure();
        TreadShredUi.CreateText(StarterPackPitch.Headline(progress.ArmorHits), panel, new Vector2(0f, 122f), new Vector2(940f, 64f), 60,
            font).color = Color.white;
        TreadShredUi.CreateText(StarterPackPitch.Alternatives, panel, new Vector2(0f, 62f), new Vector2(940f, 32f), 28, font)
            .color = TreadShredUi.DetailColor;
        TreadShredUi.CreateText(StarterPackPitch.Firepower(progress.Progression.Level(UpgradeTrack.Firepower)), panel,
            new Vector2(0f, 26f), new Vector2(940f, 32f), 28, font).color = TreadShredUi.DetailColor;
        valueText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -14f), new Vector2(940f, 32f), 28, font);
        valueText.color = TreadShredUi.TitleColor;
        buyButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -100f), new Vector2(680f, 110f), 40, font, Buy);
        TreadShredUi.StylePrimary(buyButton);
        TreadShredUi.CreateText(StarterPackPitch.GemsNote, panel, new Vector2(0f, -172f), new Vector2(940f, 26f), 20, font)
            .color = TreadShredUi.DetailColor;
        statusText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -207f), new Vector2(940f, 30f), 24, font);
        statusText.color = TreadShredUi.DetailColor;
        TreadShredUi.CreateButton("NOT NOW", panel, new Vector2(0f, -262f), new Vector2(360f, 76f), 28, font, Close);

        showStatus = status => statusText.text = status;
        Refresh();
        TreadShredAnalytics.Log(GameEvents.OfferShown("starter_pack", store.Wallet.Balance));
    }

    private void Refresh()
    {
        var store = TreadShredStore.Ensure();
        var price = store.PriceFor(GemCatalog.StarterPack);
        countdownText.text = OfferLabels.StarterPackLimit + "  |  " + OfferLabels.StarterPackCountdown(store.StarterPackTimeLeft);
        valueText.text = StarterPackPitch.ValueLine(store.PriceFor(GemCatalog.Packs[0]), price);
        TreadShredUi.SetLabel(buyButton, StarterPackPitch.BuyButton(price));
        buyButton.interactable = !store.PurchaseInProgress;
    }

    private void Update()
    {
        // D-037-AC-07: bought, or its window ended while the interstitial was open.
        if (!TreadShredStore.Ensure().StarterPackOnSale)
        {
            Close();
            return;
        }
        Refresh();
    }

    private void Buy()
    {
        statusText.text = string.Empty;
        TreadShredStore.Ensure().BuyPack(GemCatalog.StarterPack, Close, showStatus);
    }

    private void Start() => TreadShredBack.Opened(this, Close);

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        // Closed while the sheet was opening: a delivery still credits the gems (D-011).
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Release(showStatus);
    }

    private void Close()
    {
        if (this != null)
            Destroy(gameObject);
    }
}
