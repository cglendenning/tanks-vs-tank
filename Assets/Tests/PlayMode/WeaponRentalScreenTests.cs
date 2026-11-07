using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TreadShred.Economy;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    // D-043 in real missions: the LOCKED arc shot on a cannon-only mission, the offer it opens, a rental through five
    // missions won (the arc shot's own missions not counting), and the relock.
    public sealed class WeaponRentalScreenTests
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string WalletKey = "TreadShredGemWallet";

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static object Singleton(string type) => GameType(type).GetMethod("Ensure").Invoke(null, null);

        private static PlayerProgression Progression =>
            (PlayerProgression)Singleton("TreadShredProgress").GetType().GetProperty("Progression").GetValue(Singleton("TreadShredProgress"));

        private static GemWallet Wallet => (GemWallet)Singleton("TreadShredStore").GetType().GetProperty("Wallet").GetValue(Singleton("TreadShredStore"));

        private static List<string> Texts() => UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();

        private static Button ButtonStarting(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy
                && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static IEnumerator Audit(string screen, List<Finding> findings)
        {
            foreach (var shape in ScreenAudit.Shapes)
            {
                yield return ScreenAudit.Present(shape);
                findings.AddRange(ScreenAudit.Inspect(shape, screen));
            }
        }

        private static IEnumerator Load(int map)
        {
            Singleton("TreadShredProgress").GetType().GetMethod("SelectTier").Invoke(Singleton("TreadShredProgress"), new object[] { DifficultyTier.Recruit });
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv" + map);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
        }

        private static IEnumerator Win()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType(GameType("ManagerScore"));
            manager.GetType().GetField("CountEmnemy", Members).SetValue(manager, 1);
            manager.GetType().GetMethod("Victory", Members).Invoke(manager, null);
            yield return new WaitForSecondsRealtime(3.5f);
        }

        private static Weapon InHand()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            return (Weapon)player.GetType().GetProperty("WeaponInHand").GetValue(player);
        }

        private static ProgressionState Used(int rentalLeft = 0) => new ProgressionState
        {
            clearedMissions = new[] { (1L << 40) - 1, 0L, 0L },
            usedWeapons = WeaponRental.Bit(Weapon.ArcShot),
            rentedWeapon = rentalLeft > 0 ? (int)Weapon.ArcShot : 0,
            rentalMissionsLeft = rentalLeft
        };

        [UnityTest(Description = "D-043-AC-02: a cannon-only mission shows CANNON beside the used arc shot marked LOCKED; its tap pauses on the ARC SHOT FOR 5 MISSIONS offer (D-043-AC-03), NOT NOW resumes, and renting pays 100 gems and puts it in hand (D-043-AC-04), on every screen shape (D-043-AC-06)")]
        public IEnumerator LockedArcShotRentsInOneTap()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true });
                Progression.Replace(Used());
                yield return Load(2);
                var texts = Texts();
                CollectionAssert.IsSupersetOf(texts, new[] { "CANNON", "ARC SHOT", "LOCKED" });
                Assert.AreEqual(Weapon.Cannon, InHand());
                yield return Audit("MISSION WITH THE ARC SHOT LOCKED", findings);

                ButtonStarting("ARC SHOT").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("TreadShredSales"), "the offer opens");
                Assert.AreEqual(0f, Time.timeScale, "and the game is paused behind it");
                CollectionAssert.Contains(Texts(), "ARC SHOT FOR 5 MISSIONS");
                Assert.IsNotNull(ButtonStarting("RENT FOR 100 GEMS"));
                yield return Audit("ARC SHOT RENTAL OFFER", findings);

                ButtonStarting("NOT NOW").onClick.Invoke();
                yield return null;
                Assert.AreEqual(1f, Time.timeScale, "NOT NOW resumes the mission");
                CollectionAssert.Contains(Texts(), "LOCKED");
                Assert.AreEqual(200, Wallet.Balance);

                ButtonStarting("ARC SHOT").onClick.Invoke();
                yield return null;
                ButtonStarting("RENT FOR 100 GEMS").onClick.Invoke();
                yield return null;
                Assert.AreEqual(100, Wallet.Balance, "100 gems paid");
                Assert.AreEqual(1f, Time.timeScale, "the mission resumes");
                Assert.AreEqual(Weapon.ArcShot, InHand(), "the arc shot is in hand at once");
                Assert.AreEqual((Weapon?)Weapon.ArcShot, Progression.RentedWeapon);
                CollectionAssert.DoesNotContain(Texts(), "LOCKED");
                CollectionAssert.Contains(Texts(), WeaponRules.SwitchHint, "the toggle switches as on the weapon's own missions");
                yield return Audit("MISSION WITH THE ARC SHOT RENTED", findings);
            }
            finally
            {
                Time.timeScale = 1f;
                var open = GameObject.Find("TreadShredSales");
                if (open != null)
                    UnityEngine.Object.Destroy(open);
                Progression.Replace(new ProgressionState());
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-043-AC-05: a rental counts only cannon-only missions won, starting with the mission it was rented in, not the arc shot's own missions, and relocks after the fifth")]
        public IEnumerator RentalCountsCannonMissionsWon()
        {
            try
            {
                Progression.Replace(Used(WeaponRental.MissionsWon));
                yield return Load(2);
                Assert.AreEqual(Weapon.ArcShot, InHand(), "a running rental starts the mission on the arc shot");
                yield return Win();
                Assert.AreEqual(WeaponRental.MissionsWon - 1, Progression.RentalMissionsLeft);

                yield return Load(16);
                CollectionAssert.DoesNotContain(Texts(), "LOCKED", "the arc shot's own mission keeps its own toggle");
                yield return Win();
                Assert.AreEqual(WeaponRental.MissionsWon - 1, Progression.RentalMissionsLeft, "its own missions do not count");

                Progression.Replace(Used(1));
                yield return Load(3);
                yield return Win();
                Assert.IsNull(Progression.RentedWeapon, "the fifth win ends the rental");
                yield return Load(4);
                CollectionAssert.Contains(Texts(), "LOCKED", "and the arc shot is locked again");
                Assert.AreEqual(Weapon.Cannon, InHand());
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
            }
        }
    }
}
