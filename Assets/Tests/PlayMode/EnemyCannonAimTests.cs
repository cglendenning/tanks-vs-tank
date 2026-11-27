using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TreadShred.Screens.Tests
{
    // D-046-AC-04 on mission 52's map, whose red tanks fire from 2.6 m up: their straight shells used to fly level over
    // the 1.76 m tank (owner report 2026-11-11).
    public sealed class EnemyCannonAimTests
    {
        private const long All = (1L << 40) - 1;

        private static TreadShredProgress Progress => TreadShredProgress.Ensure();

        private static PlayerProgression Progression => (PlayerProgression)Progress.Progression;

        [UnityTest(Description = "D-046-AC-04: on mission 52's map, on Veteran and Elite, every enemy's cannon shell and the middle shell of its triple shot strike a tank standing in their line of fire")]
        public IEnumerator StraightShellsStrikeAStandingTank()
        {
            var report = new List<string>();
            var misses = new List<string>();
            try
            {
                foreach (var tier in new[] { DifficultyTier.Veteran, DifficultyTier.Elite })
                {
                    Progression.Replace(new ProgressionState { clearedMissions = new[] { All, All, 0L }, highestUnlockedTier = 2 });
                    Progress.SelectTier(tier);
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("Lv12");
                    yield return null;
                    yield return new WaitForSecondsRealtime(2f);
                    for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                        yield return new WaitForSecondsRealtime(0.25f);

                    // Only this test fires, and only aim decides: walls are out of the line of fire.
                    ((MonoBehaviour)UnityEngine.Object.FindFirstObjectByType<FieldOfView>()).StopAllCoroutines();
                    foreach (var wall in GameObject.FindGameObjectsWithTag("thanh").SelectMany(w => w.GetComponentsInChildren<Collider>()))
                        wall.enabled = false;
                    var mover = (MonoBehaviour)PlayerMoverment.instance;
                    var tank = mover.gameObject;
                    var turrets = UnityEngine.Object.FindObjectsByType<BulletEmnemy>(FindObjectsSortMode.None).Cast<MonoBehaviour>().Where(t => t.enabled).ToList();
                    Assert.IsNotEmpty(turrets, "map 12 has enemies");

                    foreach (var turret in turrets)
                    {
                        foreach (var weapon in new[] { EnemyWeapon.Cannon, EnemyWeapon.TripleShot })
                        {
                            ((PlayerMoverment)mover).countdie = 99;
                            var tower = (Transform)((BulletEmnemy)turret).tower;
                            var muzzle = (Transform)((BulletEmnemy)turret).pointbullet;
                            tower.LookAt(new Vector3(tank.transform.position.x, turret.transform.position.y, tank.transform.position.z));
                            var before = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).ToList();
                            ((BulletEmnemy)turret).Fire(weapon, TierCombat.For(tier));
                            var fired = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Except(before).ToList();
                            var body = fired.First().GetComponent<Rigidbody>();
                            var line = $"{tier} {turret.name} {weapon}: muzzle y {muzzle.position.y:0.00}, {fired.Count} shell(s), " +
                                       $"start velocity {body.linearVelocity}, constraints {body.constraints}";
                            var struck = false;
                            for (var t = 0f; t < 3f && !struck; t += Time.fixedDeltaTime)
                            {
                                yield return new WaitForFixedUpdate();
                                struck = (int)((PlayerMoverment)mover).countdie < 99;
                            }
                            report.Add(line + (struck ? ": struck the tank" : ": MISSED"));
                            if (!struck)
                                misses.Add(line);
                            foreach (var shell in fired.Where(s => s != null))
                                UnityEngine.Object.Destroy(shell.gameObject);
                            yield return new WaitForSecondsRealtime(0.1f);
                        }
                    }
                }
            }
            finally
            {
                Debug.Log("[D-046 aim] " + string.Join("\n[D-046 aim] ", report));
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Progress.SelectTier(DifficultyTier.Recruit);
            }
            Assert.IsEmpty(misses, "straight shells that missed a standing tank:\n" + string.Join("\n", misses));
        }
    }
}
