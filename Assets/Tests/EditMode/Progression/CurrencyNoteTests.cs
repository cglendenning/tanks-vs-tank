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
                "COINS: YOU EARN THEM IN EVERY MISSION, WIN OR LOSE. THEY BUY FIREPOWER AND SPEED.",
                "GEMS: BUY THEM OR EARN THEM AT MILESTONES. THEY BUY ARMOR, OVERDRIVE, REVIVES AND WEAPONS.",
                "FIREPOWER AND SPEED USE YOUR COINS FIRST, THEN GEMS FOR THE REST: 1 GEM = 20 COINS.",
                "A BUTTON SHOWING A PRICE BUYS IT AND APPLIES IT IN ONE TAP."
            }, ProgressionLabels.CurrencyNote());
            Assert.AreEqual(20, ProgressionRules.CoinsPerGem, "the rate in the note is the rule's");
            StringAssert.Contains("TreadShredNotePanel.Show(ProgressionLabels.CurrencyNoteTitle, ProgressionLabels.CurrencyNote(), font)",
                File.ReadAllText(Path.Combine(Application.dataPath, "Script", "Game/TreadShredUpgradePanel.cs")));
        }
    }
}
