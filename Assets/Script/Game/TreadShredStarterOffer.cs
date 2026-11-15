using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-037-AC-04: the Starter Pack interstitial, opened over each victory and failure screen and on reaching the start
/// screen while the pack is on sale: its stack revealed item by item as the War Chest's is, under ONE TIME ONLY and the
/// live countdown (D-037-AC-07). Closes itself once the pack is bought or its window ends. D-037-AC-08: over a result
/// screen it leads with the free test drive of the mission just played; when the drive ends it opens again, and closing
/// it goes on to the next mission after a victory or replays the mission after a loss.
/// </summary>
public sealed class TreadShredStarterOffer : TreadShredStackOffer
{
    // ONE TIME ONLY, or after a drive what the player just used; the countdown follows it.
    private string limit;
    private bool won;

    /// <summary>The start screen's interstitial: the purchase alone. Returns whether it opened.</summary>
    public static bool ShowIfOnSale(Font font) => Show(font, false, false);

    /// <summary>A result screen's interstitial, led by the free test drive of the mission just played.</summary>
    public static bool ShowOverResult(Font font, bool won) => Show(font, true, won);

    private static bool Show(Font font, bool overMission, bool won)
    {
        if (!TreadShredStore.Ensure().StarterPackOnSale || FindFirstObjectByType<TreadShredStarterOffer>() != null)
            return false;
        var root = TreadShredUi.CreateOverlay("TreadShredStarterOffer", 110);
        var offer = root.gameObject.AddComponent<TreadShredStarterOffer>();
        offer.limit = StarterPackPitch.Limit;
        offer.won = won;
        offer.Build(root, font, offer.limit, overMission);
        return true;
    }

    /// <summary>D-037-AC-08: the reveal once a drive ends; closing it (or buying) runs <paramref name="closed"/>.</summary>
    public static void ShowAfterTestDrive(Font font, System.Action closed)
    {
        if (!TreadShredStore.Ensure().StarterPackOnSale)
        {
            closed();
            return;
        }
        var root = TreadShredUi.CreateOverlay("TreadShredStarterOffer", 110);
        var offer = root.gameObject.AddComponent<TreadShredStarterOffer>();
        offer.onClosed = closed;
        offer.limit = StarterPackPitch.AfterDriveSubtitle;
        offer.Build(root, font, offer.limit, false);
    }

    protected override SupplyStack Stack => StarterPackPitch.Stack;

    protected override bool OnSale => TreadShredStore.Ensure().StarterPackOnSale;

    protected override void Shown(TreadShredStore store) =>
        TreadShredAnalytics.Log(GameEvents.OfferShown("starter_pack", store.Wallet.Balance));

    protected override void TestDrive(Font driveFont) =>
        TreadShredTestDrive.Begin(driveFont, StarterPackPitch.Stack, ShowAfterTestDrive, won ? TreadShredCampaign.Next : (System.Action)null);

    // D-037-AC-07: the countdown runs live beside the line.
    protected override void Tick() =>
        SubtitleText.text = limit + "  |  " + OfferLabels.StarterPackCountdown(TreadShredStore.Ensure().StarterPackTimeLeft);
}
