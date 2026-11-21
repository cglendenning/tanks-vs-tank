using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TreadShred.Screens.Tests
{
    // D-027-AC-02 in a real mission: an enemy tank destroyed by the player is wrecked, not just removed.
    public sealed class TankDestructionTests
    {
        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        private static Component[] Debris() =>
            UnityEngine.Object.FindObjectsByType(GameType("TreadShredDebris"), FindObjectsSortMode.None).Cast<Component>().ToArray();

        [UnityTest(Description = "D-027-AC-02: a destroyed enemy tank swells a fireball, throws its charred hull and parts into the air, scatters armor plates, track links and red-hot chunks with a fire flash and smoke, and all of it clears within seven seconds")]
        public IEnumerator DestroyedTankIsWrecked()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lv1");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                yield return new WaitForSecondsRealtime(0.25f);
            yield return new WaitForSeconds(0.5f);

            // Mission 1's enemies die as the player's shells kill them (Bullet and its kin): wrecked, then removed.
            var enemy = new[] { "MoveEmnemy", "EmnemyRed" }
                .SelectMany(t => UnityEngine.Object.FindObjectsByType(GameType(t), FindObjectsSortMode.None))
                .Cast<Component>().First().gameObject;
            var helpers = new[] { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
            var ground = enemy.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.sharedMesh != null && !helpers.Contains(f.sharedMesh.name))
                .Min(f => f.GetComponent<Renderer>().bounds.min.y);
            // The tank explosion the kill paths pass (the shells' PrefabExplosionTank, EmnemyRed.ExTank).
            GameObject explosion = null;
#if UNITY_EDITOR
            explosion = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/partical/ExplosionTank.prefab");
#endif
            Assert.IsNotNull(explosion, "the tank explosion prefab loads");
            var onScreen = Camera.main.WorldToViewportPoint(enemy.transform.position);
            Debug.Log("[Destruction] wrecked " + enemy.name + " at viewport " + onScreen);
            GameType("TreadShredImpact").GetMethod("Kill").Invoke(null, new object[] { enemy.transform.position, enemy, explosion });
            UnityEngine.Object.Destroy(enemy);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return new WaitForSeconds(0.15f);
            BattlefieldCapture.Screenshot(Camera.main, "destruction-0.15s");
            Assert.IsNotNull(GameObject.Find("Tread Shred Puff"), "a fireball swells");
            yield return new WaitForSeconds(0.15f);

            BattlefieldCapture.Screenshot(Camera.main, "destruction-0.3s");
            var pieces = Debris();
            Assert.GreaterOrEqual(pieces.Length, 22, "the hull, its parts and at least 22 plates, links and chunks");
            Assert.IsTrue(pieces.Any(p => p.name.EndsWith(" wreck")), "the tank's own hull and parts are among the wreck");
            Assert.IsTrue(pieces.Any(p => p.transform.position.y > ground + 1f), "parts are thrown into the air");
            var flash = GameObject.Find("Tread Shred Fire Flash");
            Assert.IsNotNull(flash, "a fire flash lights the ground");
            Assert.IsNotNull(flash.GetComponent<Light>());

            yield return new WaitForSeconds(0.5f);
            BattlefieldCapture.Screenshot(Camera.main, "destruction-0.8s");
            Assert.IsNotNull(GameObject.Find("Tread Shred Scorch"), "a scorch mark marks the spot");

            yield return new WaitForSeconds(1.5f);
            BattlefieldCapture.Screenshot(Camera.main, "destruction-2.3s");
            yield return new WaitForSeconds(4f);
            Assert.IsEmpty(Debris(), "every piece clears");
            Assert.IsNull(GameObject.Find("Tread Shred Fire Flash"));
            Assert.IsNull(GameObject.Find("Tread Shred Scorch"));
            Assert.IsNull(GameObject.Find("Tread Shred Puff"), "the fireballs and smoke clear");
        }
    }
}
