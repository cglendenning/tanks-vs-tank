using TreadShred.Analytics;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// D-047-AC-05, D-037-AC-08: a one-time pack's free test drive (the War Chest, the Starter Pack). The mission just
/// played loads again with everything in the pack on (the progression's test-drive units: never saved, never used up),
/// a countdown runs, and when it ends, the tank is lost or the map is cleared, the gear goes and the pack's reveal
/// opens; closing it replays the mission without the gear, or goes on where the drive was told to.
/// A drive earns nothing: ManagerScore ends it before any result is counted.
/// </summary>
public sealed class TreadShredTestDrive : MonoBehaviour
{
    private static TreadShredTestDrive instance;

    private string scene;
    private Font font;
    private SupplyStack stack;
    private System.Action<Font, System.Action> openReveal;
    private System.Action next;
    internal float left;
    private RectTransform hud;
    private Text clock;

    public static bool Active => instance != null;

    /// <summary>Starts a War Chest drive of the mission now on screen; closing its reveal replays the mission.</summary>
    public static void Begin(Font font) => Begin(font, WarChest.Stack, TreadShredWarChestOffer.ShowAfterTestDrive, null);

    /// <summary>
    /// Starts a drive of the mission now on screen with <paramref name="stack"/> on. When it ends,
    /// <paramref name="reveal"/> opens the pack's reveal; closing that runs <paramref name="next"/>, or replays the
    /// mission when it is null.
    /// </summary>
    public static void Begin(Font font, SupplyStack stack, System.Action<Font, System.Action> reveal, System.Action next)
    {
        if (instance != null)
            return;
        var host = new GameObject("TreadShredTestDrive");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<TreadShredTestDrive>();
        instance.font = font;
        instance.stack = stack;
        instance.openReveal = reveal;
        instance.next = next;
        instance.scene = SceneManager.GetActiveScene().name;
        instance.left = WarChest.TestDriveSeconds;
        TreadShredProgress.Ensure().Progression.StartTestDrive(stack);
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        TreadShredAnalytics.Log(GameEvents.OfferAccepted(stack.Offer + "_test_drive", 0, false));
        Time.timeScale = 1f;
        SceneManager.LoadScene(instance.scene);
    }

    /// <summary>Ends the drive and opens the pack's reveal; nothing when no drive is running.</summary>
    public static void Finish()
    {
        if (instance != null)
            instance.End(true);
    }

    private void OnSceneLoaded(Scene loaded, LoadSceneMode mode)
    {
        // Left the drive another way (home, another mission): the gear goes with no reveal.
        if (loaded.name != scene || hud != null)
        {
            End(false);
            return;
        }
        hud = TreadShredUi.CreateOverlay("TreadShredTestDriveHud", 60, false);
        clock = TreadShredUi.CreateText(WarChest.TestDriveClock(left), hud, new Vector2(0f, 360f), new Vector2(620f, 52f), 40, font);
        clock.color = TreadShredUi.AccentColor;
        TreadShredUi.CreateText(stack.TestDriveNote, hud, new Vector2(0f, 318f), new Vector2(620f, 30f), 24, font).color =
            TreadShredUi.TitleColor;
    }

    // Game time, so the countdown waits while the mission is paused.
    private void Update()
    {
        if (hud == null)
            return;
        left -= Time.deltaTime;
        clock.text = WarChest.TestDriveClock(left);
        if (left <= 0f)
            End(true);
    }

    private void End(bool reveal)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TreadShredProgress.Ensure().Progression.EndTestDrive();
        if (instance == this)
            instance = null;
        if (hud != null)
            Destroy(hud.gameObject);
        if (reveal)
        {
            Time.timeScale = 0f;
            var mission = scene;
            var then = next;
            openReveal(font, () =>
            {
                Time.timeScale = 1f;
                if (then != null)
                    then();
                else
                    SceneManager.LoadScene(mission);
            });
        }
        Destroy(gameObject);
    }
}
