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
    private bool starterLayout;
    private Text starterCountdown;
    private Button starterButton;
    private GameObject[] starterParts;
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
        // D-037-AC-03: while the Starter Pack is on sale the panel leads with it; the plain revive offer sits below.
        starterLayout = store.StarterPackOnSale;
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.85f);

        var panel = TreadShredUi.CreateRect("RevivePanel", backdrop, Vector2.zero,
            starterLayout ? new Vector2(1000f, 760f) : new Vector2(900f, 460f), typeof(RectTransform), typeof(Image), typeof(Outline));
        TreadShredUi.StylePanel(panel.gameObject);
        if (starterLayout)
            BuildStarterLayout(panel, font);
        else
            BuildPlainLayout(panel, font);

        showStatus = Refresh;
        Refresh(null);
        store.Wallet.BalanceChanged += OnBalanceChanged;
        TreadShredAnalytics.Log(GameEvents.OfferShown("revive", store.Wallet.Balance));
        if (starterLayout)
            TreadShredAnalytics.Log(GameEvents.OfferShown("starter_pack", store.Wallet.Balance));
    }

    private void BuildPlainLayout(RectTransform panel, Font font)
    {
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
    }

    private void BuildStarterLayout(RectTransform panel, Font font)
    {
        TreadShredUi.CreateText("TANK DESTROYED", panel, new Vector2(0f, 320f), new Vector2(700f, 70f), 52, font)
            .color = TreadShredUi.TitleColor;
        countdownText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(430f, 320f), new Vector2(120f, 70f), 52, font);
        countdownText.color = TreadShredUi.AccentColor;
        var title = TreadShredUi.CreateText(OfferLabels.StarterPackTitle, panel, new Vector2(0f, 245f), new Vector2(940f, 76f), 64,
            font);
        title.color = TreadShredUi.AccentColor;
        var headline = TreadShredUi.CreateText(OfferLabels.StarterPackReviveHeadline, panel, new Vector2(0f, 185f),
            new Vector2(940f, 46f), 36, font);
        headline.color = Color.white;
        var value = TreadShredUi.CreateText(OfferLabels.StarterPackValue, panel, new Vector2(0f, 140f), new Vector2(940f, 34f), 26,
            font);
        value.color = TreadShredUi.DetailColor;
        starterCountdown = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, 100f), new Vector2(940f, 40f), 30, font);
        starterCountdown.color = TreadShredUi.AccentColor;
        starterButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, 15f), new Vector2(720f, 120f), 38, font,
            BuyStarter);
        TreadShredUi.StylePrimary(starterButton);
        starterParts = new[] { title.gameObject, headline.gameObject, value.gameObject, starterCountdown.gameObject, starterButton.gameObject };
        balanceText = TreadShredUi.CreateText(string.Empty, panel, new Vector2(0f, -75f), new Vector2(940f, 34f), 24, font);
        balanceText.color = TreadShredUi.DetailColor;
        reviveButton = TreadShredUi.CreateButton(string.Empty, panel, new Vector2(0f, -150f), new Vector2(520f, 84f), 30, font,
            Accept);
        TreadShredUi.CreateButton("NO THANKS", panel, new Vector2(0f, -265f), new Vector2(360f, 70f), 26, font, Decline);
    }

    private void Refresh(string status)
    {
        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(offer.Cost);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        var starterShown = starterLayout && store.StarterPackOnSale;
        TreadShredUi.SetLabel(reviveButton, OfferLabels.ActionButton("REVIVE", plan, price));
        balanceText.text = status ?? (starterShown ? OfferLabels.Owned(store.Wallet.Balance)
            : OfferLabels.BalanceLine(store.Wallet.Balance, plan));
        reviveButton.interactable = !store.PurchaseInProgress;
        if (!starterLayout)
            return;
        // D-037-AC-03/07: the Starter Pack leads while it is on sale; once it is bought or runs out the revive offer remains.
        foreach (var part in starterParts)
            part.SetActive(starterShown);
        // Short of gems, the revive button would only buy the same pack without the free revive, so it waits.
        reviveButton.gameObject.SetActive(!starterShown || plan.CanSpendNow);
        TreadShredUi.SetLabel(starterButton, OfferLabels.StarterPackReviveButton(store.PriceFor(GemCatalog.StarterPack)));
        starterButton.interactable = !store.PurchaseInProgress;
    }

    private void OnBalanceChanged(int balance) => Refresh(null);

    // D-037-AC-03: buying the Starter Pack here revives at once without spending any of its gems.
    private void BuyStarter()
    {
        if (offer.Outcome != ReviveOutcome.Pending)
            return;
        TreadShredStore.Ensure().BuyPack(GemCatalog.StarterPack, () =>
        {
            if (!offer.Accept())
                return;
            TreadShredAnalytics.Log(GameEvents.OfferAccepted("starter_pack", 0, true));
            revivesThisMission++;
            Finish();
            if (player != null)
                player.Revive();
        }, showStatus);
    }

    private void Update()
    {
        if (offer.Outcome != ReviveOutcome.Pending)
            return;

        var store = TreadShredStore.Ensure();
        var expired = offer.Tick(Time.unscaledDeltaTime, store.PurchaseInProgress);
        countdownText.text = offer.SecondsShown.ToString();
        if (starterLayout)
        {
            var left = store.StarterPackTimeLeft;
            if (left > System.TimeSpan.Zero)
                starterCountdown.text = OfferLabels.StarterPackCountdown(left);
            else if (starterParts[0].activeSelf)
                Refresh(null); // D-037-AC-07: it ran out while the panel was open.
        }
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
