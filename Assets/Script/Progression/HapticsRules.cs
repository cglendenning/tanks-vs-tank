namespace TreadShred.Progression
{
    public enum HapticPulse
    {
        Hit = 0,
        Kill = 1
    }

    /// <summary>
    /// D-027-AC-03 and D-014: pulse only when Vibration is on and the device can
    /// vibrate; a device that cannot shows NOT SUPPORTED instead of a toggle.
    /// </summary>
    public static class HapticsRules
    {
        public const string NotSupported = "NOT SUPPORTED";

        public static bool ShouldPulse(bool vibrationOn, bool deviceCanVibrate)
        {
            return vibrationOn && deviceCanVibrate;
        }

        public static string SettingLabel(bool vibrationOn, bool deviceCanVibrate)
        {
            if (!deviceCanVibrate)
                return NotSupported;
            return vibrationOn ? "ON" : "OFF";
        }

        // Android one-shot lengths: short and sharp, longer for a kill.
        public static long AndroidMilliseconds(HapticPulse pulse)
        {
            return pulse == HapticPulse.Kill ? 80 : 40;
        }

        // 1-255 where the vibrator supports strength control.
        public static int AndroidAmplitude(HapticPulse pulse)
        {
            return pulse == HapticPulse.Kill ? 255 : 150;
        }
    }
}
