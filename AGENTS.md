# Tread Shred agent notes

## Specification (source of truth)

- The build contract lives in the private repository `../../treadshred-spec`
  (GitHub `cglendenning/treadshred-spec`). `TREAD_SHRED_SPECIFICATION.md` is
  the only source of truth; its HTML, index and manifest are generated.
- Read `SPEC_INDEX.md`, then `python3 spec_tools.py brief D-###` for the
  directive you are working on. Never load the whole specification into
  context.
- **Never change the specification or any existing test (assertions, data,
  structure, or deletion) without explicit approval from Craig in the current
  conversation.** Propose the change and wait. Adding new tests is allowed.
- Every Unity test names the directive it enforces as the first token of its
  description: `[Test(Description = "D-011-AC-01: ...")]`. Validate with
  `python3 spec_tools.py validate --app-root ../games/tread-shred` from the
  spec repository; it fails on any unlabeled test.

## Think first, run once (owner rule)

Waiting on Unity, simulators, emulators and phones is the slowest part of this project. Before any
Unity batch run, simulator or emulator session, device install or script run:

- Predict the outcome from static analysis first: read the code paths involved, the scene, prefab and
  settings data, existing logs and earlier results. Run only to confirm a prediction, never to explore.
- Gather everything one run should prove in that single run (all log lines, screenshots and assertions
  needed), so a run is never repeated just to see one more thing.
- Check the preconditions that make a run fail fast before starting it: device authorized and awake,
  correct tool paths, scenes and modes that exist, tags and components the code relies on.
- Prefer the fastest loop that answers the question: static reading, then filtered EditMode tests,
  then the PlayMode screen sweep, and only then simulators, emulators or devices.
- For helper tooling, prefer shell with `jq` or a compiled tool over an interpreter when the work is
  simple; reach for Python only when nothing lighter fits.

## Unity editor and build tooling

- The Unity editor is already installed on this Mac. Never download, install,
  reinstall, or update a Unity editor as an automatic fallback.
- Unity Hub's editor inventory may be wrong. Reuse the known working editor
  directly at:
  `/Users/craig/Unity/Hub/Editor/6000.3.27f2/Unity.app/Contents/MacOS/Unity`
  (6000.3.27f2, the 6.3 LTS patch the project moved to on 2026-11-01 with Craig's authorization).
  6000.3.24f1 stays installed at `/Users/craig/Unity/Hub/Editor/6000.3.24f1-arm64/` as the rollback and
  for `/Users/craig/superhero-game`.
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
  and gem pack product IDs before each build. Never reuse another app's
  bundle ID or profile.
- The game has no ads. Monetization is consumable gem packs through Unity IAP;
  device-test purchases must use an Apple sandbox account.
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
  `scripts/build-tank-release.sh` with the Tread Shred signing/profile,
  validate the IPA, run
  `PORT=<unique-port> scripts/serve-tank-ota.sh`, verify public HTTP 200 for
  both manifest and IPA, then return the OTA URL, direct IPA URL, version, and
  SHA-256. Do not report completion before all of those checks pass.
- Clean up only old Tread Shred staging directories after the active OTA
  session ends. Do not recursively delete broad workspace paths or anything
  belonging to another app.
