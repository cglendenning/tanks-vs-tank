using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Opt-in native store-screenshot driver. It is dormant in every normal build;
/// the simulator harness enables it with the PlayerPrefs key below so captures
/// show genuine gameplay projectiles instead of composited artwork.
/// </summary>
public sealed class TreadShredStoreCaptureMode : MonoBehaviour
{
    private const string CaptureModeKey = "TreadShredStoreCaptureMode";
    private const int CombatSingle = 2;
    private const int CombatDual = 3;
    private const int MissionSuccess = 4;
    private const int MissionFailure = 5;
    private const int MissionSelect = 6;
    private const int Settings = 7;
    private const int Statistics = 8;
    private const int ActionCapture = 9;
    private const int RealActionCapture = 10;
    private const int Upgrades = 11;
    private const int ReviveOffer = 12;
    // D-009: the Overdrive confirmation dialog, and the panel once Overdrive is active.
    private const int OverdriveConfirm = 13;
    private const int OverdriveActive = 14;
    // D-032: taps LEADERBOARD on the start screen through its real handler.
    private const int LeaderboardTap = 15;
    // D-032: the leaderboard screen filled with a sample page, for layout review only.
    private const int LeaderboardLayout = 16;
    // D-031: the ad-reel trailer. A real mission with the camera in close; the driver aims at the
    // nearest enemy and fires through the player's own FireAt, so every kill, explosion and callout is real.
    private const int Trailer = 17;
    // D-034: one sales clip per purchasable item, each the trailer driver with only that item's effect active.
    private const int ClipOverdrive = 18;
    private const int ClipFirepower = 19;
    private const int ClipSpeed = 20;
    private const int ClipArmor = 21;
    private const int ClipRedeploy = 22;
    private const float TrailerZoom = 0.6f;
    private const float TrailerShotSeconds = 0.5f;

    private PlayerMoverment player;
    private ManagerScore score;
    private int mode;
    private float nextShotAt;
    private float resultAt = float.PositiveInfinity;
    private bool resultShown;
    private bool IsRealActionCapture
    {
        get { return mode == ActionCapture || mode == RealActionCapture; }
    }

    private bool UsesTrailerDriver => mode == Trailer || (mode >= ClipOverdrive && mode <= ClipRedeploy);

    // The Armor clip shows real hits landing on a fully armored tank: its armor is not padded.
    private bool EnemiesFire => mode == ClipArmor;

    /// <summary>True when a capture run drives the game; the first minute (D-035) stays out of its footage.</summary>
    public static bool IsInstalled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallIfEnabled()
    {
        var mode = PlayerPrefs.GetInt(CaptureModeKey, 0);
        if (mode < CombatSingle || mode > ClipRedeploy)
            return;
        IsInstalled = true;

        Debug.Log($"[STORE_CAPTURE] installing capture driver mode={mode}");

        var driver = new GameObject("Tread Shred Store Capture Mode");
        DontDestroyOnLoad(driver);
        driver.AddComponent<TreadShredStoreCaptureMode>();
    }

    private void Awake()
    {
        mode = PlayerPrefs.GetInt(CaptureModeKey, 0);
        // The capture build is only used to produce native store artwork. Do
        // not let Unity's development console cover the gameplay frame if the
        // legacy level emits a non-fatal runtime warning.
        Debug.developerConsoleEnabled = false;
        Debug.developerConsoleVisible = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == "Start")
        {
            Debug.Log($"[STORE_CAPTURE] Start loaded, mode={mode}");
            if (mode == MissionSelect)
                Invoke(nameof(ShowMissionSelect), 0.8f);
            else if (mode == Settings)
                Invoke(nameof(ShowSettings), 0.8f);
            else if (mode == Statistics)
                Invoke(nameof(ShowStatistics), 0.8f);
            else if (mode == Upgrades)
                Invoke(nameof(ShowUpgrades), 0.8f);
            else if (mode == LeaderboardTap)
                StartCoroutine(TapLeaderboard());
            else if (mode == LeaderboardLayout)
                Invoke(nameof(ShowSampleLeaderboard), 1.5f);
            else if (UsesTrailerDriver)
                Invoke(nameof(LoadTrailerScene), 0.8f);
            else
                Invoke(nameof(LoadCombatScene), 0.8f);
            return;
        }

