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
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-009-AC-03 on the real victory screen: Overdrive carried over turns the offer card into an upgrade offer.
    public sealed class VictoryUpgradeCardTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        // The first mission, its countdown waited out, then won, as the screen sweep does.
        private static IEnumerator Win()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            Assert.IsNotNull(manager, "the mission has its score manager");
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        private static List<string> Texts() =>
            UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button CardButton(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        [UnityTest(Description = "D-009-AC-03: with Overdrive carried over the victory card offers the cheapest upgrade beside Overdrive's status, a coins upgrade applies with UNDO beside it, with every track maxed only the status shows, and each state fits every screen shape")]
        public IEnumerator CarriedOverdriveOffersAnUpgrade()
        {
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            var boosts = (CombatBoosts)GameType("TreadShredBoosts").GetProperty("Current").GetValue(null);
            var findings = new List<Finding>();
            try
            {
                // Armor at level 1 (bought with gems, D-017-AC-03), so the cheapest upgrade is one coins can buy.
                progression.Replace(new ProgressionState { coins = 5000, upgradeLevels = new[] { 1, 0, 0 } });
                boosts.ArmOverdrive();
                yield return Win();
                var texts = Texts();
                Assert.Contains("UPGRADE FIREPOWER", texts, "the cheapest upgrade replaces the Overdrive offer");
                Assert.Contains(OfferLabels.OverdriveStatus(0), texts, "Overdrive's status stays on the card");
                Assert.IsNull(CardButton(OfferLabels.OverdriveActive), "no button sells Overdrive while it runs");
                yield return Audit("victory with Overdrive carried over", findings);

                var upgrade = CardButton("UPGRADE FOR ");
                Assert.IsNotNull(upgrade, "the card's one-price upgrade button");
                upgrade.onClick.Invoke();
                yield return null;
                Assert.AreEqual(1, progression.Level(UpgradeTrack.Firepower), "coins apply on the tap (D-036-AC-03)");
                Assert.IsNotNull(CardButton(ProgressionLabels.UndoShort), "UNDO is the safety net on the card");
                yield return Audit("victory card after an upgrade", findings);

                var max = ProgressionRules.LevelsPerTrack;
                progression.Replace(new ProgressionState { coins = 5000, upgradeLevels = new[] { max, max, max } });
                yield return Win();
                Assert.Contains(OfferLabels.OverdriveStatus(0), Texts(), "only the status, with every track maxed");
                Assert.IsNull(CardButton(OfferLabels.OverdriveActive), "still no button that sells nothing");
                yield return Audit("victory with Overdrive carried over and every track maxed", findings);
            }
            finally
            {
                boosts.TankLost();
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
