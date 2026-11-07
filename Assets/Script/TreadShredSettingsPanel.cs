using System.Collections.Generic;
using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen settings overlay for the start screen: one-word toggles, gem
/// packs, and a large back button.
/// </summary>
public sealed class TreadShredSettingsPanel : MonoBehaviour
{
    private const float ColumnOffset = 400f;
    private const float ColumnWidth = 560f;
    private static readonly float[] RowY = { 250f, 130f, 10f, -110f };

    private LoadLv menu;
    private Font font;
    private Button soundButton;
    private Button musicButton;
    private Button vibrationButton;
    private Button cameraButton;
    private Text balanceText;
    private Text statusText;
    private System.Action<string> showStatus;
    private readonly List<Button> packButtons = new List<Button>();

    public static TreadShredSettingsPanel Show(LoadLv menu, Font font)
    {
        var root = TreadShredUi.CreateOverlay("TreadShredSettings", 100);
        var panel = root.gameObject.AddComponent<TreadShredSettingsPanel>();
        panel.menu = menu;
        panel.font = font;
        panel.Build(root);
        return panel;
    }

    private void Build(RectTransform root)
    {
        // Full-screen backdrop that also blocks taps on the start screen.
        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        backdrop.SetParent(root, false);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.95f);

        TreadShredUi.CreateText("SETTINGS", backdrop, new Vector2(0f, 420f), new Vector2(900f, 90f), 72, font)
            .color = TreadShredUi.TitleColor;

        soundButton = CreateToggleRow("SOUND", RowY[0], backdrop, () => menu.SetSoundEnabled(!menu.SoundEnabled));
        musicButton = CreateToggleRow("MUSIC", RowY[1], backdrop, () => menu.SetMusicEnabled(!menu.MusicEnabled));
        vibrationButton = CreateToggleRow("VIBRATION", RowY[2], backdrop,
            () => menu.SetVibrationEnabled(!menu.VibrationEnabled));
        cameraButton = CreateToggleRow("CAMERA", RowY[3], backdrop, () => menu.SetCameraFollows(!menu.CameraFollows));

        TreadShredUi.CreateText("GEMS", backdrop, new Vector2(ColumnOffset, RowY[0] + 30f),
            new Vector2(ColumnWidth, 56f), 48, font).color = TreadShredUi.TitleColor;
        balanceText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(ColumnOffset, RowY[0] - 30f),
            new Vector2(ColumnWidth, 36f), 26, font);
        balanceText.color = TreadShredUi.DetailColor;

        for (var i = 0; i < GemCatalog.Packs.Count; i++)
        {
            var slot = i;
            packButtons.Add(TreadShredUi.CreateButton(string.Empty, backdrop, new Vector2(ColumnOffset, RowY[i + 1]),
                new Vector2(ColumnWidth, 96f), 32, font, () => BuyPack(SlotPack(slot))));
        }

        statusText = TreadShredUi.CreateText(string.Empty, backdrop, new Vector2(ColumnOffset, -182f),
            new Vector2(ColumnWidth + 200f, 36f), 24, font);
        statusText.color = TreadShredUi.DetailColor;

        // D-033-AC-05: the one in-game way to a review, straight to the store's page.
        TreadShredUi.CreateButton(TreadShred.Progression.ReviewRules.RateButton, backdrop, new Vector2(-ColumnOffset, -250f),
            new Vector2(ColumnWidth, 96f), 32, font, TreadShredReviewPrompt.OpenWriteReviewPage);

        // D-044-AC-01: erasing the whole game, behind a typed confirmation.
        TreadShredUi.CreateButton(TreadShred.Save.GameReset.Title, backdrop, new Vector2(ColumnOffset, -250f),
            new Vector2(ColumnWidth, 96f), 32, font, () => TreadShredResetPanel.Show(font));

        TreadShredUi.CreateButton("BACK", backdrop, new Vector2(0f, -390f), new Vector2(440f, 120f), 52, font, Close);

        var store = TreadShredStore.Ensure();
        store.Wallet.BalanceChanged += OnBalanceChanged;
        Refresh();
        // D-037-AC-05: the gem store offering the Starter Pack counts as an offer, like the revive panel's.
        if (store.StarterPackOnSale)
            TreadShredAnalytics.Log(GameEvents.OfferShown("starter_pack", store.Wallet.Balance));
    }

    private Button CreateToggleRow(string label, float y, RectTransform parent, System.Action toggle)
    {
        var text = TreadShredUi.CreateText(label, parent, new Vector2(-ColumnOffset - 90f, y), new Vector2(360f, 96f),
            44, font);
        text.alignment = TextAnchor.MiddleLeft;
        return TreadShredUi.CreateButton(string.Empty, parent, new Vector2(-ColumnOffset + 170f, y),
            new Vector2(220f, 96f), 36, font, () =>
            {
                toggle();
                Refresh();
            });
    }

    // D-037-AC-04: while the Starter Pack is on sale it takes the smallest pack's slot (the same price for a third of
    // the gems); the smallest pack returns once it is bought.
    private static GemPack SlotPack(int slot)
    {
        return slot == 0 && TreadShredStore.Ensure().StarterPackOnSale ? GemCatalog.StarterPack : GemCatalog.Packs[slot];
    }

    private void BuyPack(GemPack pack)
    {
        statusText.text = string.Empty;
        showStatus ??= status => statusText.text = status;
        TreadShredStore.Ensure().BuyPack(pack,
            () => statusText.text = OfferLabels.GemsAdded(pack),
            showStatus);
    }

    private void Refresh()
    {
        TreadShredUi.SetLabel(soundButton, menu.SoundEnabled ? "ON" : "OFF");
        TreadShredUi.SetLabel(musicButton, menu.MusicEnabled ? "ON" : "OFF");
        TreadShredUi.SetLabel(vibrationButton,
            HapticsRules.SettingLabel(menu.VibrationEnabled, TreadShredHaptics.DeviceCanVibrate));
        vibrationButton.interactable = TreadShredHaptics.DeviceCanVibrate;
        TreadShredUi.SetLabel(cameraButton, menu.CameraFollows ? "FOLLOW" : "FIXED");

        var store = TreadShredStore.Ensure();
        balanceText.text = OfferLabels.Owned(store.Wallet.Balance);
        for (var slot = 0; slot < packButtons.Count; slot++)
        {
            var pack = SlotPack(slot);
            var price = store.PriceFor(pack);
            TreadShredUi.SetLabel(packButtons[slot], GemCatalog.IsStarterPack(pack)
                ? StarterPackPitch.StoreButton(TreadShredProgress.Ensure().ArmorHits, price)
                : OfferLabels.Pack(pack, price, "BUY "));
        }
    }

    private void OnBalanceChanged(int balance)
    {
        Refresh();
    }

    private void Start() => TreadShredBack.Opened(this, Close);

    private void Close()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        TreadShredBack.Closed(this);
        if (TreadShredStore.Instance != null)
        {
            TreadShredStore.Instance.Wallet.BalanceChanged -= OnBalanceChanged;
            // Closed while the sheet was opening: a delivery still credits the gems.
            TreadShredStore.Instance.Release(showStatus);
        }
    }
}