        if (scene.name.StartsWith("Lv", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[STORE_CAPTURE] combat scene loaded: {scene.name}");
            Invoke(nameof(AttachToPlayer), IsRealActionCapture ? 0.1f : 1.2f);
        }
    }

    private void LoadTrailerScene()
    {
        // The follow camera keeps the player's tank framed once the view is zoomed in.
        PlayerPrefs.SetFloat("Camera", 0f);
        if (mode != Trailer)
            SetClipLoadout();
        if (mode == Trailer || mode == ClipOverdrive)
            TreadShredBoosts.Current.ArmOverdrive();
        // Lv17: ten enemies and few interior obstacles, so shots connect and kills keep coming.
        SceneManager.LoadScene("Lv17", LoadSceneMode.Single);
    }

    private Transform trailerTarget;
    private bool trailerTargetVisible;
    private Vector3 trailerLastPosition;
    private float trailerProgressCheckAt;
    private float trailerSidestepUntil;
    private float trailerSidestepSign = 1f;
    private float trailerLastClearShotAt;
    private int trailerRestages;
    private Vector3 trailerCameraOffset;
    private Vector3 trailerCameraVelocity;

    // Upgrade levels for a sales clip: only the clip's own track is raised, to its top level. The capture
    // script reinstalls the app before each run, so this never reaches a player's saved progress.
    private void SetClipLoadout()
    {
        var top = TreadShred.Progression.ProgressionRules.LevelsPerTrack;
        var state = new TreadShred.Progression.ProgressionState
        {
            upgradeLevels = new[] { mode == ClipArmor ? top : 0, mode == ClipFirepower ? top : 0, mode == ClipSpeed ? top : 0 }
        };
        TreadShredProgress.Ensure().Progression.Replace(state);
        // Overdrive lasts until the tank is lost, so an earlier clip's Overdrive must not carry into this one.
        PlayerPrefs.DeleteKey(TreadShred.Economy.CombatBoosts.OverdriveKey);
        PlayerPrefs.DeleteKey(TreadShred.Economy.CombatBoosts.OverdriveLostKey);
        if (mode == ClipRedeploy)
            TreadShredBoosts.Current.ArmRedeploy();
    }

