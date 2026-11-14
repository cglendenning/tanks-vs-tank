using System;
using TreadShred.Analytics;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// D-047-AC-07: the free War Chest. On result screens it arrives on a variable schedule (WarChestArrivals); taking it
/// puts every item in the chest on for the player's next mission (the progression's test-drive units: never saved,
/// never used up) while that mission counts as usual, and its result screen opens the reveal to sell what was just
/// used. Leaving the mission any other way takes the gear away.
/// </summary>
public static class TreadShredFreeWarChest
{
    private const string HudName = "TreadShredFreeWarChestHud";

    private static readonly WarChestArrivals arrivals = new WarChestArrivals((min, max) => UnityEngine.Random.Range(min, max + 1));
    private static Font hudFont;

    /// <summary>Off only for screen tests that must not meet a random arrival.</summary>
    public static bool ArrivalsEnabled { get; set; } = true;

    /// <summary>Taken, waiting for the next mission.</summary>
    public static bool Armed { get; private set; }

    /// <summary>The free mission is being played.</summary>
    public static bool Active { get; private set; }

    /// <summary>A result screen: on the schedule, and when nothing else is selling the chest there, the free War Chest arrives.</summary>
    public static void ResultScreen(RectTransform screen, Font font)
    {
        var store = TreadShredStore.Ensure();
        if (!ArrivalsEnabled || Armed || Active || !store.WarChestOnSale ||
            UnityEngine.Object.FindFirstObjectByType<TreadShredWarChestOffer>() != null || !arrivals.ScreenShown())
            return;
        Button button = null;
        button = TreadShredUi.CreateButton(WarChest.FreeButton, screen, new Vector2(-530f, -92f), new Vector2(280f, 64f), 22, font, () =>
        {
            Arm(font);
            TreadShredUi.SetLabel(button, WarChest.ArmedLabel);
            button.interactable = false;
        });
        TreadShredUi.StylePrimary(button);
        TreadShredUi.ShrinkToFit(button.GetComponentInChildren<Text>(), 16);
        TreadShredAnalytics.Log(GameEvents.OfferShown("war_chest_free_mission", store.Wallet.Balance));
    }

    public static void Arm(Font font)
    {
        if (Armed || Active)
            return;
        Armed = true;
        hudFont = font;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TreadShredAnalytics.Log(GameEvents.OfferAccepted("war_chest_free_mission", 0, false));
    }

    /// <summary>The free mission's result screen: the gear goes and the reveal opens. False when none was being played.</summary>
    public static bool MissionOver(Font font)
    {
        if (!Active)
            return false;
        End();
        TreadShredWarChestOffer.ShowAfterTestDrive(font, () => { });
        return true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Left the free mission before its result screen.
        if (Active)
        {
            End();
            return;
        }
        // Waits for a mission, and lets a test drive run first.
        if (!scene.name.StartsWith("Lv", StringComparison.Ordinal) || TreadShredTestDrive.Active)
            return;
        if (!TreadShredStore.Ensure().WarChestOnSale)
        {
            End();
            return;
        }
        Armed = false;
        Active = true;
        // Before the tank's Start reads its armor: scene load comes first.
        TreadShredProgress.Ensure().Progression.StartTestDrive();
        var hud = TreadShredUi.CreateOverlay(HudName, 60, false);
        TreadShredUi.CreateText(WarChest.FreeNote, hud, new Vector2(0f, 360f), new Vector2(760f, 52f), 36, hudFont).color =
            TreadShredUi.AccentColor;
    }

    private static void End()
    {
        Armed = false;
        Active = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TreadShredProgress.Ensure().Progression.EndTestDrive();
        var hud = GameObject.Find(HudName);
        if (hud != null)
            UnityEngine.Object.Destroy(hud);
    }
}
