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

## Tread Shred release isolation

- Keep this game's release work completely separate from every other app on
  the Mac. The preferred isolated staging root is
  `/private/tmp/tread-shred-release`; if a clean run is needed, create a
  uniquely named child such as `/private/tmp/tread-shred-release.XXXXXX` and
  keep it alive until the OTA install is complete.
- Build artifacts, exported IPAs, manifests, HTTP logs, and Cloudflare logs
  must stay under that Tread Shred staging copy's
  `Builds/iOSDevice/ota` directory. Never use Green Pyramid, Superhero, or
  another project's `Builds`/OTA directory, and never overwrite their
  artifacts or servers.
- If the live Unity editor locks the checkout, use an isolated project copy
  for the release build (excluding `Library`, `Temp`, `Logs`, `Obj`, `Builds`,
  and `.git`) instead of interrupting the editor or downloading another
  Unity editor. The installed editor path and this repository's build scripts
  are the only required project tooling.
- Allocate a local OTA port per run. Prefer `8877` only when it is free;
  otherwise choose another unused port and record it in the release output.
  Never kill or reuse a server owned by another app. Keep the Tread Shred
  server and tunnel process running until the user has installed the build.
- The repeatable sequence is: prepare the isolated copy, run
  `scripts/build-tank-release.sh` with the Tread Shred signing/profile and
  `TREAD_SHRED_USE_TEST_ADS=1`, validate the IPA, run
  `PORT=<unique-port> scripts/serve-tank-ota.sh`, verify public HTTP 200 for
  both manifest and IPA, then return the OTA URL, direct IPA URL, version, and
  SHA-256. Do not report completion before all of those checks pass.
- Clean up only old Tread Shred staging directories after the active OTA
  session ends. Do not recursively delete broad workspace paths or anything
  belonging to another app.
