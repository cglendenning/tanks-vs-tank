using NUnit.Framework;

namespace TreadShred.Progression.Tests
{
    public sealed class HapticsRulesTests
    {
        [Test(Description = "D-027-AC-03: hits and kills pulse only when Vibration is on and the device can vibrate")]
        public void PulsesOnlyWhenOnAndSupported()
        {
            Assert.IsTrue(HapticsRules.ShouldPulse(vibrationOn: true, deviceCanVibrate: true));
            Assert.IsFalse(HapticsRules.ShouldPulse(vibrationOn: false, deviceCanVibrate: true));
            Assert.IsFalse(HapticsRules.ShouldPulse(vibrationOn: true, deviceCanVibrate: false));
            Assert.Greater(HapticsRules.AndroidMilliseconds(HapticPulse.Kill), HapticsRules.AndroidMilliseconds(HapticPulse.Hit),
                "a kill lands harder than a hit");
        }

        [Test(Description = "D-014-AC-01: the VIBRATION row shows its state, or NOT SUPPORTED on a device that cannot vibrate")]
        public void SettingShowsStateOrNotSupported()
        {
            Assert.AreEqual("ON", HapticsRules.SettingLabel(true, true));
            Assert.AreEqual("OFF", HapticsRules.SettingLabel(false, true));
            Assert.AreEqual("NOT SUPPORTED", HapticsRules.SettingLabel(true, false));
        }
    }
}
