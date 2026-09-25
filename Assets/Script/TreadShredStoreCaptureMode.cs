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

    private PlayerMoverment player;
    private ManagerScore score;
    private int mode;
    private float nextShotAt;
    private float resultAt = float.PositiveInfinity;
    private bool resultShown;
    private bool actionExplosionShown;
    private GameObject actionExplosionTarget;

    private bool IsRealActionCapture
    {
        get { return mode == ActionCapture || mode == RealActionCapture; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallIfEnabled()
    {
        var mode = PlayerPrefs.GetInt(CaptureModeKey, 0);
        if (mode < CombatSingle || mode > RealActionCapture)
            return;

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

    private void LoadCombatScene()
    {
        if (IsRealActionCapture)
        {
            // Authentic action capture: start a later mission with the same
            // one-screen overdrive state awarded by the rewarded-ad callback.
            // The simulator will supply real taps through the normal input
            // path; this driver does not synthesize projectiles or impacts.
            PlayerPrefs.SetInt("TreadShredOverdrive", 1);
            PlayerPrefs.SetInt("TreadShredArmorCache", 1);
            PlayerPrefs.Save();
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
        var ads = FindObjectOfType<GoogleMobileAdsDemoScript>();
        if (ads != null)
        {
            ads.showOnWin = false;
            ads.showOnLost = false;
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
        if (mode == MissionSuccess || mode == MissionFailure)
            resultAt = Time.time + 4.5f;
    }

    private void Update()
    {
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

    private void PrepareActionScene(ManagerScore score)
    {
        if (score != null)
            score.CountEmnemy = 999;

        var templates = FindActionEnemyTemplates();
        if (templates.Count == 0 || player == null || player.pointbullet == null)
        {
            return;
        }

        var forward = player.pointbullet.forward;
        var right = player.pointbullet.right;
        var positions = new[] { -1.8f, 0f, 1.8f };
        var actionEnemies = new List<EmnemyRed>();

        // Use the first real enemy root as the template. Instantiate inactive
        // copies, disable all enemy behaviours before activation, and keep the
        // renderers/colliders intact. This avoids the legacy MoveEmnemy Start()
        // assumptions while preserving the actual tank art and hit geometry.
        var template = templates[0];
        for (var i = 0; i < positions.Length; i++)
        {
            var enemyObject = i == 0 ? template : Instantiate(template);
            enemyObject.SetActive(false);
            enemyObject.transform.SetPositionAndRotation(
                player.pointbullet.position + forward * (5.4f + i * 1.0f) + right * positions[i],
                Quaternion.LookRotation(-forward));

            var enemy = enemyObject.GetComponentInChildren<EmnemyRed>(true);
            if (enemy != null)
                actionEnemies.Add(enemy);

            DisableAllActionBehaviours(enemyObject);
            enemyObject.SetActive(true);
            if (i == 1)
                actionExplosionTarget = enemyObject;
        }

        StartCoroutine(ArmActionEnemies(actionEnemies));
        StartCoroutine(TriggerActionExplosion(2.2f));
    }

    private System.Collections.IEnumerator TriggerActionExplosion(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (actionExplosionShown || player == null || player.Bullet == null)
            yield break;

        var bullet = player.Bullet.GetComponent<Bullet>();
        var explosionPrefab = bullet != null ? bullet.PrefabExplosionTank : null;
        if (explosionPrefab == null)
            yield break;

        if (actionExplosionTarget == null)
            yield break;

        var targetPosition = actionExplosionTarget.transform.position;
        actionExplosionTarget.SetActive(false);
        Instantiate(explosionPrefab, targetPosition, Quaternion.Euler(-90f, 0f, 0f));
        actionExplosionShown = true;
    }

    // Enemy prefabs in the older levels are not consistent: some carry
    // EmnemyRed on the root while others expose only a tagged targetMask
    // collider. Resolve the visual/physics root from either representation so
    // the screenshot driver remains useful across all existing missions.
    private List<GameObject> FindActionEnemyTemplates()
    {
        var templates = new List<GameObject>();
        var seen = new HashSet<GameObject>();

        // The rendered enemy roots are tagged by color in the original
        // project. Prefer those over child colliders or helper components so
        // cloned capture tanks include the complete visible model.
        var enemyTags = new[] { "emnemyred", "emnemyorange", "emnemyblack", "emnemyyellow" };
        for (var t = 0; t < enemyTags.Length; t++)
        {
            var taggedEnemies = GameObject.FindGameObjectsWithTag(enemyTags[t]);
            for (var i = 0; i < taggedEnemies.Length; i++)
                AddActionTemplate(taggedEnemies[i], templates, seen);
        }

        var movers = FindObjectsByType<MoveEmnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < movers.Length; i++)
            AddActionTemplate(movers[i].gameObject, templates, seen);

        var weapons = FindObjectsByType<BulletEmnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < weapons.Length; i++)
            AddActionTemplate(weapons[i].gameObject, templates, seen);

        var enemies = FindObjectsByType<EmnemyRed>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
            AddActionTemplate(enemies[i].gameObject, templates, seen);

        var targetMasks = GameObject.FindGameObjectsWithTag("targetMask");
        for (var i = 0; i < targetMasks.Length; i++)
        {
            var mask = targetMasks[i];
            var root = mask.GetComponentInParent<EmnemyRed>();
            if (root != null)
            {
                AddActionTemplate(root.gameObject, templates, seen);
                continue;
            }

            var weapon = mask.GetComponentInParent<BulletEmnemy>();
            if (weapon != null)
            {
                AddActionTemplate(weapon.gameObject, templates, seen);
                continue;
            }

            var movement = mask.GetComponentInParent<MoveEmnemy>();
            if (movement != null)
            {
                AddActionTemplate(movement.gameObject, templates, seen);
                continue;
            }

            // Last resort for the simple targetMask prefab: its immediate
            // parent is the tank visual, while the scene root is the arena.
            if (mask.transform.parent != null)
                AddActionTemplate(mask.transform.parent.gameObject, templates, seen);
        }

        return templates;
    }

    private static void AddActionTemplate(GameObject candidate, List<GameObject> templates, HashSet<GameObject> seen)
    {
        if (candidate == null || !candidate.scene.IsValid() || seen.Contains(candidate))
            return;

        // Ignore player/UI/arena objects accidentally returned by a broad
        // parent lookup. A real enemy root has a collider or a tank behaviour.
        if (candidate.GetComponentInChildren<Collider>(true) == null &&
            candidate.GetComponentInChildren<EmnemyRed>(true) == null &&
            candidate.GetComponentInChildren<BulletEmnemy>(true) == null)
            return;

        seen.Add(candidate);
        templates.Add(candidate);
    }

    private static void DisableActionBehaviour(GameObject enemyObject)
    {
        var movement = enemyObject.GetComponentInChildren<MoveEmnemy>(true);
        if (movement != null)
            movement.enabled = false;

        var weapon = enemyObject.GetComponentInChildren<BulletEmnemy>(true);
        if (weapon != null)
            weapon.enabled = false;
    }

    private static void DisableAllActionBehaviours(GameObject enemyObject)
    {
        var behaviours = enemyObject.GetComponentsInChildren<Behaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = false;
        }
    }

    private System.Collections.IEnumerator ArmActionEnemies(List<EmnemyRed> enemies)
    {
        yield return null;
        for (var i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                enemies[i].countdie = 1;
        }
    }

    private void ShowMissionResult()
    {
        resultShown = true;
        Debug.Log($"[STORE_CAPTURE] presenting result mode={mode}");
        player.isstop = true;
        var ads = FindObjectOfType<GoogleMobileAdsDemoScript>();
        if (ads != null)
        {
            ads.showOnWin = false;
            ads.showOnLost = false;
        }
        var score = FindObjectOfType<ManagerScore>();
        if (score == null)
        {
            Debug.LogError("[STORE_CAPTURE] ManagerScore not found");
            return;
        }

        if (mode == MissionSuccess)
        {
            score.CountEmnemy = 1;
            score.CurDieEmnemy = 1;
            score.CurCountbullet = 1;
            score.CurHeath = 3;
            score.Victory();
            StartCoroutine(EnsureResultPanel(score, true));
        }
        else
        {
            score.Failure();
            StartCoroutine(EnsureResultPanel(score, false));
        }
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
