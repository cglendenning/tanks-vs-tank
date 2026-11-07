using System.Collections.Generic;
using NUnit.Framework;
using TreadShred.Economy;

namespace TreadShred.Progression.Tests
{
    public sealed class WeaponRentalTests
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

        private static PlayerProgression NewProgression() =>
            new PlayerProgression(new MemoryStore(), new GemWallet(new MemoryWalletStore()));

        [Test(Description = "D-043-AC-01: a special weapon becomes rentable once a mission carrying it is played, never before; the cannon is never offered")]
        public void RentableOnceUsed()
        {
            var progression = NewProgression();
            Assert.IsNull(progression.OfferedWeapon);
            progression.WeaponUsed(Weapon.Cannon);
            Assert.IsNull(progression.OfferedWeapon);
            progression.WeaponUsed(Weapon.ArcShot);
            Assert.AreEqual(Weapon.ArcShot, progression.OfferedWeapon);
            Assert.Throws<System.InvalidOperationException>(() => progression.StartRental(Weapon.Seeker), "never used");
        }

        [Test(Description = "D-043-AC-02: the toggle offers the most recently introduced weapon used, or the one rented while a rental runs")]
        public void OffersTheLatestOrTheRented()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.WeaponUsed(Weapon.TripleShot);
            Assert.AreEqual(Weapon.TripleShot, progression.OfferedWeapon);
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(Weapon.ArcShot, progression.OfferedWeapon);
            Assert.AreEqual(Weapon.ArcShot, progression.RentedWeapon);
        }

        [Test(Description = "D-043-AC-05: a rental lasts five missions won, then the weapon relocks and can be rented again")]
        public void RentalCountsMissionsWon()
        {
            var progression = NewProgression();
            progression.WeaponUsed(Weapon.ArcShot);
            progression.StartRental(Weapon.ArcShot);
            Assert.AreEqual(5, progression.RentalMissionsLeft);
            for (var won = 1; won < 5; won++)
                progression.RentalMissionWon();
            Assert.AreEqual(1, progression.RentalMissionsLeft);
            Assert.AreEqual(Weapon.ArcShot, progression.RentedWeapon);
            progression.RentalMissionWon();
            Assert.IsNull(progression.RentedWeapon, "relocked after the fifth");
            Assert.AreEqual(Weapon.ArcShot, progression.OfferedWeapon, "and offered again, locked");
            progression.RentalMissionWon();
            Assert.AreEqual(0, progression.RentalMissionsLeft, "no rental, nothing to count");
        }

        [Test(Description = "D-043-AC-05: a restored save (cloud or reinstall) keeps the weapons used and the running rental")]
        public void RestoredSaveKeepsTheRental()
        {
            var progression = NewProgression();
            progression.Replace(new ProgressionState
            {
                usedWeapons = WeaponRental.Bit(Weapon.ArcShot) | WeaponRental.Bit(Weapon.TripleShot),
                rentedWeapon = (int)Weapon.ArcShot,
                rentalMissionsLeft = 3
            });
            Assert.AreEqual(Weapon.ArcShot, progression.RentedWeapon);
            Assert.AreEqual(3, progression.RentalMissionsLeft);
            progression.Replace(new ProgressionState { usedWeapons = WeaponRental.Bit(Weapon.TripleShot) });
            Assert.IsNull(progression.RentedWeapon);
            Assert.AreEqual(Weapon.TripleShot, progression.OfferedWeapon);
        }

        [Test(Description = "D-043-AC-03: the offer costs R-WEAPON-RENTAL's 100 gems for 5 missions, titled <WEAPON> FOR 5 MISSIONS, with LOCKED and NOT NOW, on the weapon's own sales screen and clip")]
        public void OfferWordingAndPrice()
        {
            Assert.AreEqual((100, 5), (WeaponRental.CostGems, WeaponRental.MissionsWon));
            Assert.AreEqual("ARC SHOT FOR 5 MISSIONS", WeaponRental.OfferTitle(Weapon.ArcShot));
            Assert.AreEqual("SEEKER FOR 5 MISSIONS", WeaponRental.OfferTitle(Weapon.Seeker));
            Assert.AreEqual(("LOCKED", "NOT NOW"), (WeaponRental.LockedLabel, WeaponRental.Decline));
            Assert.AreEqual(100, GemCatalog.Packs[0].Gems, "short of gems, one tap buys the 100-gem pack ($0.99)");
            var expected = new Dictionary<Weapon, string> { { Weapon.ArcShot, "arc-shot" }, { Weapon.TripleShot, "triple-shot" }, { Weapon.Seeker, "seeker" } };
            foreach (var entry in expected)
            {
                var item = SalesPitches.ForWeapon(entry.Key);
                Assert.AreEqual(entry.Value, SalesPitches.ClipName(item));
                Assert.AreEqual(WeaponRental.OfferTitle(entry.Key), SalesPitches.For(item).Title);
                CollectionAssert.Contains(SalesPitches.For(item).Points, "YOURS FOR 5 MISSIONS WON");
                CollectionAssert.Contains(SalesPitches.All, item);
            }
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SalesPitches.ForWeapon(Weapon.Cannon));
        }
    }
}
