# Tread Shred

Modern Unity 6 rebuild of the original tank game for iOS and Android from one source tree.

## Toolchain

- Unity `6000.3.27f2`
- iOS device target: iOS 15+, ARM64, IL2CPP
- Android target: API 36, min API 25, ARM64, IL2CPP
- iOS bundle ID: `com.tgts.tanksvstank`
- Android application ID: `com.tgts.tankkvstank`

## Analytics (Firebase)

Firebase Analytics (project `tread-shred-20261030`) records the events that drive LTV (spec D-021). The Firebase packages are vendored in `Packages/GooglePackages`. The client config files are not committed; regenerate them before building:

```sh
firebase apps:sdkconfig IOS 1:156147767939:ios:4c8da85fb37091fdd4d9b6 --project tread-shred-20261030 --out Assets/GoogleService-Info.plist
firebase apps:sdkconfig ANDROID 1:156147767939:android:a01c6cd896257b5ad4d9b6 --project tread-shred-20261030 --out Assets/google-services.json
```

## In-app purchases

The game has no ads. Revenue comes from consumable gem packs sold through Unity IAP (`com.unity.purchasing`), which uses StoreKit on iOS and Google Play Billing on Android. Gems are spent on the result-screen boosts: Armored Redeploy after a loss and Overdrive after a win. When the balance is short, the same button buys the smallest covering pack and applies the boost once the purchase is delivered.

- Catalog, prices in gems, and the starter grant: `Assets/Script/Economy/GemCatalog.cs`.
- Wallet persistence and duplicate-delivery protection: `Assets/Script/Economy/GemWallet.cs`, tested under `Assets/Tests/EditMode`.
- Store bridge: `Assets/Script/Store/TreadShredStore.cs`.

Before release, create consumable products with exactly the IDs in `GemCatalog.Packs` in App Store Connect and the Google Play Console, then test with Apple sandbox accounts and Play license testers. Purchases cannot complete in the iOS Simulator without a StoreKit configuration; the boosts still work there from the starter grant.

The in-game display type is Black Ops One, bundled under the SIL Open Font License; its license is included at `Assets/Art/BlackOpsOne-OFL.txt`.

## Source-of-truth assets

All runtime art ships with this Unity project and is tracked in Git. Tank and projectile surfaces live under `Assets/Art/`, and tactical UI icons live under `Assets/Image/`. Every imported asset has its matching Unity `.meta` file. The `Builds/` directory is intentionally ignored because it contains generated iOS/Android products, not source assets.

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
