using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-019 on the real screens: the 120-mission list, the tier in the HUD, NEXT MISSION through the promotion into
    // Veteran and past Mission 120, and tiers that play harder.
    public sealed class CampaignScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string WalletKey = "TreadShredGemWallet";
        private static readonly string[] Keys =
        {
            "TreadShredTier", "TreadShredPromotionPending", "TreadShredCampaignClearedPending", "Sound", "Lv40", "Lv120"
        };

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Progress => GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression => (PlayerProgression)Progress.GetType().GetProperty("Progression").GetValue(Progress);

        private static GemWallet Wallet
        {
            get
            {
                var store = GameType("TreadShredStore").GetMethod("Ensure").Invoke(null, null);
                return (GemWallet)store.GetType().GetProperty("Wallet").GetValue(store);
            }
        }

        private static void SelectTier(DifficultyTier tier) => Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { tier });

        private static List<Text> Texts() => UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy).ToList();

        private static Button ButtonStarting(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy
                && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator LoadMission(int number, int? seed = null)
        {
            SelectTier(Campaign.TierOf(number));
            // Enemies wander at random until the tank sees them; the same seed gives every tier the same wandering.
            if (seed is int value)
                UnityEngine.Random.InitState(value);
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv" + Campaign.MapOf(number));
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static IEnumerator Win()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        // NEXT MISSION, then NO THANKS, PLAY on the offer it opens first (D-040).
        private static IEnumerator NextMission()
        {
            var buttons = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
            buttons.GetType().GetMethod("NextLv").Invoke(buttons, null);
            yield return new WaitForSecondsRealtime(0.5f);
            ButtonStarting("NO THANKS, PLAY")?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Runs a test body with the player's progress, wallet and campaign keys put back afterwards.
        private static IEnumerator Restoring(IEnumerator body)
        {
            var wallet = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                while (body.MoveNext())
                    yield return body.Current;
            }
            finally
            {
                Time.timeScale = 1f;
                foreach (var overlay in new[] { "TreadShredPromotion", "TreadShredRevive", "TreadShredNextMissionOffer" })
                {
                    var open = GameObject.Find(overlay);
                    if (open != null)
                        UnityEngine.Object.Destroy(open);
                }
                foreach (var key in Keys)
                    PlayerPrefs.DeleteKey(key);
                Progression.Replace(new ProgressionState());
                SelectTier(DifficultyTier.Recruit);
                Wallet.Replace(JsonUtility.FromJson<WalletState>(wallet));
            }
        }

        private static long All => (1L << Campaign.MissionsPerTier) - 1;


        [UnityTest(Description = "D-019-AC-03: the mission list shows all 120 missions under RECRUIT, VETERAN and ELITE headers with no tier picker, unlocked in order from saved per-tier progress (D-019-AC-01, D-019-AC-06), on every screen shape")]
        public IEnumerator MissionListShowsTheCampaign()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                // A save from before the campaign: every Recruit mission and Veteran 1-5 cleared, Veteran open.
                Progression.Replace(new ProgressionState { clearedMissions = new[] { All, 31L, 0L }, highestUnlockedTier = 1 });
                Time.timeScale = 1f;
                SceneManager.LoadScene("Start");
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                ButtonStarting("START MISSION").onClick.Invoke();
                yield return new WaitForSecondsRealtime(3.5f);

                var rows = GameObject.Find("Canvas/PanelMenu/Scroll/Panel").GetComponentsInChildren<Button>()
                    .Where(b => b.transform.parent.name == "Panel")
                    .ToDictionary(b => int.Parse(b.transform.Find("Text").GetComponent<Text>().text.Substring(8)));
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 120), rows.Keys);
                bool Locked(int n) => rows[n].transform.Find("Medel/Lock").gameObject.activeSelf;
                Assert.IsFalse(Locked(1));
                Assert.IsFalse(Locked(41), "mission 40 cleared opens Veteran");
                Assert.IsFalse(Locked(46), "Veteran 1-5 cleared: mission 46 is next");
                Assert.IsTrue(Locked(47));
                Assert.IsTrue(Locked(81));
                Assert.IsNotNull(rows[46].transform.Find("Play"), "the list opens on mission 46 with its play arrow");

                var headers = Texts().Where(t => t.text.Contains(" · ")).ToList();
                CollectionAssert.AreEquivalent(new[] { "RECRUIT · 1–40", "VETERAN · 41–80", "ELITE · 81–120" }, headers.Select(t => t.text));
                foreach (var header in headers)
                    Assert.IsTrue(header.font.HasCharacter('·') && header.font.HasCharacter('–'), header.font.name + " draws the header");
                Assert.IsNull(ButtonStarting("VETERAN"), "no tier picker");
                Assert.IsNull(GameObject.Find("TreadShredTierSelector"));
                yield return Audit("MISSION LIST, 120 MISSIONS", findings);
            }
        }

        [UnityTest(Description = "D-019-AC-05: the first clear of Mission 40 then NEXT MISSION plays the promotion (shell casings, the shell-rain sound, PROMOTED TO VETERAN, what changes, the gems) and then Mission 41 on Veteran, named in the HUD (D-019-AC-03, D-019-AC-04)")]
        public IEnumerator FortyLeadsToTheVeteranPromotion()
        {
            var findings = new List<Finding>();
            yield return Restoring(Body());
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));

            IEnumerator Body()
            {
                PlayerPrefs.SetFloat("Sound", 0f);
                Progression.Replace(new ProgressionState { clearedMissions = new[] { All >> 1, 0L, 0L } });
                yield return LoadMission(40);
                CollectionAssert.Contains(Texts().Select(t => t.text), "MISSION 40 · RECRUIT", "the HUD names the mission and tier");
                var gems = Wallet.Balance;
                yield return Win();
                yield return NextMission();

                var promotion = GameObject.Find("TreadShredPromotion");
                Assert.IsNotNull(promotion, "the promotion plays before Mission 41");
                var texts = Texts().Select(t => t.text).ToList();
                CollectionAssert.Contains(texts, "PROMOTED TO VETERAN");
                CollectionAssert.Contains(texts, "ENEMIES ARE TOUGHER, FASTER AND FIRE MORE OFTEN");
                CollectionAssert.Contains(texts, "+30 GEMS FOR YOUR PROMOTION");
                Assert.AreEqual(gems + ProgressionRules.TierUnlockGems(DifficultyTier.Veteran) + ProgressionRules.MilestoneMissionGems, Wallet.Balance,
                    "the promotion gems and Mission 40's milestone gems were paid");
                Assert.AreEqual(70, promotion.GetComponentsInChildren<Image>().Count(i => i.name == "Casing"));
                var sound = promotion.GetComponent<AudioSource>();
                Assert.IsNotNull(sound, "the shell rain plays");
                Assert.AreEqual("ShellRain", sound.clip.name);
                Assert.IsTrue(sound.isPlaying);
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Audit("PROMOTION TO VETERAN", findings);

                for (var waited = 0f; SceneManager.GetActiveScene().name != "Lv1" && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                yield return new WaitForSecondsRealtime(2.5f);
                Assert.AreEqual("Lv1", SceneManager.GetActiveScene().name, "Mission 41 plays the first map");
                CollectionAssert.Contains(Texts().Select(t => t.text), "MISSION 41 · VETERAN");
                Assert.IsNull(GameObject.Find("TreadShredPromotion"));
                yield return Audit("MISSION 41 ON VETERAN", findings);
            }
        }

        [UnityTest(Description = "D-019-AC-04: after Mission 120, NEXT MISSION returns to the mission list with ALL 120 MISSIONS CLEARED")]
        public IEnumerator AfterOneTwentyTheCampaignIsCleared()
        {
            yield return Restoring(Body());

            IEnumerator Body()
            {
                Progression.Replace(new ProgressionState { clearedMissions = new[] { All, All, All >> 1 }, highestUnlockedTier = 2 });
                yield return LoadMission(120);
                CollectionAssert.Contains(Texts().Select(t => t.text), "MISSION 120 · ELITE");
                yield return Win();
                yield return NextMission();
                yield return new WaitForSecondsRealtime(2.5f);
                Assert.AreEqual("Start", SceneManager.GetActiveScene().name);
                CollectionAssert.Contains(Texts().Select(t => t.text), "ALL 120 MISSIONS CLEARED");
            }
        }

        [UnityTest(Description = "D-019-AC-02: with base upgrades, one enemy in sight of a tank that never moves (13 hits of armor) destroys it, timed from its first hit, in at most 0.6 of a Recruit enemy's time on Veteran and 0.4 on Elite")]
        public IEnumerator HigherTierEnemiesDestroyATankFaster()
        {
            var times = new Dictionary<int, float>();
            yield return Restoring(Body());
            var recruit = times[17];
            Assert.LessOrEqual(times[57], recruit * 0.6f, $"Veteran {times[57]:0.00} s against Recruit {recruit:0.00} s");
            Assert.LessOrEqual(times[97], recruit * 0.4f, $"Elite {times[97]:0.00} s against Recruit {recruit:0.00} s");

            IEnumerator Body()
            {
                Progression.Replace(new ProgressionState { clearedMissions = new[] { All, All, 0L }, highestUnlockedTier = 2 });
                // The same open map in each tier (Mission 17's: few interior walls).
                foreach (var number in new[] { 17, 57, 97 })
                {
                    yield return LoadMission(number);
                    var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
                    var sight = UnityEngine.Object.FindFirstObjectByType(GameType("FieldOfView")) as Component;
                    Assert.IsNotNull(sight, "the tank's field of view");
                    // Thirteen hits of armor, so the time covers about six reloads and no single reload's timing decides it.
                    var health = player.GetType().GetField("countdie");
                    const int startHits = 13;
                    health.SetValue(player, startHits);

                    // One enemy that fires, frozen in clear sight; every other enemy stands down.
                    var enemies = UnityEngine.Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None)
                        .Cast<Component>().Where(e => e.CompareTag("emnemyred") == false).ToList();
                    Assert.IsNotEmpty(enemies, $"mission {number} has an enemy that fires");
                    var shooter = enemies[0];
                    foreach (var gun in UnityEngine.Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None).Cast<Component>())
                    {
                        if (gun.gameObject != shooter.gameObject)
                            gun.gameObject.SetActive(false);
                    }
                    foreach (var mover in UnityEngine.Object.FindObjectsByType(GameType("MoveEmnemy"), FindObjectsSortMode.None).Cast<Component>())
                    {
                        if (mover.gameObject != shooter.gameObject)
                            mover.gameObject.SetActive(false);
                        else
                            ((Behaviour)mover).enabled = false;
                    }
                    Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None).Length, "one enemy can fire");
                    var body = shooter.GetComponent<Rigidbody>();
                    if (body != null)
                        body.isKinematic = true;
                    var origin = sight.transform.position;
                    shooter.transform.position = new Vector3(origin.x, shooter.transform.position.y, origin.z) + sight.transform.forward * 5f;
                    // Aimed before the clock starts, so the first shell always lands and every tier is timed from the same shell.
                    shooter.GetType().GetMethod("LookPlayer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shooter, null);

                    Time.captureDeltaTime = 1f / 120f;
                    float firstHit = -1f, start = Time.time;
                    while (GameObject.Find("TreadShredRevive") == null && Time.time - start < 60f)
                    {
                        if (firstHit < 0f && (int)health.GetValue(player) < startHits)
                            firstHit = Time.time;
                        yield return null;
                    }
                    var destroyed = Time.time;
                    Time.captureDeltaTime = 0f;
                    Assert.IsNotNull(GameObject.Find("TreadShredRevive"), $"mission {number}: the enemy destroyed the tank within 60 s");
                    Assert.Greater(firstHit, 0f, $"mission {number}: the tank was hit");
                    times[number] = destroyed - firstHit;
                    Debug.Log($"[D-019] mission {number}: one enemy destroyed the tank {times[number]:0.00} s after its first hit");
                    UnityEngine.Object.Destroy(GameObject.Find("TreadShredRevive"));
                    Time.timeScale = 1f;
                }
            }
        }
    }
}
