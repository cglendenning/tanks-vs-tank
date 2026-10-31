using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// UnityTest with a Description, so play-mode tests name their directive the same way as
    /// every other test: [UnityTest(Description = "D-###-AC-##: ...")].
    /// </summary>
    public sealed class UnityTestAttribute : UnityEngine.TestTools.UnityTestAttribute, IApplyToTest
    {
        public string Description { get; set; }

        void IApplyToTest.ApplyToTest(Test test)
        {
            ApplyToTest(test);
            if (!string.IsNullOrEmpty(Description))
                test.Properties.Set(PropertyNames.Description, Description);
        }
    }
}
