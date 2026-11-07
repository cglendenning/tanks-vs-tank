using System.Linq;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    public sealed class WeaponDrawerTests
    {
        private sealed class MemoryStore : IProgressionStore
        {
            private ProgressionState state = new ProgressionState();
            public ProgressionState Load() => state;
            public void Save(ProgressionState saved) => state = saved;
        }

        private sealed class MemoryWalletStore : IWalletStore
        {
            private WalletState state = new WalletState();
            public WalletState Load() => state;
            public void Save(WalletState saved) => state = saved;
        }

        private static PlayerProgression NewProgression() => new PlayerProgression(new MemoryStore(), new GemWallet(new MemoryWalletStore()));

        private static string Describe(PlayerProgression progression, Weapon native) =>
            string.Join(", ", progression.Drawer(native).Select(e => e.Weapon + ":" + e.State + (e.MissionsLeft > 0 ? "(" + e.MissionsLeft + ")" : "")));

        [Test(Description = "D-043-AC-01: a special weapon becomes rentable once a mission carrying it is played and stays rentable on every later mission, never before; the cannon is never sold")]
        public void UsedWeaponsStayRentable()
        {
            var progression = NewProgression();
            Assert.AreEqual("Cannon:Available", Describe(progression, Weapon.Cannon), "nothing used yet: only the cannon");
            Assert.Throws<System.InvalidOperationException>(() => progression.StartRental(Weapon.ArcShot));
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.Cannon);
            Assert.AreEqual("Cannon:Available, ArcShot:Locked", Describe(progression, Weapon.Cannon));
            Assert.IsTrue(progression.HasUsed(Weapon.ArcShot));
            Assert.IsFalse(progression.HasUsed(Weapon.Cannon));
        }

        [Test(Description = "D-038-AC-02: the drawer lists the cannon, the mission's own weapon, then every weapon used in the order they are introduced; a used weapon shows on missions with their own special weapon too (D-043-AC-02)")]
        public void DrawerListsEveryWeapon()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.Seeker);
            Assert.AreEqual("Cannon:Available, TripleShot:Available, ArcShot:Locked, Seeker:Locked", Describe(progression, Weapon.TripleShot));
            Assert.AreEqual("Cannon:Available, ArcShot:Available, Seeker:Locked", Describe(progression, Weapon.ArcShot), "its own weapon is free there");
            Assert.AreEqual(Weapon.TripleShot, progression.StartingWeapon(Weapon.TripleShot), "a special weapon's mission starts on it");
        }

        [Test(Description = "D-043-AC-04: each weapon has its own rental, it starts with 5 missions won, and renting one that is still running adds 5")]
        public void RentalsArePerWeaponAndStack()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.TripleShot);
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(5, progression.RentalLeft(Weapon.ArcShot));
            Assert.AreEqual(0, progression.RentalLeft(Weapon.TripleShot));
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(10, progression.RentalLeft(Weapon.ArcShot), "re-renting adds 5");
            progression.StartRental(Weapon.TripleShot);
            Assert.AreEqual("Cannon:Available, ArcShot:Rented(10), TripleShot:Rented(5)", Describe(progression, Weapon.Cannon));
        }

        [Test(Description = "D-043-AC-05: a mission won uses one mission of every running rental except the weapon its own map carries; a rental relocks at zero; a cannon-only mission starts on a running rental; rentals come back with a restored save")]
        public void RentalsCountMissionsWon()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.Seeker);
            progression.StartRental(Weapon.ArcShot);
            progression.StartRental(Weapon.Seeker);
            Assert.AreEqual(Weapon.Seeker, progression.StartingWeapon(Weapon.Cannon), "the latest-introduced running rental");
            progression.RentalMissionWon(Weapon.ArcShot);
            Assert.AreEqual((5, 4), (progression.RentalLeft(Weapon.ArcShot), progression.RentalLeft(Weapon.Seeker)), "the arc shot's own map is free");
            for (var won = 0; won < 4; won++)
                progression.RentalMissionWon(Weapon.Cannon);
            Assert.AreEqual((1, 0), (progression.RentalLeft(Weapon.ArcShot), progression.RentalLeft(Weapon.Seeker)));
            Assert.AreEqual("Cannon:Available, ArcShot:Rented(1), Seeker:Locked", Describe(progression, Weapon.Cannon), "the seeker relocked");

            var restored = NewProgression();
            restored.Replace(new ProgressionState { usedWeapons = WeaponRental.Bit(Weapon.ArcShot), rentals = new[] { 0, 3, 0, 0 } });
            Assert.AreEqual(3, restored.RentalLeft(Weapon.ArcShot));
            Assert.AreEqual(Weapon.ArcShot, restored.StartingWeapon(Weapon.Cannon));
        }

        [Test(Description = "D-043-AC-06: a running rental shows a bar of the missions left out of a full rental and <n> LEFT")]
        public void RentalCountdownLabels()
        {
            Assert.AreEqual("3 LEFT", WeaponRental.Left(3));
            Assert.AreEqual(0.6f, WeaponRental.BarFill(3), 1e-5f);
            Assert.AreEqual(1f, WeaponRental.BarFill(10), "a stacked rental shows full");
            Assert.AreEqual(0f, WeaponRental.BarFill(0));
        }

        [Test(Description = "D-043-AC-03: the offer costs R-WEAPON-RENTAL's 100 gems, titled <WEAPON> FOR 5 MISSIONS, with LOCKED and NOT NOW; short of gems the 100-gem pack covers it")]
        public void OfferWordingAndPrice()
        {
            Assert.AreEqual((100, 5, 5), (WeaponRental.CostGems, WeaponRental.MissionsWon, WeaponRental.RepurchaseAdds));
            Assert.AreEqual("ARC SHOT FOR 5 MISSIONS", WeaponRental.OfferTitle(Weapon.ArcShot));
            Assert.AreEqual(("LOCKED", "NOT NOW"), (WeaponRental.LockedLabel, WeaponRental.Decline));
            Assert.AreEqual(100, GemCatalog.Packs[0].Gems);
        }
    }
}
