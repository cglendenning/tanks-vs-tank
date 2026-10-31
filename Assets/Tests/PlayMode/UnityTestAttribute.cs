using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// UnityTest with a Description property, so play-mode tests name the directive they enforce
    /// the same way as every other test.
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
