# Tread Shred production-readiness review

Release target: `1.8.0` / iOS build `11` / Android version code `4`.

## Last verified September 25, 2026

- The production iOS build 11 is signed, uploaded, processed as `VALID`, attached to the editable App Store Connect version, and submitted. Apple currently shows `Waiting for Review`.
- The Apple upload emitted warning 90076 because the team identifier changed from the legacy build. It is non-blocking for this game, which does not use Keychain storage.
- The Google Play listing copy is now `Tread Shred`; the existing icon, feature graphic, and six phone screenshots are present.
- Google Play financial-features and health-app declarations are complete. Content rating and Data safety are actioned and ready to go with the next review submission.
- Google Play still reports the package as `Removed by Google`; the appeal remains submitted. A production Android release cannot be published until Google reinstates the listing and the upload-key reset is active.

## Completed

- One Unity 6 source tree targets iOS and Android.
- Production AdMob app IDs, interstitial units, and rewarded units are configured for both platforms.
- Test IDs remain available only when `TREAD_SHRED_USE_TEST_ADS=1` is explicitly set.
- Unused Unity IAP dependency was removed.
- iOS App Store build was uploaded successfully and processed as `VALID` in App Store Connect.
- iPhone and iPad marketing screenshots are stored under `marketing/screenshots/` and the new iPhone 6.7-inch and iPad Pro 12.9-inch assets are uploaded to the editable version.
- iOS age-rating fields, privacy URLs, version notes, and production metadata were updated where the App Store Connect API permits it.
- Apple App Privacy was completed and published in App Store Connect for the AdMob/ATT data flow: Device ID, Product Interaction, Advertising Data, Crash Data, and Performance Data. Build 11 includes a native ATT request before UMP/AdMob startup and is now `WAITING_FOR_REVIEW`.
- Android targets API 36, uses ARM64/IL2CPP, and the release script fails closed instead of producing a debug-signed store bundle.

## Remaining store actions

### Apple App Store

The iOS 1.8.0 build 11 is attached and waiting for review. Release is configured for automatic release after approval. Apple App Privacy is published and reflects the production Google Mobile Ads SDK and ATT behavior.

### Google Play

The existing Play listing still shows `App removed` while Google's appeal/review is in progress. After the upload-key reset became valid on September 25, 2026 at 08:05:50 UTC, the staged AAB was removed and re-uploaded successfully as Android version `1.8.0` / version code `3`, targeting API 36 and ARM64. The Advertising ID declaration was corrected for AdMob use. Google now shows the production rollout and app-content changes `In review`; the release is not live until Google completes that review. The Play listing still contains the older icon/screenshots; the new source assets are committed under `marketing/screenshots/store-ready/` and require a separate listing-asset update after the removal gate clears.

Do not reset the Play upload key without confirming the account-level security change. Once the original key or an approved replacement is available, run `scripts/build-tank-android-release.sh` with the four `TREAD_SHRED_ANDROID_*` signing variables and upload the resulting AAB.
