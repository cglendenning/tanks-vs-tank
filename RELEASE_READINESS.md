# Tread Shred production-readiness review

Release target: `1.8.0` / iOS build `4` / Android version code `4`.

## Last verified September 23, 2026

- The production iOS build 4 is signed, uploaded, processed as `VALID`, and attached to the editable App Store Connect version. Apple currently shows `Prepare for Submission`; App Review contact phone and email still need to be filled before `Add for Review` becomes available.
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
- Apple App Privacy was completed and published in App Store Connect for the AdMob/ATT data flow: Device ID, Product Interaction, Advertising Data, Crash Data, and Performance Data. The iOS 1.8.0 submission was added to review and is now `WAITING_FOR_REVIEW`.
- Android targets API 36, uses ARM64/IL2CPP, and the release script fails closed instead of producing a debug-signed store bundle.

## Remaining store actions

### Apple App Store

The iOS 1.8.0 build 4 is attached and ready for review. Release is configured for automatic release after approval. Apple App Privacy is published and reflects the production Google Mobile Ads SDK and ATT behavior. Fill the two App Review contact fields, click `Add for Review`, then confirm the submission in the review queue.

### Google Play

The existing Play listing is marked removed and previously expected upload-key SHA-1 `16:35:84:E5:0C:40:7A:EA:2C:B1:84:D6:9B:71:F8:81:BD:75:14:2A`. A new upload keystore is retained outside the repository at `/Users/craig/keys/tread-shred-upload-v2.jks`, with password stored in the local Keychain; its SHA-1 is `79:8E:51:1A:55:BA:4E:89:AD:D3:F4:64:0F:16:61:A7:73:38:5E:A9`. The reset request is accepted but becomes valid on September 25, 2026 at 08:05:50 UTC. The rebuilt AAB is staged in the Play production draft and must be resubmitted after that time. An appeal for the old removed listing has been submitted.

Do not reset the Play upload key without confirming the account-level security change. Once the original key or an approved replacement is available, run `scripts/build-tank-android-release.sh` with the four `TREAD_SHRED_ANDROID_*` signing variables and upload the resulting AAB.
