# Tread Shred agent notes

## Unity editor and build tooling

- The Unity editor is already installed on this Mac. Never download, install,
  reinstall, or update a Unity editor as an automatic fallback.
- Unity Hub's editor inventory may be wrong. Reuse the known working editor
  directly at:
  `/Users/craig/Unity/Hub/Editor/6000.3.24f1-arm64/Unity.app/Contents/MacOS/Unity`
- Before assuming the editor is unavailable, inspect prior successful logs and
  the reference projects (`/Users/craig/greenpyramid` and
  `/Users/craig/superhero-game`) for the resolved executable and scripts.
- If that exact path ever changes or is genuinely unavailable, stop and report
  the missing path. Do not invoke `unity install`, Unity Hub installation, or
  any editor download without explicit user authorization in the current turn.

## iPhone release IPA and OTA deployment

- For every requested iOS release IPA or device test build, use the reusable
  `ios-release-ota` skill and complete the entire release flow in the same
  turn. A release IPA build is not complete until an OTA install link has been
  published and returned to the user.
- Target Craig's iPhone 12 mini by default. The registered device UDID is
  `00008101-001C4D0818782C3A`; verify that the exported Ad Hoc provisioning
  profile contains it before publishing.
- Before signing, run the known-good CI signing setup:
  `/Users/craig/greenpyramid/scripts/ensure_ci_signing_keychain.sh`
  Do not infer that signing is unavailable from the ambient keychain or from
  `security find-identity` alone. Use the reference workflow from
  `/Users/craig/greenpyramid` and `/Users/craig/superhero-game` when needed.
- Inspect the current bundle identifier, version, export options, profile,
  and ad configuration before each build. Never reuse another app's bundle ID,
  profile, or production AdMob IDs.
- Test-ad mode must remain enabled for every device-test release. Do not ship
  production ad units in an OTA test IPA. Confirm that audio is paused/faded
  before interstitial ads and restored afterward.
- After export, validate all of the following before sharing the link: the IPA
  is Apple Distribution signed, the bundle ID and version match the manifest,
  the Ad Hoc profile contains the target UDID, and the IPA passes a public HTTP
  200 check.
- Publish using the repository OTA helper, keeping the tunnel process alive:
  `PORT=<unused-port> bash scripts/serve-tank-ota.sh <absolute-path-to-ipa>`
  Return the complete `itms-services://` URL automatically, along with the
  direct IPA URL and SHA-256 when available. Tell the user to open the OTA URL
  in Safari on the iPhone and keep the tunnel/session running while installing.
- Never expose private signing keys, passwords, provisioning contents, or
  other credentials in logs or user-facing output. If the release cannot be
  signed after the reference workflow is attempted, report the exact failed
  check and ask only for the missing credential/profile input.
