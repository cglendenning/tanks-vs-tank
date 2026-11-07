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

        [Test(Description = "D-043-AC-04: each weapon has its own rental, a unit of supply (D-045) that lasts 5 missions won on its own; renting again adds a unit, and units take turns in rotation")]
        public void RentalsArePerWeaponAndStack()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.TripleShot);
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(5, progression.RentalLeft(Weapon.ArcShot));
            Assert.AreEqual(0, progression.RentalLeft(Weapon.TripleShot));
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(10, progression.RentalLeft(Weapon.ArcShot), "re-renting adds a unit: 5 more");
            progression.StartRental(Weapon.TripleShot);
            Assert.AreEqual("Cannon:Available, ArcShot:Rented(15), TripleShot:Rented(10)", Describe(progression, Weapon.Cannon),
                "arc shot, triple shot, arc shot: one unit every 5 missions won");
        }

        [Test(Description = "D-043-AC-05: weapon rentals are supply (D-045): every 5 missions won one unit goes, the arc shot's before the seeker's; a rental relocks at zero; a cannon-only mission starts on a running rental; a 1.9.5 save's rental comes back as a unit")]
        public void RentalsCountMissionsWon()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.Seeker);
            progression.StartRental(Weapon.ArcShot);
            progression.StartRental(Weapon.Seeker);
            Assert.AreEqual(Weapon.Seeker, progression.StartingWeapon(Weapon.Cannon), "the latest-introduced running rental");
            Assert.AreEqual((5, 10), (progression.RentalLeft(Weapon.ArcShot), progression.RentalLeft(Weapon.Seeker)), "one unit each, in rotation");
            progression.SupplyMissionWon();
            Assert.AreEqual((4, 9), (progression.RentalLeft(Weapon.ArcShot), progression.RentalLeft(Weapon.Seeker)), "every mission won counts, on any map");
            for (var won = 0; won < 4; won++)
                progression.SupplyMissionWon();
            Assert.AreEqual((0, 5), (progression.RentalLeft(Weapon.ArcShot), progression.RentalLeft(Weapon.Seeker)));
            Assert.AreEqual("Cannon:Available, ArcShot:Locked, Seeker:Rented(5)", Describe(progression, Weapon.Cannon), "the arc shot relocked");

            var restored = NewProgression();
            restored.Replace(new ProgressionState { usedWeapons = WeaponRental.Bit(Weapon.ArcShot), rentals = new[] { 0, 3, 0, 0 } });
            Assert.AreEqual(5, restored.RentalLeft(Weapon.ArcShot), "three missions left in 1.9.5 become one unit");
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

        [Test(Description = "D-043-AC-02: a used weapon that is neither rented nor the mission's own shows LOCKED with its price in the drawer, on every mission, including one with its own special weapon")]
        public void UsedWeaponsShowLockedWithTheirPrice()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            foreach (var native in new[] { Weapon.Cannon, Weapon.TripleShot, Weapon.Seeker })
                Assert.AreEqual(DrawerState.Locked, progression.Drawer(native).Single(e => e.Weapon == Weapon.ArcShot).State, native.ToString());
            var drawer = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Script/Game/TreadShredWeaponToggle.cs"));
            StringAssert.Contains("return WeaponRental.LockedLabel + \" · \" + price;", drawer, "LOCKED in text with the price, no icon");
            StringAssert.DoesNotContain("Sprite", drawer, "no lock icon");
        }

        [Test(Description = "D-043-AC-07: the weapon button, the drawer with locked and rented weapons, and the offer are screen-audited on every shape, and every rental offer and rental is logged")]
        public void DrawerAndOfferAreAuditedAndLogged()
        {
            var screen = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Tests/PlayMode/WeaponDrawerScreenTests.cs"));
            foreach (var audited in new[] { "WEAPON BUTTON", "WEAPON DRAWER", "ARC SHOT RENTAL OFFER FROM THE DRAWER", "ARC SHOT RENTED" })
                StringAssert.Contains("Audit(\"" + audited + "\"", screen);
            var offer = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Script/Game/TreadShredRentalOffer.cs"));
            StringAssert.Contains("GameEvents.OfferShown(OfferName, store.Wallet.Balance)", offer);
            StringAssert.Contains("GameEvents.OfferAccepted(OfferName, WeaponRental.CostGems, packBought)", offer);
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
