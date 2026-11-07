using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TreadShred.Screens.Tests
{
    // D-046 in real missions: waves, sight and volleys on Veteran and Elite.
    public sealed class TierCombatScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const long All = (1L << 40) - 1;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Progress => GameType("TreadShredProgress").GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression => (PlayerProgression)Progress.GetType().GetProperty("Progression").GetValue(Progress);

        // Every turret on the map: tanks with health (EmnemyRed) and one-hit targets.
        private static Component[] Enemies() => UnityEngine.Object.FindObjectsByType(GameType("BulletEmnemy"), FindObjectsSortMode.None)
            .Cast<Component>().Where(e => ((Behaviour)e).enabled).ToArray();

        private static int Remaining()
        {
            var score = GameType("ManagerScore").GetField("instance").GetValue(null);
            return (int)score.GetType().GetField("CountEmnemy").GetValue(score);
        }

        private static float SightRadius() =>
            (float)GameType("FieldOfView").GetField("viewRadius").GetValue(UnityEngine.Object.FindFirstObjectByType(GameType("FieldOfView")));

        private static IEnumerator Play(DifficultyTier tier, string map = "Lv1")
        {
            Progression.Replace(new ProgressionState { clearedMissions = new[] { All, All, 0L }, highestUnlockedTier = 2 });
            Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { tier });
            Time.timeScale = 1f;
            SceneManager.LoadScene(map);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        // Destroys live enemies one at a time as the tank's shells would: a tank through its health, a one-hit target the way
        // the triple shot, arc shot and seeker destroy one.
        private static IEnumerator Destroy(int count)
        {
            for (var destroyed = 0; destroyed < count; destroyed++)
            {
                var turret = Enemies().FirstOrDefault();
                if (turret == null)
                    yield break;
                var tank = turret.GetComponent(GameType("EmnemyRed"));
                if (tank != null)
                    tank.GetType().GetMethod("DeadPlayer").Invoke(tank, new object[] { 10000 });
                else
                {
                    var score = GameType("ManagerScore").GetField("instance").GetValue(null);
                    score.GetType().GetMethod("ClaimKill").Invoke(score, new object[] { turret.gameObject });
                    score.GetType().GetMethod("EnemyKilled").Invoke(score, null);
                    UnityEngine.Object.Destroy(turret.gameObject);
                }
                yield return null;
            }
        }

        private static int Alive() => Enemies().Length;

        [UnityTest(Description = "D-046-AC-05: on Veteran, half the map's enemies destroyed brings every one back at its starting spot (and Elite a third wave at half the second), the mission waits for every wave, enemies spot the tank from 1.5x Recruit's range, and the map with reinforcements passes the screen audit")]
        public IEnumerator WavesSightAndAudit()
        {
            var findings = new List<Finding>();
            try
            {
                yield return Play(DifficultyTier.Recruit);
                var recruitSight = SightRadius();

                yield return Play(DifficultyTier.Veteran);
                Assert.AreEqual(recruitSight * 1.5f, SightRadius(), 0.01f, "Veteran enemies spot the tank from 1.5x as far");
                var size = Alive();
                Assert.Greater(size, 0, "the map has enemies");
                yield return Destroy((size + 1) / 2);
                Assert.AreEqual(size - (size + 1) / 2 + size, Remaining(), "half destroyed: the whole map's enemies come back, and the victory waits for them");
                Assert.Greater(Alive(), size - (size + 1) / 2, "those whose spot is clear are already on the map");
                foreach (var screen in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(screen);
                    findings.AddRange(ScreenAudit.Inspect(screen, "VETERAN REINFORCEMENTS"));
                }
                // Waiting reinforcements appear as their spots clear; the mission ends when the victory count reaches 0.
                for (var round = 0; round < 40 && Remaining() > 0; round++)
                {
                    yield return Destroy(Alive());
                    yield return new WaitForSecondsRealtime(0.25f);
                }
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.AreEqual((0, 0), (Remaining(), Alive()), "every Veteran enemy destroyed, and no third wave");

                yield return Play(DifficultyTier.Elite);
                size = Alive();
                var half = (size + 1) / 2;
                yield return Destroy(half);
                Assert.AreEqual(2 * size - half, Remaining(), "the second wave");
                yield return Destroy(half);
                Assert.AreEqual(3 * size - 2 * half, Remaining(), "half again down: Elite's third wave arrives");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { DifficultyTier.Recruit });
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-046-AC-01: on mission 47's map (Veteran) reinforcements never appear inside an enemy or the tank, every enemy stays inside the walls, and destroying every wave wins the mission")]
        public IEnumerator ReinforcementsStayInsideTheWalls()
        {
            try
            {
                yield return Play(DifficultyTier.Veteran, "Lv7");
                // The walls (tagged thanh) bound the map.
                var walls = GameObject.FindGameObjectsWithTag("thanh").SelectMany(w => w.GetComponentsInChildren<Collider>()).Select(c => c.bounds).ToList();
                Assert.IsNotEmpty(walls, "the map has walls");
                var area = walls[0];
                foreach (var wall in walls)
                    area.Encapsulate(wall);
                float minX = area.min.x - 0.5f, maxX = area.max.x + 0.5f, minZ = area.min.z - 0.5f, maxZ = area.max.z + 0.5f;
                Debug.Log($"[D-046] map 7 walls x {minX:0.0}..{maxX:0.0}, z {minZ:0.0}..{maxZ:0.0}");
                var start = Enemies().Select(e => e.transform.position).ToList();
                yield return Destroy((start.Count + 1) / 2);
                for (var sample = 0; sample < 12; sample++)
                {
                    var alive = Enemies().Select(e => e.transform.position).ToList();
                    for (var a = 0; a < alive.Count; a++)
                    {
                        Assert.IsTrue(alive[a].x >= minX && alive[a].x <= maxX && alive[a].z >= minZ && alive[a].z <= maxZ,
                            $"an enemy at {alive[a]} left the walls (sample {sample})");
                        for (var b = a + 1; b < alive.Count; b++)
                            Assert.Greater(Vector3.Distance(alive[a], alive[b]), 1f, $"two enemies on one spot at {alive[a]} (sample {sample})");
                    }
                    yield return new WaitForSecondsRealtime(0.25f);
                }

                for (var round = 0; round < 40 && Remaining() > 0; round++)
                {
                    yield return Destroy(Alive());
                    yield return new WaitForSecondsRealtime(0.25f);
                }
                Assert.AreEqual(0, Remaining(), "every wave destroyed: the mission is won");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { DifficultyTier.Recruit });
            }
        }

        [UnityTest(Description = "D-046-AC-06: on map 1 a target goes down to one shell on Recruit, two on Veteran and three on Elite")]
        public IEnumerator TargetsTakeTierHealth()
        {
            try
            {
                var tiers = new[] { (DifficultyTier.Recruit, 1), (DifficultyTier.Veteran, 2), (DifficultyTier.Elite, 3) };
                foreach (var (tier, hits) in tiers)
                {
                    yield return Play(tier);
                    var target = Enemies().First(e => e.GetComponent(GameType("EmnemyRed")) == null).gameObject;
                    var survives = GameType("TreadShredTargetHealth").GetMethod("Survives");
                    for (var shell = 1; shell < hits; shell++)
                        Assert.IsTrue((bool)survives.Invoke(null, new object[] { target }), tier + ": shell " + shell + " leaves it standing");
                    Assert.IsFalse((bool)survives.Invoke(null, new object[] { target }), tier + ": shell " + hits + " destroys it");
                }
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { DifficultyTier.Recruit });
            }
        }

        [UnityTest(Description = "D-046-AC-04: on Veteran one enemy's attack is a volley of three shells a quarter second apart: a cannon shell, a triple shot and an arc shot")]
        public IEnumerator OneEnemyFiresAVolley()
        {
            try
            {
                yield return Play(DifficultyTier.Veteran);
                // Hold the tank's view, so only this test sets the enemy off, and let every enemy reload.
                ((MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(GameType("FieldOfView"))).StopAllCoroutines();
                yield return new WaitForSecondsRealtime(2.5f);
                var turret = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(GameType("BulletEmnemy"));
                var shots = (int)turret.GetType().GetField("volleyShot", Members).GetValue(turret);
                var before = UnityEngine.Object.FindObjectsByType(GameType("BulletRed"), FindObjectsSortMode.None).Length;

                turret.GetType().GetMethod("BulletPlayer").Invoke(turret, null);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.AreEqual(shots + 3, (int)turret.GetType().GetField("volleyShot", Members).GetValue(turret), "three shots in the volley");
                Assert.Greater(UnityEngine.Object.FindObjectsByType(GameType("BulletRed"), FindObjectsSortMode.None).Length, before,
                    "the third is an arc shot");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Progress.GetType().GetMethod("SelectTier").Invoke(Progress, new object[] { DifficultyTier.Recruit });
            }
        }
    }
}
