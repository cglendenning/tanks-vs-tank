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
    // D-038 and D-043 in real missions: the weapon button, the drawer it slides out, renting from it, and the countdown.
    public sealed class WeaponDrawerScreenTests
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

        private static Button Under(string root, string label)
        {
            var parent = GameObject.Find(root);
            return parent == null ? null : parent.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => (b.GetComponentInChildren<Text>()?.text ?? string.Empty) == label);
        }

        private static Button ButtonStarting(string label) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy
                && (b.GetComponentInChildren<Text>()?.text ?? string.Empty).StartsWith(label, StringComparison.Ordinal));

        private static Weapon InHand()
        {
            var player = UnityEngine.Object.FindFirstObjectByType(GameType("PlayerMoverment"));
            return (Weapon)player.GetType().GetProperty("WeaponInHand").GetValue(player);
        }

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

        private static ProgressionState Used(Weapon weapon, int rentalLeft = 0)
        {
            var rentals = new int[4];
            rentals[(int)weapon] = rentalLeft;
            return new ProgressionState { clearedMissions = new[] { (1L << 40) - 1, 0L, 0L }, usedWeapons = WeaponRental.Bit(weapon), rentals = rentals };
        }

        [UnityTest(Description = "D-038-AC-02: on the triple shot's map with the arc shot used, the weapon button names the triple shot; tapping it pauses on a drawer of CANNON, TRIPLE SHOT (in hand) and ARC SHOT (LOCKED · 100 GEMS), and choosing the cannon puts it in hand and resumes (D-038-AC-03), on every screen shape (D-038-AC-05)")]
        public IEnumerator DrawerChoosesAWeapon()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true });
                Progression.Replace(Used(Weapon.ArcShot));
                yield return Load(26);
                Assert.AreEqual(Weapon.TripleShot, InHand(), "the triple shot's mission starts on it");
                CollectionAssert.Contains(Texts(), WeaponRules.SwitchHint);
                yield return Audit("WEAPON BUTTON", findings);

                Under("TreadShredWeaponToggle", "TRIPLE SHOT").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.AreEqual(0f, Time.timeScale, "the drawer pauses the game");
                CollectionAssert.IsSupersetOf(Texts(), new[] { "CANNON", "TRIPLE SHOT", "ARC SHOT", "IN HAND", "LOCKED · 100 GEMS" });
                yield return Audit("WEAPON DRAWER", findings);

                Under("TreadShredWeaponDrawer", "CANNON").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.AreEqual(1f, Time.timeScale, "choosing resumes the mission");
                Assert.AreEqual(Weapon.Cannon, InHand());
                Assert.IsNotNull(Under("TreadShredWeaponToggle", "CANNON"), "the button names the weapon in hand");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
                Wallet.Replace(JsonUtility.FromJson<WalletState>(saved));
            }
            Assert.IsEmpty(findings, ScreenAudit.Report(findings));
        }

        [UnityTest(Description = "D-043-AC-04: renting the arc shot from the drawer on the triple shot's map pays 100 gems, puts it in hand with 5 LEFT (D-043-AC-06) and resumes, and the win there uses one of its missions (D-043-AC-05), on every screen shape (D-043-AC-07)")]
        public IEnumerator RentFromTheDrawer()
        {
            var findings = new List<Finding>();
            var saved = PlayerPrefs.GetString(WalletKey, JsonUtility.ToJson(new WalletState()));
            try
            {
                Wallet.Replace(new WalletState { balance = 200, starterGranted = true });
                Progression.Replace(Used(Weapon.ArcShot));
                yield return Load(26);
                Under("TreadShredWeaponToggle", "TRIPLE SHOT").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Under("TreadShredWeaponDrawer", "ARC SHOT").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(GameObject.Find("TreadShredSales"), "a locked weapon opens its offer");
                CollectionAssert.Contains(Texts(), "ARC SHOT FOR 5 MISSIONS");
                yield return Audit("ARC SHOT RENTAL OFFER FROM THE DRAWER", findings);
                ButtonStarting("RENT FOR 100 GEMS").onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.AreEqual(100, Wallet.Balance);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.AreEqual(Weapon.ArcShot, InHand(), "in hand at once");
                CollectionAssert.Contains(Texts(), "5 LEFT", "the countdown under the button");
                yield return Audit("ARC SHOT RENTED", findings);
                yield return Win();
                Assert.AreEqual(4, Progression.RentalLeft(Weapon.ArcShot), "the triple shot's map uses an arc-shot mission");
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

        [UnityTest(Description = "D-038-AC-01: a cannon-only mission shows no weapon control before any special weapon is used; with a rental running it starts on the rented weapon with its missions left")]
        public IEnumerator CannonMissionsBeforeAndAfter()
        {
            try
            {
                Progression.Replace(new ProgressionState { clearedMissions = new[] { (1L << 40) - 1, 0L, 0L } });
                yield return Load(2);
                Assert.IsNull(GameObject.Find("TreadShredWeaponToggle"), "nothing to choose");
                Progression.Replace(Used(Weapon.Seeker, 3));
                yield return Load(2);
                Assert.AreEqual(Weapon.Seeker, InHand(), "a cannon-only mission starts on the running rental");
                Assert.IsNotNull(Under("TreadShredWeaponToggle", "SEEKER"));
                CollectionAssert.Contains(Texts(), "3 LEFT");
            }
            finally
            {
                Time.timeScale = 1f;
                Progression.Replace(new ProgressionState());
            }
        }
    }
}
