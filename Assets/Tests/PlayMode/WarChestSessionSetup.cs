using System;
using System.Linq;
using NUnit.Framework;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// D-047: every screen test runs as if the War Chest had already been offered this session, so its interstitial never
    /// covers the failure screens they check; WarChestTests reopens it for its own checks.
    /// </summary>
    [SetUpFixture]
    public sealed class WarChestSessionSetup
    {
        public static void Offered(bool offered) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TreadShredWarChestOffer")).First(t => t != null)
                .GetProperty("ShownThisSession").SetValue(null, offered);

        [OneTimeSetUp]
        public void AlreadyOffered() => Offered(true);
    }
}
