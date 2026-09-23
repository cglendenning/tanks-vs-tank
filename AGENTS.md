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
