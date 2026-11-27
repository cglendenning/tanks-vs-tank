using TreadShred.Analytics;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-037-AC-04: the Starter Pack interstitial: its stack revealed item by item as the War Chest's is, under ONE TIME
/// ONLY and the live countdown (D-037-AC-07). It opens by itself once, over the first victory or failure screen or start
/// screen of the pack's window; after that the result screen's callout (TreadShredStarterCallout) opens it. Closes
/// itself once the pack is bought or its window ends. D-037-AC-08: over a result screen it leads with the free test
/// drive of the mission just played; when the drive ends it opens again, and closing it goes on to the next mission
/// after a victory or replays the mission after a loss. A player gets two drives in all.
/// </summary>
public sealed class TreadShredStarterOffer : TreadShredStackOffer
{
    // ONE TIME ONLY, or after a drive what the player just used; the countdown follows it.
    private string limit;
    private bool won;

    /// <summary>The start screen's interstitial, the purchase alone, unless it has opened by itself before. Returns whether it opened.</summary>
    public static bool ShowIfOnSale(Font font) => Show(font, false, false, byItself: true);

    /// <summary>A result screen's interstitial, led by the free test drive of the mission just played, unless it has opened by itself before.</summary>
    public static bool ShowOverResult(Font font, bool won) => Show(font, true, won, byItself: true);

    /// <summary>D-037-AC-04: the result screen's callout, tapped: the interstitial with the free test drive, every time.</summary>
    public static bool OpenFromCallout(Font font, bool won) => Show(font, true, won, byItself: false);

    private static bool Show(Font font, bool overMission, bool won, bool byItself)
    {
        var store = TreadShredStore.Ensure();
        var alreadyOpen = FindFirstObjectByType<TreadShredStarterOffer>() != null;
        if (byItself ? !StarterPackPitch.OpensByItself(store.Wallet.StarterPackShown, store.StarterPackOnSale, alreadyOpen)
                     : !StarterPackPitch.Opens(store.StarterPackOnSale, alreadyOpen))
            return false;
        store.Wallet.MarkStarterPackShown();
        var root = TreadShredUi.CreateOverlay("TreadShredStarterOffer", 110);
        var offer = root.gameObject.AddComponent<TreadShredStarterOffer>();
        offer.limit = StarterPackPitch.Limit;
        offer.won = won;
        offer.Build(root, font, offer.limit, StarterPackPitch.OffersTestDrive(overMission, store.Wallet.StarterPackTestDrives));
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

    protected override void TestDrive(Font driveFont)
    {
        // D-037-AC-08: counted as it starts; after the second the purchase stands alone.
        TreadShredStore.Ensure().Wallet.CountStarterPackTestDrive();
        TreadShredTestDrive.Begin(driveFont, StarterPackPitch.Stack, ShowAfterTestDrive, won ? TreadShredCampaign.Next : (System.Action)null);
    }

    // D-037-AC-07: the countdown runs live beside the line.
    protected override void Tick() =>
        SubtitleText.text = limit + "  |  " + OfferLabels.StarterPackCountdown(TreadShredStore.Ensure().StarterPackTimeLeft);
}
