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
    private float remaining;
    private bool finished;

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

        var root = new GameObject("TreadShredRevive", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        var panel = root.AddComponent<TreadShredRevivePanel>();
        panel.player = player;
        panel.Build((RectTransform)root.transform, TreadShredUi.FindFont(GameObject.Find("Canvas")));
        Time.timeScale = 0f;
        return true;
    }

    private int Cost => ReviveRules.CostGems(revivesThisMission);

    private void Build(RectTransform root, Font font)
    {
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        backdrop.SetParent(root, false);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.85f);

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

        remaining = ProgressionRules.ReviveOfferSeconds;
        Refresh(null);
        TreadShredAnalytics.Log(GameEvents.OfferShown("revive", TreadShredStore.Ensure().Wallet.Balance));
    }

    private void Refresh(string status)
    {
        var store = TreadShredStore.Ensure();
        var plan = GemCatalog.PlanFor(Cost, store.Wallet.Balance);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        TreadShredUi.SetLabel(reviveButton, OfferLabels.ActionButton("REVIVE", plan, price));
        balanceText.text = status ?? OfferLabels.BalanceLine(store.Wallet.Balance, plan);
    }

    private void Update()
    {
        if (finished)
            return;

        // The countdown waits while the store's purchase sheet is open.
        if (!TreadShredStore.Ensure().PurchaseInProgress)
            remaining -= Time.unscaledDeltaTime;
        countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        if (remaining <= 0f)
            Decline();
    }

    private void Accept()
    {
        if (finished)
            return;

        var cost = Cost;
        var packBought = !GemCatalog.PlanFor(cost, TreadShredStore.Ensure().Wallet.Balance).CanSpendNow;
        TreadShredStore.Ensure().SpendOrBuy(cost, () =>
        {
            TreadShredAnalytics.Log(GameEvents.OfferAccepted("revive", cost, packBought));
            revivesThisMission++;
            Finish();
            if (player != null)
                player.Revive();
        }, status => Refresh(status));
    }

    private void Decline()
    {
        if (finished)
            return;

        Finish();
        if (player != null)
            player.Die();
    }

    private void Finish()
    {
        finished = true;
        Time.timeScale = 1f;
        Destroy(gameObject);
    }
}
