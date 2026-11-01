using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>D-032-AC-01: a mission clears on its final kill, so every mission's enemy total must match its enemies.</summary>
    public sealed class MissionEnemyTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [Test(Description = "D-032-AC-01: every mission's enemy total equals the enemies placed in it, so it cannot clear with an enemy left")]
        public void EveryMissionCountsItsEnemies()
        {
            var manager = GameType("ManagerScore");
            var tank = GameType("EmnemyRed");
            var rows = new List<string>();
            var problems = new List<string>();
            for (var mission = 1; mission <= 40; mission++)
            {
                EditorSceneManager.OpenScene($"Assets/Scence/Lv{mission}.unity", OpenSceneMode.Single);
                var managers = Object.FindObjectsByType(manager, FindObjectsInactive.Include, FindObjectsSortMode.None);
                var declared = managers.Length == 1 ? (int)manager.GetField("CountEmnemy").GetValue(managers[0]) : -1;
                var killable = new HashSet<GameObject>();
                var inactive = 0;
                foreach (var component in Object.FindObjectsByType(tank, FindObjectsInactive.Include, FindObjectsSortMode.None))
                    killable.Add(((Component)component).gameObject);
                foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    // Each target's model child carries the tag too but has no collider, so no bullet can hit it.
                    if (transform.CompareTag("targetMask") && transform.GetComponent<Collider>() != null)
                        killable.Add(transform.gameObject);
                }
                foreach (var enemy in killable)
                {
                    if (!enemy.activeInHierarchy)
                        inactive++;
                }
                var tanks = killable.Count(e => e.GetComponent(tank) != null);
                var row = $"Lv{mission}: declares {declared}, has {killable.Count} ({tanks} tanks, {killable.Count - tanks} one-hit targets, {inactive} inactive)";
                rows.Add(row);
                if (declared != killable.Count)
                    problems.Add(row);
            }
            Debug.Log("[MissionAudit]\n" + string.Join("\n", rows));
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
