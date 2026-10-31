#!/usr/bin/env bash
# D-029-AC-05: no store release while the game fails the specification's
# compliance check. Store release scripts run this before building.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SPEC_ROOT="${TREAD_SHRED_SPEC_ROOT:-$HOME/treadshred-spec}"

if [ ! -f "$SPEC_ROOT/spec_tools.py" ]; then
  echo "Release blocked: specification repository not found at $SPEC_ROOT (set TREAD_SHRED_SPEC_ROOT)." >&2
  exit 1
fi
# D-029-AC-06: post-launch criteria (met only once the game is live) do not block a release.
if ! python3 "$SPEC_ROOT/spec_tools.py" compliance --release --app-root "$ROOT_DIR"; then
  echo "Release blocked: the game does not meet the specification (D-029). Run spec_tools.py compliance for details." >&2
  exit 1
fi
