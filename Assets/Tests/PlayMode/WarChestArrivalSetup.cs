using System;
using System.Linq;
using NUnit.Framework;

namespace TreadShred.Screens.Tests
{
    /// <summary>
    /// D-047-AC-07: the free War Chest arrives at random, so every screen test runs with arrivals off and sees the same
    /// result screens each time; WarChestFreeMissionTests turns them on for its own checks.
    /// </summary>
    [SetUpFixture]
    public sealed class WarChestArrivalSetup
    {
        public static void Arrivals(bool enabled) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TreadShredFreeWarChest")).First(t => t != null)
                .GetProperty("ArrivalsEnabled").SetValue(null, enabled);

        [OneTimeSetUp]
        public void ArrivalsOff() => Arrivals(false);
    }
}
