using System;
using System.Runtime.InteropServices;
using TreadShred.Progression;
using UnityEngine;

/// <summary>
/// D-027-AC-03: haptic pulses on hits and kills through the platform's own
/// haptics (Taptic Engine on iOS, the system vibrator on Android), only on
/// devices that can vibrate.
/// </summary>
public static class TreadShredHaptics
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int TreadShredHapticsSupported();
    [DllImport("__Internal")] private static extern void TreadShredHapticsPulse(int pulse);
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
#endif

    private static bool? supported;

    /// <summary>Whether this device has vibration hardware; checked once.</summary>
    public static bool DeviceCanVibrate
    {
        get
        {
            if (!supported.HasValue)
                supported = Detect();
            return supported.Value;
        }
    }

    public static void Pulse(HapticPulse pulse, bool vibrationOn)
    {
        if (!HapticsRules.ShouldPulse(vibrationOn, DeviceCanVibrate))
            return;
        try
        {
            Play(pulse);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Haptics] pulse failed: " + error.Message);
        }
    }

    private static bool Detect()
    {
        try
        {
#if UNITY_IOS && !UNITY_EDITOR
            return TreadShredHapticsSupported() != 0;
#elif UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            return vibrator != null && vibrator.Call<bool>("hasVibrator");
#else
            return false;
#endif
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Haptics] capability check failed: " + error.Message);
            return false;
        }
    }

    private static void Play(HapticPulse pulse)
    {
#if UNITY_IOS && !UNITY_EDITOR
        TreadShredHapticsPulse((int)pulse);
#elif UNITY_ANDROID && !UNITY_EDITOR
        var milliseconds = HapticsRules.AndroidMilliseconds(pulse);
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            if (version.GetStatic<int>("SDK_INT") < 26)
            {
                vibrator.Call("vibrate", milliseconds);
                return;
            }
        }
        // -1 is VibrationEffect.DEFAULT_AMPLITUDE for vibrators without strength control.
        var amplitude = vibrator.Call<bool>("hasAmplitudeControl") ? HapticsRules.AndroidAmplitude(pulse) : -1;
        using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
        using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
            vibrator.Call("vibrate", effect);
#endif
    }
}
