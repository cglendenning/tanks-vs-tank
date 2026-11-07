using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TreadShred.Progression.Tests
{
    public sealed class WeaponAndGroundTests
    {
        private static string Script(string relative) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Script", relative));

        [Test(Description = "D-038-AC-01: the old unlabeled weapon icon never shows, and a mission with only the cannon gets no weapon control")]
        public void CannonMissionsShowNoWeaponControl()
        {
            Assert.IsFalse(WeaponRules.CanSwitch(Weapon.Cannon));
            Assert.AreEqual(Weapon.Cannon, WeaponRules.Switch(Weapon.Cannon, Weapon.Cannon), "nothing to switch to");
            var player = Script("Player/PlayerMoverment.cs");
            StringAssert.DoesNotContain("StyteBullet.transform.GetChild(1)", player, "the legacy icons are never switched on");
            StringAssert.Contains("TreadShredWeaponToggle.Attach(this, MissionWeapon,", player);
            StringAssert.Contains("if (TreadShredProgress.Ensure().Progression.Drawer(missionWeapon).Count < 2)\n            return null;", Script("Game/TreadShredWeaponToggle.cs"));
        }

        [Test(Description = "D-038-AC-02: the drawer names the cannon and the special weapons from R-WEAPONS, in the order of the tank's weapon types, the one in hand on the accent-styled button")]
        public void ToggleNamesBothWeapons()
        {
            Assert.AreEqual(new[] { "CANNON", "ARC SHOT", "TRIPLE SHOT", "SEEKER" },
                new[] { Weapon.Cannon, Weapon.ArcShot, Weapon.TripleShot, Weapon.Seeker }.Select(WeaponRules.Name).ToArray());
            Assert.IsTrue(new[] { Weapon.ArcShot, Weapon.TripleShot, Weapon.Seeker }.All(WeaponRules.CanSwitch));
            var player = Script("Player/PlayerMoverment.cs");
            var types = player.Substring(player.IndexOf("public enum PlayerType", System.StringComparison.Ordinal));
            types = types.Substring(0, types.IndexOf('}'));
            Assert.Less(types.IndexOf("banthang", System.StringComparison.Ordinal), types.IndexOf("banvong", System.StringComparison.Ordinal));
            Assert.Less(types.IndexOf("banvong", System.StringComparison.Ordinal), types.IndexOf("bandan3", System.StringComparison.Ordinal));
            Assert.Less(types.IndexOf("bandan3", System.StringComparison.Ordinal), types.IndexOf("bandanduoi", System.StringComparison.Ordinal),
                "Weapon casts from PlayerType, so the orders must match");
            StringAssert.Contains("TreadShredUi.StylePrimary(weaponButton);", Script("Game/TreadShredWeaponToggle.cs"), "the weapon in hand is the accent-styled button");
        }

        [Test(Description = "D-038-AC-03: a tap in the drawer switches weapons, and only the cannon, the mission's weapon or a rented one can be in hand")]
        public void ATapSwitchesWeapons()
        {
            Assert.AreEqual(Weapon.Cannon, WeaponRules.Switch(Weapon.ArcShot, Weapon.ArcShot));
            Assert.AreEqual(Weapon.ArcShot, WeaponRules.Switch(Weapon.ArcShot, Weapon.Cannon));
            Assert.AreEqual(Weapon.Seeker, WeaponRules.Switch(Weapon.Seeker, Weapon.Cannon));
            var player = Script("Player/PlayerMoverment.cs");
            StringAssert.Contains("if (weapon == TreadShred.Progression.Weapon.Cannon || weapon == MissionWeapon\n            || TreadShredProgress.Ensure().Progression.RentalLeft(weapon) > 0)\n            typeplayer = (PlayerType)(int)weapon;", player);
            StringAssert.Contains("if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) == false)", player, "a touch on the HUD never fires");
        }

        [Test(Description = "D-038-AC-04: (TAP TO CHANGE) sits inside the weapon button whenever there is a choice, and the button hides while paused and once the mission ends")]
        public void ToggleHintAndHiding()
        {
            Assert.AreEqual("(TAP TO CHANGE)", WeaponRules.SwitchHint);
            StringAssert.Contains("weaponButton = Cell(root, ButtonWidth, WeaponRules.SwitchHint, Toggle, out _);", Script("Game/TreadShredWeaponToggle.cs"),
                "the instruction sits inside the weapon button");
            var pause = Script("ManagerScore/ManagerButton.cs");
            StringAssert.Contains("TreadShredWeaponToggle.SetVisible(false);", pause);
            StringAssert.Contains("TreadShredWeaponToggle.SetVisible(true);", pause);
            StringAssert.Contains("TreadShredWeaponToggle.Hide();", Script("ManagerScore/ManagerScore.cs"));
        }

        [Test(Description = "D-039-AC-01: missions 1 to 5 use the first ground, 6 to 10 the second and so on to 36 to 40 on the eighth, each a separate texture in the build")]
        public void EveryFiveMissionsGetANewGround()
        {
            Assert.AreEqual(new[] { 1, 1, 2, 2, 3, 4, 5, 6, 7, 8, 8 },
                new[] { 1, 5, 6, 10, 11, 16, 21, 26, 31, 36, 40 }.Select(GroundRules.GroundFor).ToArray());
            Assert.AreEqual(8, Enumerable.Range(1, 40).Select(GroundRules.GroundFor).Distinct().Count());
            var hashes = Enumerable.Range(1, GroundRules.Grounds).Select(n =>
            {
                var path = Path.Combine(Application.dataPath, "Resources", GroundRules.ResourcePath(n) + ".jpg");
                Assert.IsTrue(File.Exists(path), path);
                return System.BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(File.ReadAllBytes(path)));
            }).ToList();
            Assert.AreEqual(GroundRules.Grounds, hashes.Distinct().Count(), "eight different textures");
        }

        [Test(Description = "D-039-AC-02: the ground replaces the arena floor's texture as the mission loads, tiled by R-GROUNDS, and a ground that cannot load leaves the floor and logs an error")]
        public void GroundReplacesOnlyTheFloorTexture()
        {
            Assert.AreEqual((6f, 5f), (GroundRules.TilingX, GroundRules.TilingY));
            var ground = Script("Game/TreadShredGround.cs");
            StringAssert.Contains("GameObject.Find(FloorPath)", ground);
            StringAssert.Contains("material.mainTexture = texture;", ground);
            StringAssert.Contains("new Vector2(GroundRules.TilingX, GroundRules.TilingY)", ground);
            Assert.AreEqual(2, ground.Split(new[] { "Debug.LogError(" }, System.StringSplitOptions.None).Length - 1, "missing floor or texture: logged, floor kept");
            StringAssert.Contains("TreadShredGround.Apply(TreadShredProgress.CurrentMission());", Script("Player/PlayerMoverment.cs"));
        }
    }
}
