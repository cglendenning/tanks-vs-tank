using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TreadShred.Screens.Tests
{
    // D-027-AC-02: the wreck looks burnt by extremely hot incendiaries (owner direction 2026-11-26), and its turret is
    // blown off a different way each time.
    public sealed class TankWreckBurnTests
    {
        private static readonly Color Paint = new Color(0.8f, 0.2f, 0.7f);
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        // A two-part tank in one paint: a hull and a turret on it, with meshes that are not Unity primitives.
        private static GameObject Tank()
        {
            var tank = new GameObject("Test Tank");
            var material = new Material(Shader.Find("Standard")) { color = Paint };
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreach (var (name, scale, height) in new[] { ("Hull", new Vector3(3f, 1f, 4f), 0.5f), ("Turret", new Vector3(1.5f, 0.6f, 1.5f), 1.3f) })
            {
                var part = new GameObject(name);
                part.transform.SetParent(tank.transform, false);
                part.transform.localPosition = Vector3.up * height;
                part.transform.localScale = scale;
                var mesh = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
                mesh.name = name.ToLowerInvariant();
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            Object.Destroy(cube);
            return tank;
        }

        private static Material[] Armor() =>
            Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.name.EndsWith(" fragment") || r.name.EndsWith(" wreck"))
                .SelectMany(r => r.materials).ToArray();

        [UnityTest(Description = "D-027-AC-02: blown-off armor is white-hot, then smolders with an ember glow while keeping a hint of its paint, never charred black")]
        public IEnumerator BlownOffArmorSmolders()
        {
            var tank = Tank();
            GameType("TreadShredWreck").GetMethod("Spawn").Invoke(null, new object[] { tank, null });
            Object.Destroy(tank);

            yield return new WaitForSeconds(0.1f);
            var hot = Armor();
            Assert.GreaterOrEqual(hot.Length, 5, "the hull's fragments and the turret fly");
            Assert.IsTrue(hot.All(m => m.IsKeywordEnabled("_EMISSION")), "the armor gives off its own heat");
            Assert.IsTrue(hot.All(m => m.GetColor(EmissionColor).maxColorComponent > 1f), "white-hot in the blast");

            yield return new WaitForSeconds(1.4f);
            var smoldering = Armor();
            Assert.IsNotEmpty(smoldering);
            foreach (var material in smoldering)
            {
                var glow = material.GetColor(EmissionColor);
                Assert.Greater(glow.r, 0.03f, "still glowing");
                Assert.Greater(glow.r, glow.g * 2f, "an ember-orange glow, not white");
                Assert.Greater(glow.g, glow.b, "an ember-orange glow, not white");
                var surface = material.color;
                Assert.Less(surface.r, Paint.r, "scorched by the blast");
                Assert.Greater(surface.r, Paint.r * 0.45f, "not charred black");
                Assert.Greater(surface.r - surface.g, (Paint.r - Paint.g) * 0.4f, "keeps a hint of its paint");
            }
        }

        [Test(Description = "D-027-AC-02: a turret is blown off a different way each time: in any direction, spinning flat or tumbling end over end, high or low, at once or a split second later")]
        public void TurretBlowsOffDifferentlyEachTime()
        {
            Random.InitState(20261126);
            var blownOff = GameType("TreadShredWreck").GetMethod("BlownOff", BindingFlags.NonPublic | BindingFlags.Static);
            var launches = Enumerable.Range(0, 60).Select(_ =>
            {
                var launch = blownOff.Invoke(null, null);
                var fields = launch.GetType();
                return (Velocity: (Vector3)fields.GetField("Item1").GetValue(launch), Spin: (Vector3)fields.GetField("Item2").GetValue(launch),
                    Delay: (float)fields.GetField("Item3").GetValue(launch));
            }).ToArray();

            var quadrants = launches.Select(l => Mathf.FloorToInt((Mathf.Atan2(l.Velocity.z, l.Velocity.x) + Mathf.PI) / (Mathf.PI / 2f)) % 4).Distinct().Count();
            Assert.AreEqual(4, quadrants, "thrown in every direction");
            Assert.IsTrue(launches.Any(l => Mathf.Abs(l.Spin.normalized.y) > 0.9f), "sometimes spinning flat");
            Assert.IsTrue(launches.Any(l => Mathf.Abs(l.Spin.normalized.y) < 0.1f), "sometimes tumbling end over end");
            Assert.Greater(launches.Max(l => l.Velocity.y) - launches.Min(l => l.Velocity.y), 5f, "sometimes high, sometimes low");
            Assert.Greater(launches.Max(l => new Vector3(l.Velocity.x, 0f, l.Velocity.z).magnitude), 4f, "sometimes thrown wide");
            Assert.IsTrue(launches.Any(l => l.Delay > 0f) && launches.Any(l => l.Delay == 0f), "at once or a split second later");
            Assert.IsTrue(launches.All(l => l.Spin.magnitude >= 400f && l.Velocity.y >= 7f), "always violent");
        }
    }
}
