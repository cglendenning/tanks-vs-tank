using TreadShred.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// D-034: the full sales screen behind every clip. It shows the item working, the outcome it buys and the
/// offer's own buy button; buying closes it and continues on the screen it came from.
/// </summary>
public sealed class TreadShredSalesScreen : MonoBehaviour
{
    // Above the result screen, the upgrade screen (100) and the revive offer (120).
    private const int SortingOrder = 130;

    /// <param name="detail">The line above the button: the next level, or the gem balance.</param>
    /// <param name="buy">The offer's own buy action, run after this screen closes.</param>
    public static TreadShredSalesScreen Show(SaleItem item, Font font, string buttonLabel, string detail, bool canBuy,
        UnityAction buy)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredSales", SortingOrder);
        var screen = root.gameObject.AddComponent<TreadShredSalesScreen>();
        screen.Build(root, item, font, buttonLabel, detail, canBuy, buy);
        return screen;
    }

    private void Build(RectTransform root, SaleItem item, Font font, string buttonLabel, string detail, bool canBuy,
        UnityAction buy)
    {
        var backdrop = TreadShredUi.CreateBackdrop(root, 0.96f);
        var pitch = SalesPitches.For(item);

        TreadShredUi.CreateText(pitch.Title, backdrop, new Vector2(0f, 420f), new Vector2(1200f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;
        // Columns stay within +/-700 reference units so a 4:3 iPad shows all of them.
        TreadShredSalesClip.Create(backdrop, new Vector2(-300f, 60f), new Vector2(720f, 405f), item, font, null);

        const float column = 390f;
        var outcome = TreadShredUi.CreateText(pitch.Outcome, backdrop, new Vector2(column, 210f), new Vector2(560f, 100f), 40, font);
        outcome.color = Color.white;
        TreadShredUi.ShrinkToFit(outcome, 28);
        for (var i = 0; i < pitch.Points.Count; i++)
        {
            var point = TreadShredUi.CreateText(pitch.Points[i], backdrop, new Vector2(column, 120f - i * 64f),
                new Vector2(560f, 56f), 26, font);
            point.color = TreadShredUi.DetailColor;
            TreadShredUi.ShrinkToFit(point, 18);
        }
        var detailText = TreadShredUi.CreateText(detail, backdrop, new Vector2(column, -60f), new Vector2(560f, 40f), 24, font);
        detailText.color = TreadShredUi.TitleColor;
        TreadShredUi.ShrinkToFit(detailText, 16);

        var buyButton = TreadShredUi.CreateButton(buttonLabel, backdrop, new Vector2(column, -160f), new Vector2(560f, 110f), 30,
            font, () =>
            {
                Destroy(gameObject);
                buy();
            });
        TreadShredUi.ShrinkToFit(buyButton.GetComponentInChildren<Text>(), 16);
        buyButton.interactable = canBuy;
        TreadShredUi.CreateButton("BACK", backdrop, new Vector2(0f, -390f), new Vector2(440f, 120f), 52, font, Close);
    }

    private void Close() => Destroy(gameObject);

    private void Start() => TreadShredBack.Opened(this, Close);

    private void OnDestroy() => TreadShredBack.Closed(this);
}
