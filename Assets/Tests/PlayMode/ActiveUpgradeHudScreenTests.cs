using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-017-AC-10 in a real mission: the HUD shows the upgrades at work beside OVERDRIVE ACTIVE and the shields.
    public sealed class ActiveUpgradeHudScreenTests
    {
        private static PlayerProgression Progression
        {
            get
            {
                var progress = TreadShredProgress.Ensure();
                return (PlayerProgression)progress.Progression;
            }
        }

        private static CombatBoosts Boosts => (CombatBoosts)TreadShredBoosts.Current;

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static string ExtraArmor() => GameObject.Find("ExtraArmor").GetComponent<Text>().text;

        // Loads mission 2 with the given upgrade levels, then freezes time once the countdown ends so no enemy shot lands.
        // D-045: Overdrive is supply, so a mission with it running starts from a save holding Overdrive units.
        private static IEnumerator StartMission(int armor, int firepower, int speed, int overdrive = 0)
        {
            Progression.Replace(new ProgressionState
            {
                upgradeLevels = new[] { armor, firepower, speed }, clearedMissions = new[] { 1L, 0L, 0L },
                supply = new[] { 0, 0, 0, overdrive, 0, 0, 0 }
            });
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv2");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            Time.timeScale = 0f;
        }

        [UnityTest(Description = "D-017-AC-10: with Overdrive armed and every track upgraded, the HUD shows OVERDRIVE ACTIVE, a line per upgraded track in the corner and +2 beside the shields, which counts down with hits; without Overdrive only the upgrades show; both pass the screen audit on every shape")]
        public IEnumerator HudShowsTheUpgradesAtWork()
        {
            var findings = new List<Finding>();
            try
            {
                yield return StartMission(4, 2, 1, overdrive: 1);
                var texts = Texts();
                Assert.Contains(OfferLabels.OverdriveActive, texts);
                Assert.Contains("ARMOR LEVEL 4", texts);
                Assert.Contains("FIREPOWER LEVEL 2", texts);
                Assert.Contains("SPEED LEVEL 1", texts);
                Assert.AreEqual("+2", ExtraArmor(), "armor level 4 is five hits: two beyond the three shields");
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "MISSION WITH OVERDRIVE AND UPGRADES"));
                }

                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoverment>();
                player.DeadPlayer(1);
                Assert.AreEqual("+1", ExtraArmor(), "a hit uses the extra armor first");
                player.DeadPlayer(1);
                Assert.AreEqual(string.Empty, ExtraArmor(), "then only the three shields are left");

                yield return StartMission(0, 0, 3);
                texts = Texts();
                CollectionAssert.DoesNotContain(texts, OfferLabels.OverdriveActive);
                Assert.Contains("SPEED LEVEL 3", texts);
                Assert.IsFalse(texts.Any(t => t.StartsWith("ARMOR LEVEL", StringComparison.Ordinal)), "only upgraded tracks are named");
                Assert.AreEqual(string.Empty, ExtraArmor(), "no armor upgrade: nothing beside the shields");
                foreach (var shape in ScreenAudit.Shapes)
                {
                    yield return ScreenAudit.Present(shape);
                    findings.AddRange(ScreenAudit.Inspect(shape, "MISSION WITH UPGRADES ONLY"));
                }
            }
            finally
            {
                Progression.Replace(new ProgressionState());
                Time.timeScale = 1f;
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
