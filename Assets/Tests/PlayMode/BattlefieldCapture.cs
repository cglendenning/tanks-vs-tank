using System;
using System.IO;
using UnityEngine;

namespace TreadShred.Screens.Tests
{
    // Renders the arena camera into an image for the battlefield checks, and saves it when
    // scripts/run-unity-tests.sh --screenshots sets TREADSHRED_SCREENSHOTS.
    public static class BattlefieldCapture
    {
        public const int Width = 2340;
        public const int Height = 1080;

        private static readonly string ScreenshotDirectory = Environment.GetEnvironmentVariable("TREADSHRED_SCREENSHOTS");

        // Rendering needs a graphics device: the suite runs PlayMode with -nographics unless screenshots are asked for
        // (scripts/run-unity-tests.sh --screenshots), and a camera render without one crashes the editor.
        public static bool Available => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        /// <summary>Saves the frame when screenshots are asked for and a graphics device is there; otherwise nothing.</summary>
        public static void Screenshot(Camera camera, string name)
        {
            if (Available && !string.IsNullOrEmpty(ScreenshotDirectory))
                Render(camera, name);
        }

        public static Color[] Render(Camera camera, string saveAs = null)
        {
            if (!Available)
                throw new InvalidOperationException("no graphics device to render with");
            var target = new RenderTexture(Width, Height, 24);
            var keep = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = keep;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var pixels = image.GetPixels();
            if (saveAs != null && !string.IsNullOrEmpty(ScreenshotDirectory))
            {
                Directory.CreateDirectory(ScreenshotDirectory);
                File.WriteAllBytes(Path.Combine(ScreenshotDirectory, saveAs + ".png"), image.EncodeToPNG());
            }
            UnityEngine.Object.Destroy(image);
            target.Release();
            UnityEngine.Object.Destroy(target);
            return pixels;
        }
    }
}
