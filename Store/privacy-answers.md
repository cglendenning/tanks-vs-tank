# Privacy declarations for Tread Shred 1.9.2 (D-016-AC-01)

What the build collects: Google Analytics for Firebase on both platforms, plus Tenjin attribution on Android
(`TreadShredAnalytics`, `TreadShredAttribution`). No accounts, no advertising SDK, no ads.
Firebase defaults per Google (support.google.com/analytics/answer/10285841): app-instance ID, coarse
location from masked IP, product interaction, and in-app purchase events; none linked to identity, and
Firebase never collects the advertising ID on either platform.
Android only (D-005, D-023): Tenjin's SDK reads the advertising ID and app set ID, records app opens and
verified gem pack purchases, and forwards installs and purchases to AppLovin, which bought the ad, to
measure and optimize ad campaigns.
iOS: no advertising ID, no tracking prompt, no attribution SDK. Apple's SKAdNetwork tells AppLovin, and
Tenjin through `NSAdvertisingAttributionReportEndpoint`, which campaign drove an install, carrying only a
conversion value for purchase revenue; Apple sends it without any device identifier.
Unity IAP (`com.unity.purchasing`) can attach the vendor ID (IDFV) to the purchase events it reports to Unity, which
Device ID and Purchase History below cover; it reads the advertising ID only under Unity's ads-intent consent, which
the game never grants. App Review rejected 1.9.3 on 2026-11-03 (Guideline 5.1.2(i)) because the App Privacy label in
App Store Connect said Device ID and Product Interaction were used to track; the label must match this file.
Game Center scores and iCloud saves go to Apple under the player's own Apple account; the developer
does not receive them. Purchases are processed by Apple and Google.
Re-check this file whenever an SDK is added.

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
- **Collected**, processed ephemerally: **No**, required: **Yes** (collected automatically):
  - Location: **Approximate location**, purpose **Analytics**
  - App activity: **App interactions**, purpose **Analytics**
  - Device or other IDs: **Device or other IDs**, purposes **Analytics** and **Advertising or marketing**
  - Financial info: **Purchase history**, purposes **Analytics** and **Advertising or marketing**
- **Shared**, purpose **Advertising or marketing** (Tenjin forwards them to AppLovin to measure the ad
  campaigns that bought each install; Firebase and Tenjin process data on the developer's behalf):
  - Device or other IDs: **Device or other IDs**
  - Financial info: **Purchase history**

## Both stores

- Contains ads: **No**
