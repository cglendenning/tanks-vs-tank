using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TreadShred.BuildPolicy.Tests
{
    public sealed class FirstMinuteBuildTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static string Read(string relative) => File.ReadAllText(Path.Combine(Application.dataPath, relative));

        [Test(Description = "D-035-AC-02: mission 1 has five enemy tanks, all beyond the inner wall at the start, and its enemy total matches")]
        public void MissionOneHasFiveTanksBeyondTheWall()
        {
            EditorSceneManager.OpenScene("Assets/Scence/Lv1.unity", OpenSceneMode.Single);
            var enemies = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.CompareTag("targetMask") && t.GetComponent<Collider>() != null).ToList();
            var wall = GameObject.Find("Obstance").GetComponent<Collider>().bounds;
            var player = GameObject.FindGameObjectWithTag("player").transform.position;
            var manager = Object.FindFirstObjectByType(GameType("ManagerScore"));

            Assert.AreEqual(5, enemies.Count);
            Assert.AreEqual(5, (int)GameType("ManagerScore").GetField("CountEmnemy").GetValue(manager));
            Assert.Less(player.z, wall.min.z, "the player starts below the inner wall");
            foreach (var enemy in enemies)
                Assert.Greater(enemy.position.z, wall.max.z, enemy.name + " starts beyond the inner wall");
        }

        [Test(Description = "D-035-AC-04: the preview flag reaches only a build made with TREAD_SHRED_FIRST_MINUTE_PREVIEW=1 and is never saved to the project")]
        public void PreviewFlagNeverReachesTheProject()
        {
            var automation = Read("Editor/TankBuildAutomation.cs");
            Assert.AreEqual(2, automation.Split(new[] { "extraScriptingDefines = PreviewDefines()" }, StringSplitOptions.None).Length - 1,
                "the iOS and Android builds pass it per build");
            StringAssert.Contains("Environment.GetEnvironmentVariable(\"TREAD_SHRED_FIRST_MINUTE_PREVIEW\") == \"1\"", automation);
            StringAssert.DoesNotContain("SetScriptingDefineSymbols", automation, "defines saved to the project would reach store builds");
            var settings = File.ReadAllBytes(Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectSettings.asset"));
            Assert.IsFalse(Encoding.ASCII.GetString(settings).Contains("TREADSHRED_FIRST_MINUTE_PREVIEW"), "the project settings never hold the preview flag");
        }

        [Test(Description = "D-035-AC-05: builds turn off the Made with Unity splash, so the game opens on its own screen")]
        public void UnitySplashIsOff()
        {
            var automation = Read("Editor/TankBuildAutomation.cs");
            StringAssert.Contains("PlayerSettings.SplashScreen.show = false;", automation);
            StringAssert.Contains("PlayerSettings.SplashScreen.showUnityLogo = false;", automation);
        }
    }
}
