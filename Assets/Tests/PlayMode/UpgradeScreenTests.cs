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
    // The real upgrade screen: confirm, upgrade, undo, with every state audited on every screen shape.
    public sealed class UpgradeScreenTests
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

        [UnityTest(Description = "D-017-AC-06: on the upgrade screen an upgrade is confirmed, then UNDO UPGRADE returns its coins and gems, and every step fits on every screen shape")]
        public IEnumerator ConfirmUpgradeThenUndo()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Start");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            var font = (Font)GameType("TreadShredUi").GetMethod("FindFont").Invoke(null, new object[] { GameObject.Find("Canvas") });
            var progression = (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));
            var wallet = (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));
            progression.Replace(new ProgressionState { coins = 33 });
            wallet.CreditReward(20);
            var gems = wallet.Balance;
            var findings = new List<Finding>();

            var panel = (Component)GameType("TreadShredUpgradePanel").GetMethod("Show").Invoke(null, new object[] { font });
            yield return null;
            panel.GetType().GetMethod("Upgrade", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, new object[] { UpgradeTrack.Armor });
            yield return null;
            var dialog = (Component)UnityEngine.Object.FindFirstObjectByType(GameType("TreadShredConfirmDialog"));
            Assert.IsNotNull(dialog, "D-017-AC-05: the upgrade asks first");
            Assert.AreEqual((0, 33), (progression.Level(UpgradeTrack.Armor), progression.Coins), "D-017-AC-05: nothing spent before confirming");
            yield return Audit("upgrade confirmation", findings);

            dialog.GetType().GetMethod("Confirm").Invoke(dialog, null);
            yield return null;
            Assert.AreEqual((1, 13, gems - 9), (progression.Level(UpgradeTrack.Armor), progression.Coins, wallet.Balance));
            var undo = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.GetComponentInChildren<Text>()?.text == ProgressionLabels.Undo);
            Assert.IsTrue(undo.gameObject.activeInHierarchy, "UNDO UPGRADE shows after an upgrade");
            yield return Audit("upgrades after an upgrade", findings);

            undo.onClick.Invoke();
            yield return null;
            Assert.AreEqual((0, 33, gems), (progression.Level(UpgradeTrack.Armor), progression.Coins, wallet.Balance));
            Assert.IsFalse(undo.gameObject.activeInHierarchy, "nothing left to undo");
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(t => t.text == "ARMOR BACK TO LEVEL 0. 20 COINS AND 9 GEMS RETURNED"), "the screen says what came back");
            yield return Audit("upgrades after an undo", findings);

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
