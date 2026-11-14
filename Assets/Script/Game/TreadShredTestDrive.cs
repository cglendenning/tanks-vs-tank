using TreadShred.Analytics;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// D-047-AC-05: the War Chest's free test drive. The lost mission loads again with everything in the chest on (the
/// progression's test-drive units: never saved, never used up), a countdown runs, and when it ends, the tank is lost or
/// the map is cleared, the gear goes and the War Chest reveal opens; closing it replays the mission without the gear.
/// A drive earns nothing: ManagerScore ends it before any result is counted.
/// </summary>
public sealed class TreadShredTestDrive : MonoBehaviour
{
    private static TreadShredTestDrive instance;

    private string scene;
    private Font font;
    private float left;
    private RectTransform hud;
    private Text clock;

    public static bool Active => instance != null;

    /// <summary>Starts a drive of the mission now on screen.</summary>
    public static void Begin(Font font)
    {
        if (instance != null)
            return;
        var host = new GameObject("TreadShredTestDrive");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<TreadShredTestDrive>();
        instance.font = font;
        instance.scene = SceneManager.GetActiveScene().name;
        instance.left = WarChest.TestDriveSeconds;
        TreadShredProgress.Ensure().Progression.StartTestDrive();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        TreadShredAnalytics.Log(GameEvents.OfferAccepted("war_chest_test_drive", 0, false));
        Time.timeScale = 1f;
        SceneManager.LoadScene(instance.scene);
    }

    /// <summary>Ends the drive and opens the War Chest reveal; nothing when no drive is running.</summary>
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
        TreadShredUi.CreateText(WarChest.TestDriveNote, hud, new Vector2(0f, 318f), new Vector2(620f, 30f), 24, font).color =
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
            TreadShredWarChestOffer.ShowAfterTestDrive(font, () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mission);
            });
        }
        Destroy(gameObject);
    }
}
