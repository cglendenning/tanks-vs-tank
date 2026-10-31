using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class CurrencyNoteTests
    {
        [Test(Description = "D-018-AC-04: the upgrade screen's COINS AND GEMS note says where each currency comes from, what it buys, and the gem-to-coin rate")]
        public void NoteExplainsBothCurrencies()
        {
            Assert.AreEqual("COINS AND GEMS", ProgressionLabels.CurrencyNoteTitle);
            CollectionAssert.AreEqual(new[]
            {
                "COINS: EARNED IN EVERY MISSION, WIN OR LOSE. THEY BUY PERMANENT UPGRADES.",
                "GEMS: BOUGHT IN THE STORE OR EARNED AT MILESTONES. THEY BUY BOOSTS AND REVIVES.",
                "SHORT ON COINS? GEMS COVER THE GAP AT 1 GEM FOR 20 COINS."
            }, ProgressionLabels.CurrencyNote());
            Assert.AreEqual(20, ProgressionRules.CoinsPerGem, "the rate in the note is the rule's");
            StringAssert.Contains("TreadShredNotePanel.Show(ProgressionLabels.CurrencyNoteTitle, ProgressionLabels.CurrencyNote(), font)",
                File.ReadAllText(Path.Combine(Application.dataPath, "Script", "Game/TreadShredUpgradePanel.cs")));
        }
    }
}
