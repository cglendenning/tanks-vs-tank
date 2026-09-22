# Tread Shred

Modern Unity 6 rebuild of the original tank game for iOS and Android from one source tree.

## Toolchain

- Unity `6000.3.24f1`
- iOS device target: iOS 15+, ARM64, IL2CPP
- Android target: API 35, min API 25, ARM64, IL2CPP
- Bundle/application ID: `com.cglendenning.tanksvstank` (retained for existing signing/provisioning)

## Ads and privacy

The old Google Mobile Ads integration has been replaced with the current Unity plugin plus UMP consent handling. Ads are test-only by default. Interstitials are shown only at the existing win/loss break points, are preloaded, and are rate-limited. No banner is requested unless explicitly enabled.

Before enabling production ads, verify the Tread Shred app ID and ad-unit IDs in `Assets/Resources/TankAdConfiguration.asset` against the AdMob account for this app. Test ads are enabled for release validation.

The in-game display type is Black Ops One, bundled under the SIL Open Font License; its license is included at `Assets/Art/BlackOpsOne-OFL.txt`.

## Build validation

From the project root:

```sh
Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod TankBuildAutomation.BuildAndroidFromCommandLine

Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod TankBuildAutomation.BuildIosFromCommandLine
```

The Android APK is written to `Builds/Android/TreadShred.apk`. The iOS command exports `Builds/iOSDevice`, then CocoaPods must be installed before opening/building `Unity-iPhone.xcworkspace`.

## Signed iOS release and OTA

Use `scripts/build-tank-release.sh` after installing a valid Apple distribution certificate and Ad Hoc provisioning profile. It requires:

```sh
export TANK_TEAM_ID="..."
export TANK_CODE_SIGN_IDENTITY="Apple Distribution: ..."
export TANK_PROVISIONING_PROFILE_SPECIFIER="..."
scripts/build-tank-release.sh
```

Then run `scripts/serve-tank-ota.sh` to generate the OTA manifest and serve the signed IPA through the same Cloudflare tunnel workflow used by the other Unity app.

## Repository hygiene

Unity-generated `Library`, `Temp`, `Logs`, build products, CocoaPods output, and archived legacy plugins are intentionally excluded from source control. The migration keeps the old integration out of the active project; it is not a runtime dependency.
