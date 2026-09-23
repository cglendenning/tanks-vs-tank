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

The existing Play listing is marked removed and previously expected upload-key SHA-1 `16:35:84:E5:0C:40:7A:EA:2C:B1:84:D6:9B:71:F8:81:BD:75:14:2A`. A new upload keystore is retained outside the repository at `/Users/craig/keys/tread-shred-upload-v2.jks`, with password stored in the local Keychain; its SHA-1 is `79:8E:51:1A:55:BA:4E:89:AD:D3:F4:64:0F:16:61:A7:73:38:5E:A9`. The reset request is accepted but becomes valid on September 25, 2026 at 08:05:50 UTC. The rebuilt AAB is staged in the Play production draft and must be resubmitted after that time. An appeal for the old removed listing has been submitted.

Do not reset the Play upload key without confirming the account-level security change. Once the original key or an approved replacement is available, run `scripts/build-tank-android-release.sh` with the four `TREAD_SHRED_ANDROID_*` signing variables and upload the resulting AAB.
