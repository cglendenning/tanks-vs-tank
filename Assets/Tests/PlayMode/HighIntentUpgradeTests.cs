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
    // D-036 on the real upgrade screen: a coins upgrade applies on the tap, and the money labels fit.
    public sealed class HighIntentUpgradeTests
    {
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

        [UnityTest(Description = "D-036-AC-03: an upgrade the coins cover applies on the tap with no dialog, UNDO UPGRADE reverses it, and the purchase labels fit every screen shape")]
        public IEnumerator CoinsUpgradeAppliesOnTheTap()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            var wallet = (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));
            progression.Replace(new ProgressionState { coins = 500 });
            var findings = new List<Finding>();

            var panel = (Component)GameType("TreadShredUpgradePanel").GetMethod("Show").Invoke(null, new object[] { font });
            yield return null;
            panel.GetType().GetMethod("Upgrade", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, new object[] { UpgradeTrack.Firepower });
            yield return null;
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType(GameType("TreadShredConfirmDialog")), "no dialog for an upgrade the coins cover");
            Assert.AreEqual((1, 300), (progression.Level(UpgradeTrack.Firepower), progression.Coins), "applied on the tap");
            var undo = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.GetComponentInChildren<Text>()?.text == ProgressionLabels.Undo);
            Assert.IsTrue(undo.gameObject.activeInHierarchy, "UNDO UPGRADE is the safety net");
            undo.onClick.Invoke();
            yield return null;
            Assert.AreEqual((0, 500), (progression.Level(UpgradeTrack.Firepower), progression.Coins), "undo gives the coins back");

            // No coins and no gems: every row shows what buys it, the store's price (the editor's test store reports
            // its own) or the pack's gems before the store answers, and those labels must fit.
            progression.Replace(new ProgressionState { coins = 0 });
            wallet.TrySpend(wallet.Balance);
            yield return null;
            var labels = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            Assert.IsTrue(labels.Any(t => t.StartsWith("UPGRADE: ", StringComparison.Ordinal)),
                "a row the player cannot pay for names what buys it: " + string.Join(" | ", labels.Where(t => t.StartsWith("UPGRADE", StringComparison.Ordinal))));
            Assert.IsFalse(labels.Any(t => t.Contains(" COINS + ")), "no row mixes currencies");
            yield return Audit("upgrades priced in money", findings);

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
