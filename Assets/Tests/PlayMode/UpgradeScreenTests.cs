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
    // The real upgrade screen: confirm, upgrade, undo, with every state audited on every screen shape.
    public sealed class UpgradeScreenTests
    {
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
            var font = (Font)TreadShredUi.FindFont(GameObject.Find("Canvas"));
            var progression = (PlayerProgression)TreadShredProgress.Ensure().Progression;
            var wallet = (GemWallet)TreadShredStore.Ensure().Wallet;
            progression.Replace(new ProgressionState { coins = 33 });
            wallet.CreditReward(20);
            var gems = wallet.Balance;
            var findings = new List<Finding>();

            var panel = TreadShredUpgradePanel.Show(font);
            yield return null;
            panel.Upgrade(UpgradeTrack.Firepower);
            yield return null;
            var dialog = UnityEngine.Object.FindFirstObjectByType<TreadShredConfirmDialog>();
            Assert.IsNotNull(dialog, "D-017-AC-05: the upgrade asks first");
            Assert.AreEqual((0, 33), (progression.Level(UpgradeTrack.Firepower), progression.Coins), "D-017-AC-05: nothing spent before confirming");
            yield return Audit("upgrade confirmation", findings);

            dialog.Confirm();
            yield return null;
            Assert.AreEqual((1, 13, gems - 9), (progression.Level(UpgradeTrack.Firepower), progression.Coins, wallet.Balance));
            var undo = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.GetComponentInChildren<Text>()?.text == ProgressionLabels.Undo);
            Assert.IsTrue(undo.gameObject.activeInHierarchy, "UNDO UPGRADE shows after an upgrade");
            yield return Audit("upgrades after an upgrade", findings);

            undo.onClick.Invoke();
            yield return null;
            Assert.AreEqual((0, 33, gems), (progression.Level(UpgradeTrack.Firepower), progression.Coins, wallet.Balance));
            Assert.IsFalse(undo.gameObject.activeInHierarchy, "nothing left to undo");
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(t => t.text == "FIREPOWER BACK TO LEVEL 0. 20 COINS AND 9 GEMS RETURNED"), "the screen says what came back");
            yield return Audit("upgrades after an undo", findings);

            UnityEngine.Object.Destroy(panel.gameObject);
            yield return null;
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }
    }
}
