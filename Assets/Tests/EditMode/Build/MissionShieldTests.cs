using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TreadShred.BuildPolicy.Tests
{
    /// <summary>D-017-AC-09: the HUD shields can only show the hits left if every mission links its three shields.</summary>
    public sealed class MissionShieldTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [Test(Description = "D-017-AC-09: every mission links its player to three HUD shields, so hits, destruction and revives show on them (mission 11 had none)")]
        public void EveryMissionLinksItsShields()
        {
            var player = GameType("PlayerMoverment");
            var problems = new List<string>();
            for (var mission = 1; mission <= 40; mission++)
            {
                EditorSceneManager.OpenScene($"Assets/Scence/Lv{mission}.unity", OpenSceneMode.Single);
                var players = Object.FindObjectsByType(player, FindObjectsInactive.Include, FindObjectsSortMode.None);
                var shields = players.Length == 1 ? ((IList)player.GetField("Heart").GetValue(players[0])).Cast<Object>().ToList() : new List<Object>();
                var linked = shields.Count(shield => shield != null);
                if (players.Length != 1 || shields.Count != 3 || linked != 3 || shields.Distinct().Count() != 3)
                    problems.Add($"Lv{mission}: {players.Length} players, {shields.Count} shield slots, {linked} linked");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
