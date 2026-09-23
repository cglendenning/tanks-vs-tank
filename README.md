# Tread Shred

Modern Unity 6 rebuild of the original tank game for iOS and Android from one source tree.

## Toolchain

- Unity `6000.3.24f1`
- iOS device target: iOS 15+, ARM64, IL2CPP
- Android target: API 36, min API 25, ARM64, IL2CPP
- iOS bundle ID: `com.tgts.tanksvstank`
- Android application ID: `com.tgts.tankkvstank`

## Ads and privacy

The old Google Mobile Ads integration has been replaced with the current Unity plugin plus UMP consent handling. Production builds use the Tread Shred AdMob app and unit IDs stored in `Assets/Resources/TankAdConfiguration.asset`; test ads are selected only when `TREAD_SHRED_USE_TEST_ADS=1` is explicitly set for a validation build. Interstitials are shown only at the existing win/loss break points, are preloaded, and are rate-limited. No banner is requested unless explicitly enabled.

Before a store submission, verify the Tread Shred app and ad-unit IDs in `Assets/Resources/TankAdConfiguration.asset` against the AdMob account for this app. New AdMob units can take time to begin serving after creation; the app must remain compliant with consent and policy requirements while they ramp.

The in-game display type is Black Ops One, bundled under the SIL Open Font License; its license is included at `Assets/Art/BlackOpsOne-OFL.txt`.

## Source-of-truth assets

All runtime art ships with this Unity project and is tracked in Git. Tank and projectile surfaces live under `Assets/Art/`, tactical UI icons live under `Assets/Image/`, and ad/build configuration lives under `Assets/Resources/`. Every imported asset has its matching Unity `.meta` file. The `Builds/` directory is intentionally ignored because it contains generated iOS/Android products, not source assets.

## Build validation

From the project root:

```sh
Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod TankBuildAutomation.BuildAndroidFromCommandLine

Unity -batchmode -quit -nographics -projectPath . \
  -executeMethod TankBuildAutomation.BuildIosFromCommandLine
```

The Android APK is written to `Builds/Android/TreadShred.apk`. The iOS command exports `Builds/iOSDevice`, then CocoaPods must be installed before opening/building `Unity-iPhone.xcworkspace`.

For a store AAB, never use the editor's debug signing. Set the upload keystore variables and run `scripts/build-tank-android-release.sh`:

```sh
export TREAD_SHRED_ANDROID_KEYSTORE="/secure/path/tread-shred-upload.jks"
export TREAD_SHRED_ANDROID_KEY_ALIAS="..."
export TREAD_SHRED_ANDROID_KEYSTORE_PASSWORD="..."
export TREAD_SHRED_ANDROID_KEY_PASSWORD="..."
scripts/build-tank-android-release.sh
```

The release build fails closed when those values are absent. The keystore and passwords are intentionally not stored in this repository.

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
