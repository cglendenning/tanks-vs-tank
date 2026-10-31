using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class AgentRulesTests
    {
        [Test(Description = "D-002-AC-02: the repository's agent instructions forbid changing the specification or existing tests without explicit owner approval")]
        public void AgentInstructionsStateTheTestImmutabilityRule()
        {
            var rules = File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AGENTS.md"));
            StringAssert.Contains("Never change the specification or any existing test", rules);
            StringAssert.Contains("without explicit approval", rules);
            StringAssert.Contains("Adding new tests is allowed", rules);
        }
    }
}