    private void StartTrailer()
    {
        if (!EnemiesFire)
        {
            // Enemy weapons off for this capture only, so the reel is not cut short by an early death.
            var enemyWeapons = FindObjectsByType<BulletEmnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < enemyWeapons.Length; i++)
                enemyWeapons[i].enabled = false;
            player.countdie = 99;
        }
        // Enemies fire through BulletPlayer(), called whatever the component's state, so the Armor clip
        // limits the guns by keeping two enemies: their shells land one at a time on ten levels of armor.
        if (mode == ClipRedeploy)
            KeepEnemies(3);
        else if (mode == ClipArmor)
            KeepEnemies(2);
        StageEnemiesInView();
        var view = Camera.main;
        if (view != null)
        {
            // The level camera clamps to the arena, which loses a tank near the edge once zoomed; frame the action instead.
            var follow = view.GetComponent("Camerafllow") as Behaviour;
            if (follow != null)
                follow.enabled = false;
            var ground = new Plane(Vector3.up, player.transform.position);
            var ray = new Ray(view.transform.position, view.transform.forward);
            trailerCameraOffset = ground.Raycast(ray, out var distance) ? view.transform.position - ray.GetPoint(distance) : new Vector3(0f, 30f, -1f);
            trailerCameraOffset *= TrailerZoom;
        }
        nextShotAt = Time.time + TrailerShotSeconds;
        trailerLastClearShotAt = Time.time;
        Debug.Log("[STORE_CAPTURE] trailer armed");
    }

    // The Armored Redeploy clip ends on MISSION CLEAR within seconds: only the nearest enemies stay, and the
    // mission counts just those, so the last kill is the real victory.
    private void KeepEnemies(int count)
    {
        var enemies = FindObjectsByType<MoveEmnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        System.Array.Sort(enemies, (a, b) => (a.transform.position - player.transform.position).sqrMagnitude
            .CompareTo((b.transform.position - player.transform.position).sqrMagnitude));
        for (var i = count; i < enemies.Length; i++)
            enemies[i].gameObject.SetActive(false);
        if (score != null)
            score.CountEmnemy = Mathf.Min(count, enemies.Length);
    }

    // Capture-only staging: moves the mission's real enemies (behaviour and kill logic intact) onto open
    // ground in a fan in front of the player, each with a clear line from the gun and no wall beside it.
    private void StageEnemiesInView()
    {
        var enemies = FindObjectsByType<MoveEmnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var staged = 0;
        for (var i = 0; i < enemies.Length; i++)
        {
            if (PlaceInView(enemies[i].transform, i))
                staged++;
        }
        Debug.Log($"[STORE_CAPTURE] trailer staged {staged} of {enemies.Length} enemies in view");
    }

    private bool PlaceInView(Transform enemy, int seed)
    {
        var origin = player.transform.position;
        var forward = -origin;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var angle = ((seed * 37 + attempt * 23) % 100) - 50f;
            var distance = 6f + ((seed + attempt) % 5) * 2.2f;
            var spot = origin + Quaternion.Euler(0f, angle, 0f) * forward * distance;
            if (!UnityEngine.AI.NavMesh.SamplePosition(spot, out var onMesh, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
                continue;
            spot = onMesh.position;
            if (WallBetween(origin + Vector3.up * 0.4f, spot + Vector3.up * 0.4f) || Crowded(spot))
                continue;
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.Warp(spot);
            else
                enemy.position = spot;
            Physics.SyncTransforms();
            return true;
        }
        return false;
    }

    private static bool WallBetween(Vector3 from, Vector3 to)
    {
        foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to)))
        {
            if (hit.collider.CompareTag("thanh"))
                return true;
        }
        return false;
    }

    private static bool Crowded(Vector3 spot)
    {
        foreach (var nearby in Physics.OverlapSphere(spot, 1.1f))
        {
            if (nearby.CompareTag("thanh") || nearby.CompareTag("targetMask"))
                return true;
        }
        return false;
    }

    // Game time stands still during the mission countdown, so firing starts on ENGAGE.
    private void TrailerTick()
    {
        if (player == null || Time.timeScale == 0f || Time.time < nextShotAt)
            return;
        nextShotAt = Time.time + TrailerShotSeconds;
        trailerTarget = null;
        var best = float.MaxValue;
        // Every mission's enemy tanks drive with MoveEmnemy, whatever they are tagged.
        foreach (var enemy in FindObjectsByType<MoveEmnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var distance = (enemy.transform.position - player.transform.position).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                trailerTarget = enemy.transform;
            }
        }
        trailerTargetVisible = trailerTarget != null && ClearShot(trailerTarget);
        if (trailerTargetVisible)
        {
            trailerLastClearShotAt = Time.time;
            player.FireAt(trailerTarget.position);
        }
        else if (trailerTarget != null && Time.time - trailerLastClearShotAt > 1.5f)
        {
            // An enemy pinned behind a wall would stall the reel: bring it back into the open.
            PlaceInView(trailerTarget, ++trailerRestages);
            trailerLastClearShotAt = Time.time;
        }
    }

    // True when nothing but the target stands between the player's gun and it.
    private bool ClearShot(Transform target)
    {
        var from = player.pointbullet.position;
        var to = target.position + Vector3.up * 0.3f;
        foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to)))
        {
            if (hit.transform.IsChildOf(player.transform) || hit.collider.isTrigger)
                continue;
            if (!hit.transform.IsChildOf(target))
                return false;
        }
        return true;
    }

    // With no clear shot, drive toward the nearest enemy through the tank's own Move, as the joystick
    // would; sidestep for a moment whenever a wall stops progress.
    private void DriveTrailer()
    {
        if (mode == ClipSpeed)
        {
            StrafeAtSpeed();
            return;
        }
        if (trailerTarget == null || trailerTargetVisible || Time.timeScale == 0f)
            return;
        var toward = trailerTarget.position - player.transform.position;
        toward.y = 0f;
        var direction = toward.normalized;
        if (Time.time >= trailerProgressCheckAt)
        {
            if ((player.transform.position - trailerLastPosition).sqrMagnitude < 0.04f && Time.time >= trailerSidestepUntil)
            {
                trailerSidestepUntil = Time.time + 0.9f;
                trailerSidestepSign = -trailerSidestepSign;
            }
            trailerLastPosition = player.transform.position;
            trailerProgressCheckAt = Time.time + 0.5f;
        }
        if (Time.time < trailerSidestepUntil)
            direction = Quaternion.Euler(0f, 90f * trailerSidestepSign, 0f) * direction;
        player.Move(direction.x, direction.z);
        player.transform.GetChild(1).rotation = Quaternion.LookRotation(direction);
    }

    private float strafeFlipAt;
    private float strafeSign = 1f;

    // The Speed clip keeps the tank moving the whole time: circling the nearest enemy, closing in when far,
    // turning back every couple of seconds, while the trailer driver keeps firing on clear shots.
    private void StrafeAtSpeed()
    {
        if (trailerTarget == null || Time.timeScale == 0f)
            return;
        var toward = trailerTarget.position - player.transform.position;
        toward.y = 0f;
        if (Time.time >= strafeFlipAt)
        {
            strafeSign = -strafeSign;
            strafeFlipAt = Time.time + 1.8f;
        }
        var across = Quaternion.Euler(0f, 90f * strafeSign, 0f) * toward.normalized;
        var direction = (across + toward.normalized * Mathf.Clamp01((toward.magnitude - 6f) / 6f)).normalized;
        player.Move(direction.x, direction.z);
    }

    // Keeps the shooter and the current target in frame, moving smoothly between kills.
    private void LateUpdate()
    {
        if (!UsesTrailerDriver || player == null || Camera.main == null || trailerCameraOffset == Vector3.zero)
            return;
        DriveTrailer();
        var focus = trailerTarget != null ? Vector3.Lerp(player.transform.position, trailerTarget.position, 0.4f) : player.transform.position;
        var view = Camera.main.transform;
        view.position = Vector3.SmoothDamp(view.position, focus + trailerCameraOffset, ref trailerCameraVelocity, 0.35f, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    private void LoadCombatScene()
    {
        if (IsRealActionCapture)
        {
            // Authentic action capture: start a later mission with the same
            // one-screen overdrive state awarded by the rewarded-ad callback.
            // The simulator will supply real taps through the normal input
            // path; this driver does not synthesize projectiles or impacts.
            TreadShredBoosts.Current.ArmOverdrive();
            TreadShredBoosts.Current.ArmRedeploy();
            SceneManager.LoadScene("Lv26", LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene("Lv1", LoadSceneMode.Single);
    }

    private void ShowMissionSelect()
    {
        var menu = FindObjectOfType<LoadLv>();
        if (menu != null)
            menu.PlayToMenu();
    }

    private void ShowSettings()
    {
        var menu = FindObjectOfType<LoadLv>();
        if (menu != null)
            menu.Option();
    }

    private void ShowReviveOffer()
    {
        if (player != null)
            TreadShredRevivePanel.TryOffer(player);
    }

    private void ShowUpgrades()
    {
        var menu = FindObjectOfType<LoadLv>();
        if (menu != null)
            menu.Upgrades();
    }

    private void ShowStatistics()
    {
        var menu = FindObjectOfType<LoadLv>();
        if (menu != null)
            menu.Static();
    }

    private void AttachToPlayer()
    {
        player = FindObjectOfType<PlayerMoverment>();
        score = FindObjectOfType<ManagerScore>();
        Debug.Log($"[STORE_CAPTURE] attach player={(player != null)} score={(score != null)} mode={mode}");
        if (UsesTrailerDriver && player != null)
        {
            StartTrailer();
            return;
        }
        if (mode == ReviveOffer && player != null)
        {
            Invoke(nameof(ShowReviveOffer), 2.5f);
            return;
        }
        if (IsRealActionCapture)
        {
            // Keep the authentic player fire/explosion sequence alive long
            // enough to photograph. Enemy weapons remain untouched in normal
            // builds; this capture-only mode disables their fire so the frame
            // is not lost to an unrelated early death.
            var enemyWeapons = FindObjectsByType<BulletEmnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < enemyWeapons.Length; i++)
                enemyWeapons[i].enabled = false;
            player.countdie = 99;
            player.isstop = false;
            if (score != null)
            {
                if (score.VictoryofFail != null)
                    score.VictoryofFail.SetActive(false);
                if (score.VictoryObj != null)
                    score.VictoryObj.SetActive(false);
                if (score.FailureObj != null)
                    score.FailureObj.SetActive(false);
                score.CurHeath = 3;
            }
            nextShotAt = float.PositiveInfinity;
            return;
        }

        nextShotAt = Time.time + (mode == ActionCapture ? 1.4f : 0.2f);
        if (mode == MissionSuccess || mode == MissionFailure || ShowsOverdriveOffer)
            resultAt = Time.time + 4.5f;
    }

    private void Update()
    {
        if (UsesTrailerDriver)
        {
            TrailerTick();
            return;
        }
        if (mode >= CombatSingle && mode <= RealActionCapture)
            Debug.developerConsoleVisible = false;

        if (IsRealActionCapture)
        {
            // Some legacy later-level scenes ship with the result panel
            // enabled in the serialized scene. Keep the authentic mission
            // visible for this capture and let player-fired rounds determine
            // the actual hit/explosion frame.
            if (score != null)
            {
                if (score.VictoryofFail != null)
                    score.VictoryofFail.SetActive(false);
                if (score.VictoryObj != null)
                    score.VictoryObj.SetActive(false);
                if (score.FailureObj != null)
                    score.FailureObj.SetActive(false);
            }
            if (player != null)
                player.isstop = false;
        }

        if (player == null || player.pointbullet == null || player.Bullet == null)
            return;

        if (!resultShown && Time.time >= resultAt)
        {
            ShowMissionResult();
            return;
        }

        if (Time.time < nextShotAt)
            return;

        var dual = mode == CombatDual || mode == ActionCapture;
        nextShotAt = Time.time + (dual ? 0.5f : 0.58f);
        FireRound(dual);
    }

    private void ShowMissionResult()
    {
        resultShown = true;
        Debug.Log($"[STORE_CAPTURE] presenting result mode={mode}");
        player.isstop = true;
        var score = FindObjectOfType<ManagerScore>();
        if (score == null)
        {
            Debug.LogError("[STORE_CAPTURE] ManagerScore not found");
            return;
        }

        if (mode == MissionSuccess || ShowsOverdriveOffer)
        {
            score.CountEmnemy = 1;
            score.CurDieEmnemy = 1;
            score.CurCountbullet = 1;
            score.CurHeath = 3;
            score.Victory();
            StartCoroutine(EnsureResultPanel(score, true));
            if (ShowsOverdriveOffer)
                StartCoroutine(UseOverdriveOffer(score));
        }
        else
        {
            score.Failure();
            StartCoroutine(EnsureResultPanel(score, false));
        }
    }

    // Logs frames before and after the tap, so a frozen game shows up in the capture log.
    private System.Collections.IEnumerator TapLeaderboard()
    {
        yield return new WaitForSeconds(3f);
        var menu = FindObjectOfType<LoadLv>();
        Debug.Log($"[STORE_CAPTURE] leaderboard tap frame={Time.frameCount} menu={(menu != null)}");
        if (menu != null)
            menu.OpenLeaderboard();
        yield return new WaitForSecondsRealtime(4f);
        Debug.Log($"[STORE_CAPTURE] four seconds after tap frame={Time.frameCount} timeScale={Time.timeScale}");
    }

    // Sample rows exercise the layout: long names, big scores, and the player below the top 25.
    private void ShowSampleLeaderboard()
    {
        string[] first = { "IRON", "GHOST", "STEEL", "RAPID", "NIGHT", "STORM", "BRAVO", "DELTA" };
        string[] second = { "VIPER", "HAMMER", "FALCON", "ANVIL", "RAZOR", "TALON", "SABER" };
        var rows = new System.Collections.Generic.List<LeaderboardEntry>();
        for (var i = 0; i < LeaderboardPage.TopCount; i++)
            rows.Add(new LeaderboardEntry(i + 1, first[i % first.Length] + " " + second[i % second.Length] + " " + (10 + i * 3),
                155000 - i * 4100, false));
        rows[3] = new LeaderboardEntry(4, "averyveryverylongnickname", rows[3].Score, false);
        var page = new LeaderboardPage(rows, new LeaderboardEntry(1284, "craig", 23400, true));
        TreadShredLeaderboardPanel.OpenWithPage(TreadShredUi.FindFont(GameObject.Find("Canvas")), page);
    }

    private bool ShowsOverdriveOffer => mode == OverdriveConfirm || mode == OverdriveActive;

    // Taps the offer through its real button and, for the active shot, confirms the real dialog.
    private System.Collections.IEnumerator UseOverdriveOffer(ManagerScore score)
    {
        yield return new WaitForSeconds(3.5f);
        if (score.OfferButton == null)
        {
            Debug.LogError("[STORE_CAPTURE] Overdrive offer button not found");
            yield break;
        }
        score.OfferButton.onClick.Invoke();
        if (mode != OverdriveActive)
            yield break;
        yield return new WaitForSeconds(1f);
        var dialog = FindObjectOfType<TreadShredConfirmDialog>();
        if (dialog == null)
            Debug.LogError("[STORE_CAPTURE] confirmation dialog not shown");
        else
            dialog.Confirm();
    }

    private System.Collections.IEnumerator EnsureResultPanel(ManagerScore score, bool success)
    {
        yield return new WaitForSeconds(2.5f);
        var result = score.VictoryofFail;
        var selected = success ? score.VictoryObj : score.FailureObj;
        Debug.Log($"[STORE_CAPTURE] result state root={(result != null && result.activeSelf)} selected={(selected != null && selected.activeSelf)}");
        if (result != null && !result.activeSelf)
            result.SetActive(true);
        if (selected != null && !selected.activeSelf)
            selected.SetActive(true);
    }

    private void FireRound(bool dual)
    {
        var origin = player.pointbullet.position;
        var direction = player.pointbullet.forward;
        var barrelRight = player.pointbullet.right;
        SpawnProjectile(origin + (dual ? -barrelRight * 0.42f : Vector3.zero),
            dual ? (direction - barrelRight * 0.08f).normalized : direction);
        if (dual)
            SpawnProjectile(origin + barrelRight * 0.42f, (direction + barrelRight * 0.08f).normalized);
    }

    private void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        var projectile = Instantiate(player.Bullet, origin, Quaternion.LookRotation(direction));
        var body = projectile.GetComponent<Rigidbody>();
        if (body != null)
            body.linearVelocity = direction * 15f;
    }
}
