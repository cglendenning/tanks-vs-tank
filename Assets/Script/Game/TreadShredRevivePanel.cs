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
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);

        var panel = TreadShredUi.CreateRect("RevivePanel", backdrop, Vector2.zero, new Vector2(900f, 460f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);

        TreadShredUi.CreateText("TANK DESTROYED", panel, new Vector2(0f, 170f), new Vector2(860f, 70f), 56, font)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText("REVIVE NOW WITH FULL ARMOR", panel, new Vector2(0f, 105f), new Vector2(860f, 40f), 30, font)
            .color = Color.white;
        balanceText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, 60f), new Vector2(860f, 34f), 24, font);
        balanceText.color = TreadShredUi.DetailColor;
        reviveButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -30f), new Vector2(620f, 100f), 34,
            font, Accept);
        TreadShredUi.CreateButton("NO THANKS", panel, new Vector2(0f, -150f), new Vector2(360f, 80f), 28, font, Decline);
        countdownText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(330f, 170f), new Vector2(160f, 70f), 56, font);
        countdownText.color = TreadShredUi.AccentColor;

        showStatus = Refresh;
        Refresh(null);
        TreadShredAnalytics.Log(GameEvents.OfferShown("revive", TreadShredStore.Ensure().Wallet.Balance));
    }

    private void Refresh(string status)
    {
        var store = TreadShredStore.Ensure();
        var plan = GemCatalog.PlanFor(offer.Cost, store.Wallet.Balance);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        TreadShredUi.SetLabel(reviveButton, OfferLabels.ActionButton("REVIVE", plan, price));
        balanceText.text = status ?? OfferLabels.BalanceLine(store.Wallet.Balance, plan);
        reviveButton.interactable = !store.PurchaseInProgress;
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
        var packBought = !GemCatalog.PlanFor(cost, TreadShredStore.Ensure().Wallet.Balance).CanSpendNow;
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
            TreadShredStore.Instance.Release(showStatus);
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
