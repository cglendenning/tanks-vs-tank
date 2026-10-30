using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class ScoreRunTests
    {
        [Test(Description = "D-032-AC-01: SCORE builds across won missions from 100 per kill plus 500 per mission clear")]
        public void WonMissionsBuildTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            Assert.AreEqual(300, run.Live(3, DifficultyTier.Recruit), "100 per kill, shown as it is earned");

            var first = run.Complete(won: true, kills: 3, DifficultyTier.Recruit);
            Assert.AreEqual((300, 500), (first.KillPoints, first.ClearPoints));
            Assert.AreEqual(800, first.RunScore);

            var second = run.Complete(won: true, kills: 2, DifficultyTier.Recruit);
            Assert.AreEqual(1500, second.RunScore);
            Assert.AreEqual(1500, run.Banked, "the next mission starts from the run so far");
        }

        [Test(Description = "D-032-AC-01: losing the tank ends the run with its kills counted, and SCORE starts again from zero")]
        public void LosingEndsTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            run.Complete(true, 3, DifficultyTier.Recruit);
            var lost = run.Complete(won: false, kills: 2, DifficultyTier.Recruit);

            Assert.IsFalse(lost.Won);
            Assert.AreEqual(800 + 200, lost.RunScore, "the final SCORE that HIGH SCORE is checked against");
            Assert.AreEqual(0, lost.ClearPoints, "no clear for a lost mission");
            Assert.AreEqual(0, run.Banked);
        }

        [Test(Description = "D-032-AC-01: Armored Redeploy continues the run from where it stood before the lost mission")]
        public void RedeployContinuesTheRun()
        {
            var run = new ScoreRun();
            run.Start();
            run.Complete(true, 3, DifficultyTier.Recruit);
            run.Complete(false, 2, DifficultyTier.Recruit);
            run.ResumeAfterRedeploy();

            Assert.AreEqual(800, run.Banked);
            Assert.AreEqual(900, run.Live(1, DifficultyTier.Recruit));
        }

        [Test(Description = "D-032-AC-01: Veteran doubles and Elite triples every point, and each point reads plainly where it is earned")]
        public void TierValuesAndLabels()
        {
            Assert.AreEqual((100, 500), (ScoreRules.KillValue(DifficultyTier.Recruit), ScoreRules.ClearValue(DifficultyTier.Recruit)));
            Assert.AreEqual((200, 1000), (ScoreRules.KillValue(DifficultyTier.Veteran), ScoreRules.ClearValue(DifficultyTier.Veteran)));
            Assert.AreEqual((300, 1500), (ScoreRules.KillValue(DifficultyTier.Elite), ScoreRules.ClearValue(DifficultyTier.Elite)));

            Assert.AreEqual("TARGET DESTROYED +100", ScoreRules.KillCallout(100));
            Assert.AreEqual("MISSION CLEAR +500", ScoreRules.ClearCallout(500));
            Assert.AreEqual("THIS MISSION +800 // 3 KILLS +300 // CLEAR +500",
                ScoreRules.MissionLine(new MissionScore(true, 3, 300, 500, 800)));
            Assert.AreEqual("THIS MISSION +600 // 1 KILL +100 // CLEAR +500",
                ScoreRules.MissionLine(new MissionScore(true, 1, 100, 500, 600)));
            Assert.AreEqual("THIS MISSION +200 // 2 KILLS +200", ScoreRules.MissionLine(new MissionScore(false, 2, 200, 0, 1000)));
            Assert.AreEqual("SCORE // 3,520", ScoreRules.ScoreLine(3520));
            Assert.AreEqual("NEW HIGH SCORE // 3,520", ScoreRules.HighScoreLine(3520, true));
        }
    }
}
