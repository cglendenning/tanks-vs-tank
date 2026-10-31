using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace TreadShred.Screens.Tests
{
    public readonly struct ScreenShape
    {
        public ScreenShape(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }

        public override string ToString() => Name + " " + Width + "x" + Height;
    }

    public enum Defect
    {
        Overlap,
        CutOff,
        SpillsPastButton,
        PartlyHidden,
        CrowdsButton,
        NotUpright,
        WrongFont,
        NotAllCaps,
        UnformattedNumber,
        BackDoesNotClose,
        NotReached
    }

    public readonly struct Finding
    {
        public Finding(Defect defect, string detail)
        {
            Defect = defect;
            Detail = detail;
        }

        public Defect Defect { get; }
        public string Detail { get; }

        public override string ToString() => Defect + ": " + Detail;
    }

    /// <summary>
    /// Lays the game's UI out exactly as a given screen would (every canvas is drawn by a camera
    /// into a texture of that size) and reports what a person looking at it would see wrong:
    /// text over text, text cut off by the screen edge, spilling past its button or partly under
    /// something drawn on top of it, text upside down or mirrored, the wrong typeface, lowercase
    /// copy, or a long number without thousands separators.
    /// </summary>
    public static class ScreenAudit
    {
        public static readonly ScreenShape[] Shapes =
        {
            new ScreenShape("ANDROID 20:9", 2400, 1080),
            new ScreenShape("IPHONE 12 MINI", 2340, 1080),
            new ScreenShape("16:9", 1334, 750),
            new ScreenShape("IPAD 4:3", 2048, 1536),
        };

        // Set by scripts/run-unity-tests.sh --screenshots: every audited screen is also saved as a PNG.
        private static readonly string ScreenshotDirectory = Environment.GetEnvironmentVariable("TREADSHRED_SCREENSHOTS");
        private const string Typeface = "BlackOpsOne";
        private static readonly Regex Tags = new Regex("<[^>]+>");
        private static readonly Regex LongNumber = new Regex("(?<![\\d,.])\\d{4,}(?![\\d,])");

        private static Camera camera;
        private static RenderTexture target;

        /// <summary>The text of every label a person can read on the screen last inspected.</summary>
        public static List<string> Shown { get; } = new List<string>();

        /// <summary>Labels on the screen last inspected that something drawn over them hides whole, and what hides them.</summary>
        public static List<string> Hidden { get; } = new List<string>();

        /// <summary>Draws every canvas at the given screen size; the layout settles over the next frames.</summary>
        public static IEnumerator Present(ScreenShape shape)
        {
            if (target == null || target.width != shape.Width || target.height != shape.Height)
            {
                if (target != null)
                    target.Release();
                target = new RenderTexture(shape.Width, shape.Height, 24);
            }
            if (camera == null)
            {
                // Far from the scene so it draws only the canvases placed just in front of it.
                camera = new GameObject("ScreenAuditCamera").AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, -10000f, 0f);
                camera.clearFlags = CameraClearFlags.Depth;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.enabled = false;
            }
            camera.targetTexture = target;
            foreach (var canvas in RootCanvases())
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Everything a person would see wrong on the screen as it stands.</summary>
        public static List<Finding> Inspect(ScreenShape shape, string screen)
        {
            var findings = new List<Finding>();
            var drawn = DrawOrder();
            var tolerance = 2f * shape.Height / 1080f;
            var screenRect = new Rect(0f, 0f, shape.Width, shape.Height);
            var shown = new List<Drawn>();
            Shown.Clear();
            Hidden.Clear();

            foreach (var label in drawn.Where(d => d.Graphic is Text))
            {
                var text = (Text)label.Graphic;
                if (!Visible(text) || !Ink(text, out var ink) || !Clip(text, ref ink))
                    continue;
                // Wholly off screen is hidden by design (panels that slide in); partly off screen is cut off.
                if (!ink.Overlaps(screenRect))
                    continue;
                // Under something drawn later that covers it whole: hidden, or dimmed behind a dialog.
                var over = drawn.FirstOrDefault(d => d.Order > label.Order && d.Graphic is Image image && Covers(image) && Contains(d.Rect, ink, tolerance));
                if (over.Graphic != null)
                {
                    Hidden.Add(Clean(text.text) + " under " + Path(over.Graphic.transform));
                    continue;
                }
                var described = Describe(text, screen, shape);
                if (!Contains(screenRect, ink, tolerance))
                    findings.Add(new Finding(Defect.CutOff, described));
                // Within one screen, anything drawn over part of a label hides that part.
                var cover = drawn.FirstOrDefault(d => d.Order > label.Order && d.Canvas == label.Canvas &&
                    d.Graphic is Image image && Covers(image) && Intersects(d.Rect, ink, tolerance));
                if (cover.Graphic != null)
                    findings.Add(new Finding(Defect.PartlyHidden, described + " under " + Path(cover.Graphic.transform)));
                // Text running into another button on the same screen reads as part of it, or vanishes against it,
                // whichever draws first. Buttons on a screen behind a dialog do not count.
                var crowded = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b =>
                    b.isActiveAndEnabled && !text.transform.IsChildOf(b.transform) && b.targetGraphic is Image other &&
                    other.canvas != null && other.canvas.rootCanvas == label.Canvas &&
                    Covers(other) && Intersects(PixelRect(other.rectTransform), ink, tolerance) &&
                    !Contains(PixelRect(other.rectTransform), ink, tolerance));
                if (crowded != null)
                    findings.Add(new Finding(Defect.CrowdsButton, described + " runs into " + Path(crowded.transform)));
                var button = text.GetComponentInParent<Button>();
                if (button != null && button.targetGraphic is Image face && Solid(face) &&
                    !Contains(PixelRect(face.rectTransform), ink, tolerance))
                    findings.Add(new Finding(Defect.SpillsPastButton, described));
                if (!Upright(text.rectTransform))
                    findings.Add(new Finding(Defect.NotUpright, described));
                if (text.font == null || !text.font.name.Replace(" ", string.Empty).StartsWith(Typeface))
                    findings.Add(new Finding(Defect.WrongFont, described + " uses " + (text.font ? text.font.name : "no font")));
                var copy = Tags.Replace(text.text, string.Empty);
                if (copy.Any(char.IsLower))
                    findings.Add(new Finding(Defect.NotAllCaps, described));
                if (LongNumber.IsMatch(copy))
                    findings.Add(new Finding(Defect.UnformattedNumber, described));
                shown.Add(new Drawn(text, label.Order, ink, label.Canvas));
                Shown.Add(Clean(text.text));
            }

            for (var i = 0; i < shown.Count; i++)
            for (var j = i + 1; j < shown.Count; j++)
            {
                if (Intersects(shown[i].Rect, shown[j].Rect, tolerance))
                    findings.Add(new Finding(Defect.Overlap,
                        Describe((Text)shown[i].Graphic, screen, shape) + " overlaps '" + Clean(((Text)shown[j].Graphic).text) + "'"));
            }

            Save(screen, shape);
            return findings;
        }

        public static string Report(IEnumerable<Finding> findings) =>
            string.Join("\n", findings.Select(f => f.ToString()).Distinct());

        private readonly struct Drawn
        {
            public Drawn(Graphic graphic, int order, Rect rect, Canvas canvas)
            {
                Graphic = graphic;
                Order = order;
                Rect = rect;
                Canvas = canvas;
            }

            public Graphic Graphic { get; }
            public int Order { get; }
            public Rect Rect { get; }
            public Canvas Canvas { get; }
        }

        private static IEnumerable<Canvas> RootCanvases() =>
            UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.isActiveAndEnabled);

        // Canvases in sorting order, then each canvas's graphics in hierarchy order: later draws on top.
        // Rects are clipped by any scroll view or mask they sit in; wholly clipped graphics are left out.
        private static List<Drawn> DrawOrder()
        {
            var drawn = new List<Drawn>();
            foreach (var canvas in RootCanvases().OrderBy(c => c.sortingOrder))
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                var rect = PixelRect(graphic.rectTransform);
                if (graphic.isActiveAndEnabled && Clip(graphic, ref rect))
                    drawn.Add(new Drawn(graphic, drawn.Count, rect, canvas));
            }
            return drawn;
        }

        private static bool Clip(Graphic graphic, ref Rect rect)
        {
            foreach (var mask in graphic.GetComponentsInParent<RectMask2D>())
            {
                if (mask.enabled)
                    rect = Intersection(rect, PixelRect(mask.rectTransform));
            }
            foreach (var mask in graphic.GetComponentsInParent<Mask>())
            {
                if (mask.enabled && mask.gameObject != graphic.gameObject)
                    rect = Intersection(rect, PixelRect(mask.rectTransform));
            }
            return rect.width > 0f && rect.height > 0f;
        }

        private static Rect Intersection(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                Mathf.Max(Mathf.Max(a.xMin, b.xMin), Mathf.Min(a.xMax, b.xMax)),
                Mathf.Max(Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.yMax, b.yMax)));

        private static float GroupAlpha(Component component)
        {
            var alpha = 1f;
            foreach (var group in component.GetComponentsInParent<CanvasGroup>())
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups)
                    break;
            }
            return alpha;
        }

        private static bool HasArea(RectTransform rect) =>
            Mathf.Abs(rect.lossyScale.x) > 1e-5f && Mathf.Abs(rect.lossyScale.y) > 1e-5f;

        private static bool Visible(Text text) =>
            !string.IsNullOrWhiteSpace(Tags.Replace(text.text ?? string.Empty, string.Empty)) &&
            text.color.a >= 0.05f && GroupAlpha(text) >= 0.05f && HasArea(text.rectTransform);

        // Drawn strongly enough to obscure what is under it.
        private static bool Covers(Image image) =>
            image.color.a * GroupAlpha(image) >= 0.5f && HasArea(image.rectTransform);

        // A filled box (sprites with transparent areas do not count).
        private static bool Solid(Image image) =>
            image.color.a * GroupAlpha(image) >= 0.9f && (image.sprite == null || image.type == Image.Type.Sliced) &&
            HasArea(image.rectTransform);

        // Reads left to right, right side up, relative to its canvas.
        private static bool Upright(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas.transform;
            var relative = Quaternion.Inverse(canvas.rotation) * rect.rotation;
            var scale = canvas.InverseTransformVector(rect.TransformVector(Vector3.right + Vector3.up));
            return Mathf.Abs(Mathf.DeltaAngle(0f, relative.eulerAngles.z)) < 5f && scale.x > 0f && scale.y > 0f;
        }

        // The glyphs as drawn, in screen pixels, measured by Unity's own text generator.
        private static bool Ink(Text text, out Rect ink)
        {
            ink = default;
            var rect = text.rectTransform;
            var generator = new TextGenerator();
            generator.PopulateWithErrors(text.text, text.GetGenerationSettings(rect.rect.size), text.gameObject);
            var verts = generator.verts;
            if (verts.Count == 0)
                return false;
            var unitsPerPixel = 1f / text.pixelsPerUnit;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var vert in verts)
            {
                var screen = ToScreen(rect.TransformPoint(vert.position * unitsPerPixel));
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxY = Mathf.Max(maxY, screen.y);
            }
            ink = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return ink.width > 0f && ink.height > 0f;
        }

        private static Rect PixelRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var a = ToScreen(corners[0]);
            var b = ToScreen(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Vector2 ToScreen(Vector3 world) => camera.WorldToScreenPoint(world);

        private static bool Contains(Rect outer, Rect inner, float tolerance) =>
            inner.xMin >= outer.xMin - tolerance && inner.xMax <= outer.xMax + tolerance &&
            inner.yMin >= outer.yMin - tolerance && inner.yMax <= outer.yMax + tolerance;

        private static bool Intersects(Rect a, Rect b, float tolerance) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > tolerance &&
            Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > tolerance;

        private static string Clean(string text) => Regex.Replace(Tags.Replace(text, string.Empty), "\\s+", " ").Trim();

        private static string Path(Transform transform)
        {
            var names = new List<string>();
            for (var t = transform; t != null && names.Count < 3; t = t.parent)
                names.Insert(0, t.name);
            return string.Join("/", names);
        }

        private static string Describe(Text text, string screen, ScreenShape shape) =>
            "[" + screen + ", " + shape.Name + "] '" + Clean(text.text) + "' (" + Path(text.transform) + ")";

        private static void Save(string screen, ScreenShape shape)
        {
            if (string.IsNullOrEmpty(ScreenshotDirectory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            Directory.CreateDirectory(ScreenshotDirectory);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            foreach (var sceneCamera in Camera.allCameras.Where(c => c != camera).OrderBy(c => c.depth))
            {
                var keep = sceneCamera.targetTexture;
                sceneCamera.targetTexture = target;
                sceneCamera.Render();
                sceneCamera.targetTexture = keep;
            }
            camera.Render();
            var image = new Texture2D(shape.Width, shape.Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, shape.Width, shape.Height), 0, 0);
            RenderTexture.active = previous;
            var file = Regex.Replace(screen + "-" + shape.Name, "[^A-Za-z0-9]+", "-").ToLowerInvariant() + ".png";
            File.WriteAllBytes(System.IO.Path.Combine(ScreenshotDirectory, file), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }
    }
}
