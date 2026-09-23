# Tread Shred production-readiness review

Release target: `1.8.0` / iOS build `3` / Android version code `3`.

## Completed

- One Unity 6 source tree targets iOS and Android.
- Production AdMob app IDs, interstitial units, and rewarded units are configured for both platforms.
- Test IDs remain available only when `TREAD_SHRED_USE_TEST_ADS=1` is explicitly set.
- Unused Unity IAP dependency was removed.
- iOS App Store build was uploaded successfully and processed as `VALID` in App Store Connect.
- iPhone and iPad marketing screenshots are stored under `marketing/screenshots/` and the new iPhone 6.7-inch and iPad Pro 12.9-inch assets are uploaded to the editable version.
- iOS age-rating fields, privacy URLs, version notes, and production metadata were updated where the App Store Connect API permits it.
- Android targets API 36, uses ARM64/IL2CPP, and the release script fails closed instead of producing a debug-signed store bundle.

## Remaining store actions

### Apple App Store

App Store Connect still requires the App Privacy questionnaire to be completed and published in the web UI. The binary requests ad tracking/consent through Google Mobile Ads, so this must be answered accurately for the data collected by the configured ad SDK and then published before the version can be submitted for review. Apple does not expose this questionnaire through the public App Store Connect API.

### Google Play

The existing Play listing is marked removed and expects upload-key SHA-1 `16:35:84:E5:0C:40:7A:EA:2C:B1:84:D6:9B:71:F8:81:BD:75:14:2A`. No local keystore matches that fingerprint. The rebuilt AAB is otherwise configured for the current Android requirements, but Play rejects it until the original upload keystore is restored or the Play Console upload key is explicitly reset. An appeal for the old removed listing has been submitted.

Do not reset the Play upload key without confirming the account-level security change. Once the original key or an approved replacement is available, run `scripts/build-tank-android-release.sh` with the four `TREAD_SHRED_ANDROID_*` signing variables and upload the resulting AAB.
