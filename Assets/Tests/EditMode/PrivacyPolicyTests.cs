using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Economy.Tests
{
    public sealed class PrivacyPolicyTests
    {
        private static string Root => Directory.GetParent(Application.dataPath).FullName;

        [Test(Description = "D-016-AC-01: the published privacy policy names every company the declarations cover, says there are no ads and no tracking on iOS, and the support page gives a contact and links the policy")]
        public void PolicyMatchesTheDeclarations()
        {
            var policy = File.ReadAllText(Path.Combine(Root, "Store/privacy-policy.md"));
            var support = File.ReadAllText(Path.Combine(Root, "Store/support.md"));
            foreach (var named in new[] { "Google Analytics for Firebase", "Unity In-App Purchasing", "Tenjin", "AppLovin", "SKAdNetwork", "Game Center", "iCloud", "Google Play Games" })
                StringAssert.Contains(named, policy, "the policy names " + named);
            StringAssert.Contains("Tread Shred has no ads", policy);
            StringAssert.Contains("On iPhone and iPad, Tread Shred does not track you", policy);
            StringAssert.Contains("Ad campaign measurement (Android only)", policy);
            StringAssert.IsMatch(@"[\w.]+@[\w.]+\.\w+", support, "the support page gives an email contact");
            StringAssert.Contains("(privacy-policy.md)", support);
        }
    }
}
