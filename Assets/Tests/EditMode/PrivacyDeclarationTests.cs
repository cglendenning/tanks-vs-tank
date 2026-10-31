using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class PrivacyDeclarationTests
    {
        private static string Root => Directory.GetParent(Application.dataPath).FullName;

        [Test(Description = "D-016-AC-01: the privacy declarations cover every data-collecting SDK in the build; adding an ads, attribution or second analytics SDK fails until they are updated")]
        public void DeclarationsMatchTheSdksInTheBuild()
        {
            var manifest = File.ReadAllText(Path.Combine(Root, "Packages/manifest.json"));
            var answers = File.ReadAllText(Path.Combine(Root, "Store/privacy-answers.md"));
            StringAssert.Contains("com.google.firebase.analytics", manifest, "the declared analytics SDK");
            StringAssert.Contains("Google Analytics for Firebase only", answers);
            foreach (var undeclared in new[] { "com.unity.analytics", "com.unity.ads", "com.unity.services.analytics", "applovin", "appsflyer", "com.google.ads" })
                StringAssert.DoesNotContain(undeclared, manifest.ToLowerInvariant(), undeclared + " would need new privacy declarations");
            StringAssert.Contains("Contains ads: **No**", answers);
            StringAssert.Contains("Data used to track you:** none", answers);
        }
    }
}
