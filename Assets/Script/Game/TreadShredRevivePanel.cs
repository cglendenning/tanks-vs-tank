using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// D-020: when the player's tank is destroyed, offer a gem revive before the
/// failure screen. The game is paused while the offer is up.
/// </summary>
public sealed class TreadShredRevivePanel : MonoBehaviour
{
    private static int revivesThisMission;
    private static string missionScene;

    private PlayerMoverment player;
    private Text countdownText;
    private Text balanceText;
    private Button reviveButton;
    private Text starterHeading;
    private Button starterButton;
    private ReviveOffer offer;
    private System.Action<string> showStatus;

    /// <summary>Shows the offer and returns true, or returns false when no revive is allowed.</summary>
    public static bool TryOffer(PlayerMoverment player)
    {
        var scene = SceneManager.GetActiveScene().path;
        if (scene != missionScene)
        {
            missionScene = scene;
            revivesThisMission = 0;
        }
        if (!ReviveRules.CanOffer(revivesThisMission))
            return false;

        var root = TreadShredUi.CreateOverlay("TreadShredRevive", 120);
        var panel = root.gameObject.AddComponent<TreadShredRevivePanel>();
        panel.player = player;
        panel.offer = new ReviveOffer(revivesThisMission);
        panel.Build(root, TreadShredUi.FindFont(GameObject.Find("Canvas")));
        Time.timeScale = 0f;
        return true;
    }

    private void Build(RectTransform root, Font font)
    {
        var store = TreadShredStore.Ensure();
        // D-037-AC-03: the first revive panel while the Starter Pack is on sale also offers it, once.
        var offerStarter = store.StarterPackOnSale && !store.Wallet.StarterPackOffered;
        var lift = offerStarter ? 110f : 0f;
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);

        var panel = TreadShredUi.CreateRect("RevivePanel", backdrop, Vector2.zero, new Vector2(900f, 460f + 2f * lift),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);

        TreadShredUi.CreateText("TANK DESTROYED", panel, new Vector2(0f, 170f + lift), new Vector2(860f, 70f), 56, font)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText("REVIVE NOW WITH FULL ARMOR", panel, new Vector2(0f, 105f + lift), new Vector2(860f, 40f), 30,
            font).color = Color.white;
        balanceText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, 60f + lift), new Vector2(860f, 34f), 24,
            font);
        balanceText.color = TreadShredUi.DetailColor;
        reviveButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -30f + lift), new Vector2(620f, 100f),
            34, font, Accept);
        if (offerStarter)
        {
            starterHeading = TreadShredUi.CreateText(OfferLabels.StarterPackTitle + "  |  " + OfferLabels.StarterPackLimit,
                panel, new Vector2(0f, -20f), new Vector2(860f, 40f), 30, font);
            starterHeading.color = TreadShredUi.TitleColor;
            starterButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -100f), new Vector2(620f, 90f), 32,
                font, BuyStarter);
        }
        TreadShredUi.CreateButton("NO THANKS", panel, new Vector2(0f, -150f - (offerStarter ? 90f : 0f)),
            new Vector2(360f, 80f), 28, font, Decline);
        countdownText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(330f, 170f + lift), new Vector2(160f, 70f), 56,
            font);
        countdownText.color = TreadShredUi.AccentColor;

        showStatus = Refresh;
        Refresh(null);
        store.Wallet.BalanceChanged += OnBalanceChanged;
        TreadShredAnalytics.Log(GameEvents.OfferShown("revive", store.Wallet.Balance));
        if (offerStarter)
        {
            store.Wallet.MarkStarterPackOffered();
            TreadShredAnalytics.Log(GameEvents.OfferShown("starter_pack", store.Wallet.Balance));
        }
    }

    private void Refresh(string status)
    {
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(offer.Cost);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        TreadShredUi.SetLabel(reviveButton, OfferLabels.ActionButton("REVIVE", plan, price));
        balanceText.text = status ?? OfferLabels.BalanceLine(store.Wallet.Balance, plan);
        reviveButton.interactable = !store.PurchaseInProgress;
        if (starterButton == null)
            return;
        // Bought (here or by the revive button): the row goes and the revive offer stays (D-037-AC-03).
        var onSale = store.StarterPackOnSale;
        starterHeading.gameObject.SetActive(onSale);
        starterButton.gameObject.SetActive(onSale);
        TreadShredUi.SetLabel(starterButton,
            OfferLabels.Pack(GemCatalog.StarterPack, store.PriceFor(GemCatalog.StarterPack), "BUY "));
        starterButton.interactable = !store.PurchaseInProgress;
    }

    private void OnBalanceChanged(int balance) => Refresh(null);

    private void BuyStarter()
    {
        if (offer.Outcome != ReviveOutcome.Pending)
            return;
        TreadShredStore.Ensure().BuyPack(GemCatalog.StarterPack,
            () => showStatus("+" + CountText.Format(GemCatalog.StarterPack.Gems) + " GEMS ADDED"), showStatus);
    }

    private void Update()
    {
        if (offer.Outcome != ReviveOutcome.Pending)
            return;

        var expired = offer.Tick(Time.unscaledDeltaTime, TreadShredStore.Ensure().PurchaseInProgress);
        countdownText.text = offer.SecondsShown.ToString();
        if (expired)
            Lose();
    }

    private void Accept()
    {
        if (offer.Outcome != ReviveOutcome.Pending)
            return;

        var cost = offer.Cost;
        var packBought = !TreadShredStore.Ensure().PlanFor(cost).CanSpendNow;
        TreadShredStore.Ensure().SpendOrBuy(cost, () =>
        {
            if (!offer.Accept())
                return;
            TreadShredAnalytics.Log(GameEvents.OfferAccepted("revive", cost, packBought));
            revivesThisMission++;
            Finish();
            if (player != null)
                player.Revive();
        }, showStatus);
    }

    private void Start() => TreadShredBack.Opened(this, Decline);

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        // Declined while the sheet was opening: a delivery credits the gems and spends nothing on a revive.
        if (TreadShredStore.Instance != null)
        {
            TreadShredStore.Instance.Wallet.BalanceChanged -= OnBalanceChanged;
            TreadShredStore.Instance.Release(showStatus);
        }
    }

    private void Decline()
    {
        if (offer.Decline())
            Lose();
    }

    private void Lose()
    {
        Finish();
        if (player != null)
            player.Die();
    }

    private void Finish()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
    }
}
