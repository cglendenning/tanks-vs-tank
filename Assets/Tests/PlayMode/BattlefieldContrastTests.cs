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
    // last two differ are exactly the tanks and their outlines, with no holes where a tank happens to match the ground
    // and no shadow counted as tank. Each
    // tank's edge (its outermost pixels, the outline the eye finds it by, as WCAG 1.4.11 judges graphical objects)
    // is compared with the ground around it; the body's own contrast is reported beside it.
    public sealed class BattlefieldContrastTests
    {
        // WCAG 2.1's minimum for graphical objects a player must pick out (non-text contrast, 3:1).
        private const float MinimumContrast = 3f;
        private const int Width = BattlefieldCapture.Width;
        private const int Height = BattlefieldCapture.Height;
        private const float ChangedPixel = 0.06f;
        private const int SmallestTank = 60;

        private static Type GameType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

        [UnityTest(Description = "D-039-AC-03: on the first mission of every ground, every tank's edge stands out from the ground around it by at least 3:1 in brightness")]
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
                foreach (var (tank, ratio, tankRgb, groundRgb) in measured)
                {
                    var line = "ground " + ground + " (mission " + mission + ") " + tank + ": edge " + ratio.ToString("0.00") +
                               ":1, " + tankRgb + " on ground " + groundRgb;
                    report.Add(line);
                    if (ratio < MinimumContrast)
                        failures.Add(line);
                }
            }
            Debug.Log("[Contrast]\n" + string.Join("\n", report));
            Assert.IsEmpty(failures, "tanks below " + MinimumContrast + ":1 against their ground:\n" + string.Join("\n", failures));
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
            var renderers = tanks.SelectMany(t => t.Body.GetComponentsInChildren<Renderer>()).ToList();
            var bodies = renderers.Where(r => r.name != "Tread Shred Outline").ToList();
            var paint = new Material(Shader.Find("TreadShred/Outline")) { color = Color.magenta };
            paint.SetFloat("_Width", 0f);
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
                // would count against the outline.
                var edgeLuminance = edge.Select(Luminance).OrderBy(l => l).ElementAt(edge.Count / 2);
                results.Add((name, Contrast(edgeLuminance, groundLuminance),
                    "edge " + Describe(Average(edge)) + ", body " + Describe(Average(tank)) + " (" +
                    Contrast(Luminance(Average(tank)), groundLuminance).ToString("0.00") + ":1)", Describe(Average(around))));
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

        private static string Describe(Color c) =>
            "rgb(" + Mathf.RoundToInt(c.r * 255) + "," + Mathf.RoundToInt(c.g * 255) + "," + Mathf.RoundToInt(c.b * 255) + ")";
    }
}
