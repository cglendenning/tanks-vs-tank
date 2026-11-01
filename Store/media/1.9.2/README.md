# Tread Shred 1.9.2 store listing media (2026-11-01)

The store listing on both stores, chosen for conversion (no A/B test, by Craig's decision). All frames come from
genuine gameplay: `../1.9.0/clips/trailer.mp4` (4.0-17.0 s) and the 1.9.0 store screenshots. Captions use the game's
typeface (Black Ops One) in its title gold, in a band above the image so they never cover gameplay; the set opens on
combat instead of the menu, and every caption states something true of the game.

| Path | Use | Spec | Written by |
|---|---|---|---|
| `google-play/phone/*.png` | Play phone screenshots | 1920x1080 PNG, no alpha | `play-console.py listing` / `release` |
| `google-play/tablet/*.png` | Play 7- and 10-inch screenshots (same set) | 1920x1080 PNG | `play-console.py listing` / `release` |
| `google-play/feature-graphic-1024x500.png` | Play feature graphic; also the promo video's thumbnail, so the centre stays clear for Google's play button | 1024x500 PNG | `play-console.py listing` / `release` |
| `app-store/iphone/*.png` | App Store iPhone 6.7-inch screenshots | 2796x1290 PNG | `app-store-connect.py media` |
| `app-store/ipad/*.png` | App Store iPad 12.9-inch screenshots | 2732x2048 PNG | `app-store-connect.py media` |
| `app-store/header-21x9-3840x1646.mp4` | App Store product page header (iOS 27+), Asset Library | 21:9, 30 fps, 13 s, muted | `app-store-connect.py creative-assets` |
| `app-store/search-results-3x2-1980x1320.mp4` | App Store search results asset (iOS 27+), Asset Library | 3:2, 30 fps, 13 s, muted | `app-store-connect.py creative-assets` |

The Google Play promo video is `../1.9.0/creatives/play-promo-1920x1080.mp4` on YouTube (`PROMO_VIDEO` in
`scripts/play-console.py`), with `../1.9.0/creatives/youtube-thumbnail-1280x720.jpg` as its thumbnail.
