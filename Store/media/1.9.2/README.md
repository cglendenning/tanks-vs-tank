# Tread Shred store conversion tests (prepared 2026-11-01)

All frames come from genuine gameplay: `../1.9.0/clips/trailer.mp4` (4.0-17.0 s) and the 1.9.0 store screenshots.
Captions use the game's typeface (Black Ops One) in its title gold, in a band above the image so they never cover gameplay.

| Path | Use | Spec |
|---|---|---|
| `google-play/phone/*.png` | Google Play store listing experiment: one variant against the current screenshots | 1920x1080 PNG, no alpha |
| `app-store/iphone/*.png` | App Store product page optimization treatment (screenshots only: no new build) | 2796x1290 PNG |
| `app-store/header-21x9-3840x1646.mp4` | App Store product page header (iOS 27+), Asset Library standalone submission | 21:9, 30 fps, 13 s, muted |
| `app-store/search-results-3x2-1980x1320.mp4` | App Store search results asset (iOS 27+), Asset Library standalone submission | 3:2, 30 fps, 13 s, muted |

Hypothesis: action first with benefit captions ("BLAST ENEMY TANKS", "NO ADS. JUST TANK BATTLES.") converts better than the
current uncaptioned set, which opens on the menu. Submit only after 1.9.2 is approved on each store, so no test touches its review.
