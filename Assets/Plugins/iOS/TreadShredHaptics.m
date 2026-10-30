// D-027-AC-03: Taptic Engine pulses for hits and kills.
// Core Haptics reports whether the hardware has a Taptic Engine (iPhone 8 and
// later; no iPad has one). UIImpactFeedbackGenerator plays the pulse and
// follows the player's Settings > Sounds & Haptics > System Haptics switch,
// which iOS does not let apps read.
@import CoreHaptics;
@import UIKit;

int TreadShredHapticsSupported(void)
{
    return CHHapticEngine.capabilitiesForHardware.supportsHaptics ? 1 : 0;
}

// pulse: 0 = hit taken, 1 = kill.
void TreadShredHapticsPulse(int pulse)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackStyle style = pulse == 1 ? UIImpactFeedbackStyleHeavy : UIImpactFeedbackStyleMedium;
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [generator prepare];
        [generator impactOccurred];
    });
}
