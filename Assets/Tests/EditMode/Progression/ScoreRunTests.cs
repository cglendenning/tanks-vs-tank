using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class ScoreRunTests
    {
        [Test(Description = "D-032-AC-01: SCORE builds across won missions from kills plus accuracy and armor bonuses")]
        public void WonMissionsBuildTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            Assert.AreEqual(300, run.Live(3), "100 per kill during a mission");

            var first = run.Complete(won: true, kills: 3, shots: 3, hitsTaken: 0);
            Assert.AreEqual((300, 300, 1000), (first.KillPoints, first.AccuracyBonus, first.ArmorBonus));
            Assert.AreEqual(1600, first.RunScore);

            var second = run.Complete(won: true, kills: 2, shots: 4, hitsTaken: 1);
            Assert.AreEqual((200, 150, 666), (second.KillPoints, second.AccuracyBonus, second.ArmorBonus));
            Assert.AreEqual(1600 + 200 + 150 + 666, second.RunScore);
            Assert.AreEqual(second.RunScore, run.Banked, "the next mission starts from the run so far");
        }

        [Test(Description = "D-032-AC-01: losing the tank ends the run with its kills counted, and SCORE starts again from zero")]
        public void LosingEndsTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            run.Complete(true, 3, 3, 0);
            var lost = run.Complete(won: false, kills: 2, shots: 5, hitsTaken: 3);

            Assert.IsFalse(lost.Won);
            Assert.AreEqual(1600 + 200, lost.RunScore, "the final SCORE that HIGH SCORE is checked against");
            Assert.AreEqual((0, 0), (lost.AccuracyBonus, lost.ArmorBonus), "no bonuses for a lost mission");
            Assert.AreEqual(0, run.Banked);
        }

        [Test(Description = "D-032-AC-01: Armored Redeploy continues the run from where it stood before the lost mission")]
        public void RedeployContinuesTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            run.Complete(true, 3, 3, 0);
            run.Complete(false, 2, 5, 3);
            run.ResumeAfterRedeploy();

            Assert.AreEqual(1600, run.Banked);
            Assert.AreEqual(1600 + 100, run.Live(1));
        }

        [Test(Description = "D-032-AC-01: bonuses stay within their caps and SCORE and HIGH SCORE read plainly")]
        public void BonusesAndLabels()
        {
            Assert.AreEqual(300, ScoreRules.AccuracyBonus(kills: 4, shots: 2), "Overdrive's extra rounds cannot beat 300");
            Assert.AreEqual(0, ScoreRules.AccuracyBonus(0, 0));
            Assert.AreEqual(1000, ScoreRules.ArmorBonus(0));
            Assert.AreEqual(333, ScoreRules.ArmorBonus(2));
            Assert.AreEqual(0, ScoreRules.ArmorBonus(5), "upgraded armor still measures against three hits");

            Assert.AreEqual("SCORE // 3,520", ScoreRules.ScoreLine(3520));
            Assert.AreEqual("HIGH SCORE // 3,520", ScoreRules.HighScoreLine(3520, false));
            Assert.AreEqual("NEW HIGH SCORE // 3,520", ScoreRules.HighScoreLine(3520, true));
            Assert.AreEqual("ACCURACY +300 // ARMOR +1,000",
                ScoreRules.BonusLine(new MissionScore(true, 300, 300, 1000, 1600)));
        }
    }
}
