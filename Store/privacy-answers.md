# Privacy declarations for Tread Shred 1.9.0 (D-016-AC-01)

What the build collects: Google Analytics for Firebase only (`TreadShredAnalytics`). No accounts, no
advertising SDK, no advertising ID (Android `AD_ID` permission removed; no iOS tracking prompt), no
attribution SDK (post-launch). Game Center scores and iCloud saves go to Apple under the player's own
Apple account; the developer does not receive them. Purchases are processed by Apple and Google.
Firebase defaults per Google (support.google.com/analytics/answer/10285841): app-instance ID, coarse
location from masked IP, product interaction, and in-app purchase events; none linked to identity.
Re-check this file whenever an SDK is added (attribution partner, AppLovin).

## App Store Connect: App Privacy

- Do you or your third-party partners collect data from this app? **Yes**
- **Data used to track you:** none
- **Data linked to you:** none
- **Data not linked to you**, each for **Analytics** only:
  - Identifiers: **Device ID**
  - Usage Data: **Product Interaction**
  - Purchases: **Purchase History**
  - Location: **Coarse Location**

## Google Play Console: Data safety

- Does your app collect or share any of the required user data types? **Yes**
- Is all of the user data collected by your app encrypted in transit? **Yes**
- Do you provide a way for users to request that their data is deleted? **No** (no accounts)
- **Collected, not shared**, processed ephemerally: **No**, required: **Yes** (collected automatically), purpose **Analytics**:
  - Location: **Approximate location**
  - App activity: **App interactions**
  - Device or other IDs: **Device or other IDs**
  - Financial info: **Purchase history**
- **Shared with third parties:** none (Firebase processes data on the developer's behalf)

## Both stores

- Contains ads: **No**
