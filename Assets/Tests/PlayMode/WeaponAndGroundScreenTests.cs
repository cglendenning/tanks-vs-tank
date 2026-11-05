using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-038 and D-039 in real missions: the weapon toggle on a special-weapon mission, and the ground of every block.
    public sealed class WeaponAndGroundScreenTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
        }

        private static Button ButtonLabeled(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>()?.text == label);

        private static Color PrimaryFill => (Color)GameType("TreadShredUi").GetField("PrimaryFill").GetValue(null);

        private static bool IsPrimary(Button button) => button.targetGraphic.color == PrimaryFill;

        private static object Player() => UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));

        private static string WeaponInHand() => Player().GetType().GetProperty("WeaponInHand").GetValue(Player()).ToString();

        [UnityTest(Description = "D-038-AC-05: on the arc-shot mission the CANNON | ARC SHOT toggle starts on ARC SHOT, switches both ways on a tap, shows TAP TO SWITCH then drops it, hides while paused, passes the screen audit on every shape, and a cannon mission has no toggle")]
        public IEnumerator WeaponToggleOnASpecialWeaponMission()
        {
            var findings = new List<Finding>();
            yield return Load("Lv16");
            var cannon = ButtonLabeled("CANNON");
            var arc = ButtonLabeled("ARC SHOT");
            Assert.IsNotNull(cannon, "the cannon side");
            Assert.IsNotNull(arc, "the mission's weapon side");
            Assert.AreEqual(("ArcShot", true, false), (WeaponInHand(), IsPrimary(arc), IsPrimary(cannon)), "starts on its special weapon, highlighted");
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "TAP TO SWITCH"));
            yield return Audit("arc-shot mission with the weapon toggle", findings);

            cannon.onClick.Invoke();
            yield return null;
            Assert.AreEqual(("Cannon", true, false), (WeaponInHand(), IsPrimary(cannon), IsPrimary(arc)), "one tap switches to the cannon");
            arc.onClick.Invoke();
            yield return null;
            Assert.AreEqual("ArcShot", WeaponInHand(), "and back");

            yield return new WaitForSecondsRealtime(6f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "TAP TO SWITCH"), "the hint goes after its seconds");

            var pause = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerButton"));
            var pauseGame = pause.GetType().GetMethod("PauseGame", BindingFlags.Public | BindingFlags.Instance);
            pauseGame.Invoke(pause, null);
            yield return null;
            Assert.IsNull(GameObject.Find("TreadShredWeaponToggle"), "hidden while paused");
            pauseGame.Invoke(pause, null);
            yield return null;
            Assert.IsNotNull(GameObject.Find("TreadShredWeaponToggle"), "back when play resumes");
            Time.timeScale = 1f;

            yield return Load("Lv2");
            Assert.IsNull(GameObject.Find("TreadShredWeaponToggle"), "a cannon mission shows no weapon control");
            Assert.IsNull(ButtonLabeled("CANNON"));
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-039-AC-03: the first mission of every block is fought on its own ground, from ground-1 on mission 1 to ground-8 on mission 36, and each passes the screen audit on every shape")]
        public IEnumerator EveryBlockHasItsGround()
        {
            var findings = new List<Finding>();
            for (var block = 1; block <= 8; block++)
            {
                var mission = (block - 1) * 5 + 1;
                yield return Load("Lv" + mission);
                var floor = GameObject.Find("Manager/PlaneGround");
                Assert.IsNotNull(floor, "Lv" + mission + " has its arena floor");
                var texture = floor.GetComponent<Renderer>().material.mainTexture;
                Assert.AreEqual("ground-" + block, texture != null ? texture.name : "none", "mission " + mission + "'s ground");
                yield return Audit("mission " + mission + " on ground " + block, findings);
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
