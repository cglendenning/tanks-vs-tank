using TreadShred.Analytics;
using TreadShred.Economy;
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
    private Text priceText;
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
        priceText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, 120f), new Vector2(940f, 62f), 52, font);
        priceText.color = Color.white;
        TreadShredUi.CreateText(OfferLabels.StarterPackValue, panel, new Vector2(0f, 60f), new Vector2(940f, 36f), 28, font)
            .color = TreadShredUi.DetailColor;
        TreadShredUi.CreateText(OfferLabels.StarterPackBuys, panel, new Vector2(0f, 18f), new Vector2(940f, 34f), 26, font)
            .color = TreadShredUi.DetailColor;
        buyButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -90f), new Vector2(680f, 120f), 40, font, Buy);
        TreadShredUi.StylePrimary(buyButton);
        statusText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -180f), new Vector2(940f, 34f), 24, font);
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
        priceText.text = OfferLabels.Pack(GemCatalog.StarterPack, price);
        TreadShredUi.SetLabel(buyButton, OfferLabels.Pack(GemCatalog.StarterPack, price, "BUY "));
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
