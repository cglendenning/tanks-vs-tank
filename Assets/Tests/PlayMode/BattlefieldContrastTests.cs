using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreadShred.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TreadShred.Screens.Tests
{
    // D-039-AC-03 measured, not judged by eye: the first mission of every ground is rendered as played, again with the
    // tanks' bodies painted flat magenta (which casts no shadow), and again with the tanks hidden; the pixels where the
    // last two differ are exactly the tanks, with no holes where a tank happens to match the ground and no shadow
    // counted as tank. Each tank's colour is compared with the ground around it by CIEDE2000 colour difference, which
    // counts hue as the eye does (a magenta or green tank on brown ground stands out even at a similar brightness);
    // the WCAG brightness ratios of its edge and body are reported beside it. The pass mark is the owner-approved look
    // of 2026-11-26 (13.9 at its weakest, 27 typical).
    public sealed class BattlefieldContrastTests
    {
        // CIEDE2000: about 2 is just noticeable side by side; past 10 two colours read as different at a glance.
        private const float MinimumDifference = 12f;
        private const int Width = BattlefieldCapture.Width;
        private const int Height = BattlefieldCapture.Height;
        private const float ChangedPixel = 0.06f;
        private const int SmallestTank = 60;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [UnityTest(Description = "D-039-AC-03: on the first mission of every ground, every tank's colour differs from the ground around it by at least 12 (CIEDE2000), a clearly different colour at a glance")]
        public IEnumerator EveryTankStandsOutOnEveryGround()
        {
            if (!BattlefieldCapture.Available)
                Assert.Ignore("measured on rendered frames: run with a graphics device (scripts/run-unity-tests.sh playmode --screenshots DIR)");
            LogAssert.ignoreFailingMessages = true;
            var report = new List<string>();
            var failures = new List<string>();
            for (var ground = 1; ground <= GroundRules.Grounds; ground++)
            {
                var mission = (ground - 1) * GroundRules.MissionsPerGround + 1;
                Time.timeScale = 1f;
                SceneManager.LoadScene("Lv" + mission);
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                // The countdown holds game time at zero; then the tanks spread out.
                for (var waited = 0f; Time.timeScale == 0f && waited < 10f; waited += 0.25f)
                    yield return new WaitForSecondsRealtime(0.25f);
                yield return new WaitForSeconds(1f);

                var measured = Measure(mission);
                if (measured.Count == 0)
                    failures.Add("mission " + mission + ": no tank was measured");
                foreach (var (tank, difference, tankRgb, groundRgb) in measured)
                {
                    var line = "ground " + ground + " (mission " + mission + ") " + tank + ": difference " + difference.ToString("0.0") +
                               ", " + tankRgb + " on ground " + groundRgb;
                    report.Add(line);
                    if (difference < MinimumDifference)
                        failures.Add(line);
                }
            }
            Debug.Log("[Contrast]\n" + string.Join("\n", report));
            Assert.IsEmpty(failures, "tanks less than " + MinimumDifference + " (CIEDE2000) from their ground:\n" + string.Join("\n", failures));
        }

        [Test(Description = "D-039-AC-03: the battlefield check's colour difference matches the published CIEDE2000 reference values")]
        public void ColourDifferenceMatchesTheReference()
        {
            // Sharma, Wu and Dalal (2005), test data pairs 1, 4, 22 and 25.
            Assert.AreEqual(2.0425f, ColorDifference(new Vector3(50f, 2.6772f, -79.7751f), new Vector3(50f, 0f, -82.7485f)), 0.0005f);
            Assert.AreEqual(1.0000f, ColorDifference(new Vector3(50f, -1.3802f, -84.2814f), new Vector3(50f, 0f, -82.7485f)), 0.0005f);
            Assert.AreEqual(27.1492f, ColorDifference(new Vector3(50f, 2.5f, 0f), new Vector3(73f, 25f, -18f)), 0.0005f);
            Assert.AreEqual(1.2644f, ColorDifference(new Vector3(60.2574f, -34.0099f, 36.2677f), new Vector3(60.4626f, -34.1751f, 39.4387f)), 0.0005f);
        }

        private static List<(string, float, string, string)> Measure(int mission)
        {
            var camera = Camera.main;
            // Project and render at the same shape, whatever the editor's game view is.
            camera.aspect = (float)Width / Height;
            var tanks = new List<(string Name, GameObject Body)>();
            foreach (var type in new[] { "PlayerMoverment", "EmnemyRed", "MoveEmnemy" })
            {
                foreach (var found in UnityEngine.Object.FindObjectsByType(GameType(type), FindObjectsSortMode.None))
                {
                    var body = ((Component)found).gameObject;
                    if (tanks.All(t => t.Body != body))
                        tanks.Add(((type == "PlayerMoverment" ? "player" : body.tag) + " " + body.name, body));
                }
            }
            var played = BattlefieldCapture.Render(camera, "contrast-mission-" + mission);
            // A tank's contact shadow is part of the ground it stands out from, so it stays in every render.
            var renderers = tanks.SelectMany(t => t.Body.GetComponentsInChildren<Renderer>())
                .Where(r => r.name != "Tread Shred Contact Shadow").ToList();
            var bodies = renderers;
            var paint = new Material(Shader.Find("TreadShred/Decal")) { color = Color.magenta };
            var looks = bodies.Select(r => r.sharedMaterials).ToList();
            foreach (var body in bodies)
                body.sharedMaterials = Enumerable.Repeat(paint, body.sharedMaterials.Length).ToArray();
            var flat = BattlefieldCapture.Render(camera);
            for (var i = 0; i < bodies.Count; i++)
                bodies[i].sharedMaterials = looks[i];
            UnityEngine.Object.Destroy(paint);
            var shown = renderers.Select(r => r.enabled).ToList();
            foreach (var renderer in renderers)
                renderer.enabled = false;
            var bare = BattlefieldCapture.Render(camera);
            for (var i = 0; i < renderers.Count; i++)
                renderers[i].enabled = shown[i];

            var tankPixels = new bool[Width * Height];
            for (var i = 0; i < tankPixels.Length; i++)
                tankPixels[i] = Difference(flat[i], bare[i]) > ChangedPixel;

            var results = new List<(string, float, string, string)>();
            foreach (var (name, body) in tanks)
            {
                var rect = ScreenRect(camera, body);
                if (rect.width <= 0f || rect.height <= 0f)
                {
                    Debug.Log("[Contrast] mission " + mission + " skipped " + name + ": off screen");
                    continue;
                }
                var tank = new List<Color>();
                var edge = new List<Color>();
                var around = new List<Color>();
                var margin = Mathf.Max(rect.width, rect.height) * 0.6f;
                for (var y = (int)Mathf.Max(0f, rect.yMin - margin); y < (int)Mathf.Min(Height, rect.yMax + margin); y++)
                {
                    for (var x = (int)Mathf.Max(0f, rect.xMin - margin); x < (int)Mathf.Min(Width, rect.xMax + margin); x++)
                    {
                        var i = y * Width + x;
                        var inside = rect.Contains(new Vector2(x, y));
                        if (inside && tankPixels[i])
                        {
                            tank.Add(played[i]);
                            if (OnEdge(tankPixels, x, y))
                                edge.Add(played[i]);
                        }
                        else if (!inside && !tankPixels[i])
                            around.Add(bare[i]);
                    }
                }
                if (tank.Count < SmallestTank || edge.Count == 0 || around.Count == 0)
                {
                    Debug.Log("[Contrast] mission " + mission + " skipped " + name + ": box " + rect + ", tank pixels " + tank.Count +
                              ", edge " + edge.Count + ", ground around " + around.Count + ", changed pixels on screen " +
                              tankPixels.Count(changed => changed));
                    continue;
                }
                var groundLuminance = Luminance(Average(around));
                // The typical edge pixel: antialiasing blends the outermost pixels with the ground, which an average
                // would count against the silhouette.
                var edgeLuminance = edge.Select(Luminance).OrderBy(l => l).ElementAt(edge.Count / 2);
                results.Add((name, ColorDifference(Lab(Average(tank)), Lab(Average(around))),
                    "edge " + Contrast(edgeLuminance, groundLuminance).ToString("0.00") + ":1, edge " + Describe(Average(edge)) +
                    ", body " + Describe(Average(tank)) + " (" + Contrast(Luminance(Average(tank)), groundLuminance).ToString("0.00") + ":1)",
                    Describe(Average(around))));
            }
            camera.ResetAspect();
            return results;
        }

        private static Rect ScreenRect(Camera camera, GameObject body)
        {
            // The model's meshes only: Unity primitives on a tank are helpers (aim points and the like) that can sit
            // far from it.
            var helpers = new[] { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
            var renderers = body.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.GetComponent<MeshFilter>() != null &&
                            r.GetComponent<MeshFilter>().sharedMesh != null && !helpers.Contains(r.GetComponent<MeshFilter>().sharedMesh.name))
                .Cast<Renderer>().ToList();
            if (renderers.Count == 0)
                return Rect.zero;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in Corners(bounds))
            {
                var point = camera.WorldToViewportPoint(corner);
                if (point.z <= 0f)
                    return Rect.zero;
                min = Vector2.Min(min, new Vector2(point.x * Width, point.y * Height));
                max = Vector2.Max(max, new Vector2(point.x * Width, point.y * Height));
            }
            min = Vector2.Max(min, Vector2.zero);
            max = Vector2.Min(max, new Vector2(Width, Height));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (var i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        }

        private static float Contrast(float a, float b) => (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);

        // A tank pixel with ground within two pixels of it.
        private static bool OnEdge(bool[] tankPixels, int x, int y)
        {
            for (var d = 1; d <= 2; d++)
            {
                foreach (var (dx, dy) in new[] { (d, 0), (-d, 0), (0, d), (0, -d) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !tankPixels[ny * Width + nx])
                        return true;
                }
            }
            return false;
        }

        private static float Difference(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        private static Color Average(List<Color> colors) =>
            new Color(colors.Average(c => c.r), colors.Average(c => c.g), colors.Average(c => c.b));

        // WCAG relative luminance of an sRGB colour.
        private static float Luminance(Color c) => 0.2126f * Linear(c.r) + 0.7152f * Linear(c.g) + 0.0722f * Linear(c.b);

        private static float Linear(float v) => v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);

        // CIE L*a*b* of an sRGB colour (D65 white).
        private static Vector3 Lab(Color c)
        {
            float r = Linear(c.r), g = Linear(c.g), b = Linear(c.b);
            var x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
            var y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            var z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
            float F(float v) => v > 0.008856f ? Mathf.Pow(v, 1f / 3f) : 7.787f * v + 16f / 116f;
            return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
        }

        // CIEDE2000 colour difference of two L*a*b* colours (Sharma, Wu and Dalal 2005).
        private static float ColorDifference(Vector3 first, Vector3 second)
        {
            double l1 = first.x, a1 = first.y, b1 = first.z, l2 = second.x, a2 = second.y, b2 = second.z;
            double Pow7(double v) => Math.Pow(v, 7);
            double Rad(double degrees) => degrees * Math.PI / 180.0;
            double Deg(double radians) => radians * 180.0 / Math.PI;
            var meanChroma = (Math.Sqrt(a1 * a1 + b1 * b1) + Math.Sqrt(a2 * a2 + b2 * b2)) / 2;
            var g = 0.5 * (1 - Math.Sqrt(Pow7(meanChroma) / (Pow7(meanChroma) + Pow7(25))));
            double a1p = (1 + g) * a1, a2p = (1 + g) * a2;
            double c1 = Math.Sqrt(a1p * a1p + b1 * b1), c2 = Math.Sqrt(a2p * a2p + b2 * b2);
            double h1 = (Deg(Math.Atan2(b1, a1p)) + 360) % 360, h2 = (Deg(Math.Atan2(b2, a2p)) + 360) % 360;
            var dh = c1 * c2 == 0 ? 0 : h2 - h1 > 180 ? h2 - h1 - 360 : h2 - h1 < -180 ? h2 - h1 + 360 : h2 - h1;
            var dL = l2 - l1;
            var dC = c2 - c1;
            var dH = 2 * Math.Sqrt(c1 * c2) * Math.Sin(Rad(dh / 2));
            var meanL = (l1 + l2) / 2;
            var meanC = (c1 + c2) / 2;
            var meanH = c1 * c2 == 0 ? h1 + h2 : Math.Abs(h1 - h2) <= 180 ? (h1 + h2) / 2 : h1 + h2 < 360 ? (h1 + h2 + 360) / 2 : (h1 + h2 - 360) / 2;
            var t = 1 - 0.17 * Math.Cos(Rad(meanH - 30)) + 0.24 * Math.Cos(Rad(2 * meanH)) + 0.32 * Math.Cos(Rad(3 * meanH + 6)) -
                    0.20 * Math.Cos(Rad(4 * meanH - 63));
            var rotation = 30 * Math.Exp(-Math.Pow((meanH - 275) / 25, 2));
            var rc = 2 * Math.Sqrt(Pow7(meanC) / (Pow7(meanC) + Pow7(25)));
            var sl = 1 + 0.015 * Math.Pow(meanL - 50, 2) / Math.Sqrt(20 + Math.Pow(meanL - 50, 2));
            var sc = 1 + 0.045 * meanC;
            var sh = 1 + 0.015 * meanC * t;
            var rt = -Math.Sin(Rad(2 * rotation)) * rc;
            return (float)Math.Sqrt(Math.Pow(dL / sl, 2) + Math.Pow(dC / sc, 2) + Math.Pow(dH / sh, 2) + rt * (dC / sc) * (dH / sh));
        }

        private static string Describe(Color c) =>
            "rgb(" + Mathf.RoundToInt(c.r * 255) + "," + Mathf.RoundToInt(c.g * 255) + "," + Mathf.RoundToInt(c.b * 255) + ")";
    }
}
