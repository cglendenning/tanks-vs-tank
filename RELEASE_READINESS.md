# Tread Shred production-readiness review

Release target: `1.8.0` / iOS build `11` / Android version code `4`.

## Last verified September 25, 2026

- The production iOS build 11 is signed, uploaded, processed as `VALID`, attached to the editable App Store Connect version, and submitted. Apple currently shows `Waiting for Review`.
- The Apple upload emitted warning 90076 because the team identifier changed from the legacy build. It is non-blocking for this game, which does not use Keychain storage.
- The Google Play listing copy is now `Tread Shred`; the existing icon, feature graphic, and six phone screenshots are present.
- Google Play financial-features and health-app declarations are complete. Content rating and Data safety are actioned and ready to go with the next review submission.
- Google Play still displays the historical `App removed` banner, but the corrected release and listing changes are now submitted and show `Changes in review`. The app is not live again until Google completes that review.

## Completed

- One Unity 6 source tree targets iOS and Android.
- Production AdMob app IDs, interstitial units, and rewarded units are configured for both platforms.
- Test IDs remain available only when `TREAD_SHRED_USE_TEST_ADS=1` is explicitly set.
- Unused Unity IAP dependency was removed.
- iOS App Store build was uploaded successfully and processed as `VALID` in App Store Connect.
- iPhone and iPad marketing screenshots are stored under `marketing/screenshots/` and the new iPhone 6.7-inch and iPad Pro 12.9-inch assets are uploaded to the editable version.
- iOS age-rating fields, privacy URLs, version notes, and production metadata were updated where the App Store Connect API permits it.
- Apple App Privacy was completed and published in App Store Connect for the AdMob/ATT data flow: Device ID, Product Interaction, Advertising Data, Crash Data, and Performance Data. Build 11 includes a native ATT request before UMP/AdMob startup and is now `WAITING_FOR_REVIEW`.
- Android targets API 36, uses ARM64/IL2CPP, and the release script fails closed instead of producing a debug-signed store bundle. The Android manifest now carries the production AdMob application ID explicitly so Unity/Gradle manifest merging cannot omit it.

## Remaining store actions

### Apple App Store

The iOS 1.8.0 build 11 is attached and waiting for review. Release is configured for automatic release after approval. Apple App Privacy is published and reflects the production Google Mobile Ads SDK and ATT behavior.

### Google Play

The existing Play listing still shows the historical `App removed` banner while Google's reinstatement review is in progress. The rejected build's concrete defect was a missing AdMob application ID in the merged Android manifest, which caused the app to install but fail to load. The fix is committed in `Assets/Plugins/Android/AndroidManifest.xml`; the release build also explicitly switches to the Android target before building. A new AAB was uploaded successfully as Android version `1.8.0` / version code `4`, targeting API 36 and ARM64. It was signed with the existing Keychain-managed upload key whose registered certificate fingerprint is `79:8E:51:1A:55:BA:4E:89:AD:D3:F4:64:0F:16:61:A7:73:38:5E:A9`; no upload-key reset was requested. Google shows the production rollout and app-content/listing changes as `In review`; the release is not live until Google completes that review.

Do not reset the Play upload key: the existing registered key is usable through the local Keychain workflow. Run `scripts/build-tank-android-release.sh` with the four `TREAD_SHRED_ANDROID_*` signing variables and retrieve the keystore password from the configured Keychain service rather than storing it in the repository.
