# Ad implementation review

This is an implementation review, not an AdMob account-policy approval. The AdMob Policy Center and the app's actual creatives, store listing, audience, and traffic still require owner review.

Changes made:

- Replaced the legacy Google Mobile Ads API with the current Unity plugin.
- Added UMP consent updates on every launch and a privacy-options entry point.
- Requests ads only after consent information permits ad requests.
- Uses Google test ad IDs by default and does not request ads in the Unity Editor.
- Keeps the old tank interstitial unit as an explicit production fallback rather than silently using another app's IDs.
- Removed automatic banners from the default flow.
- Limits interstitials to gameplay-result boundaries and enforces a minimum interval between displays.
- Adds the iOS ATT usage description required before any tracking authorization prompt is shown.

Before a production release:

1. Replace the placeholder/test app IDs and verify each production unit in the Tanks VS Tank AdMob account.
2. Keep test mode enabled on development devices; never click live ads during testing.
3. Confirm the store privacy disclosures, consent message, age/audience settings, and ATT behavior match the final product.
4. Review AdMob Policy Center for account-specific warnings before switching `useTestAds` off.
