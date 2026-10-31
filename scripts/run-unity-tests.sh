#!/usr/bin/env bash
# Runs the Unity tests in batch mode and prints a summary with every failure.
#
#   scripts/run-unity-tests.sh [all|editmode|playmode] [--filter NAME] [--screenshots DIR]
#
# "all" (the default) runs both suites at the same time: PlayMode runs in a copy-on-write
# clone of the project (<project>.playmode, synced from Assets, Packages and ProjectSettings
# before each run), because one Unity editor cannot open the same project twice.
# --screenshots DIR also saves every screen the PlayMode sweep audits as a PNG (needs graphics,
# so PlayMode then runs without -nographics).
# The editor's PlayerPrefs for the game are exported first and restored afterwards, so test
# seeding never changes play data. Results and logs land in $RESULTS_DIR (default
# <project>/Logs/tests). Exits non-zero when any test fails or does not run.
set -euo pipefail

UNITY="${UNITY:-/Users/craig/Unity/Hub/Editor/6000.3.24f1-arm64/Unity.app/Contents/MacOS/Unity}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
CLONE="$PROJECT.playmode"
PREFS_DOMAIN="unity.TGT Studios.Tread Shred"

suite="${1:-all}"
[ $# -gt 0 ] && shift
filter=""
screenshots=""
while [ $# -gt 0 ]; do
  case "$1" in
    --filter) filter="$2"; shift 2 ;;
    --screenshots) screenshots="$2"; shift 2 ;;
    *) echo "unknown option: $1" >&2; exit 2 ;;
  esac
done
case "$suite" in all|editmode|playmode) ;; *) echo "unknown suite: $suite" >&2; exit 2 ;; esac
[ -x "$UNITY" ] || { echo "Unity editor not found at $UNITY" >&2; exit 2; }

results="${RESULTS_DIR:-$PROJECT/Logs/tests}"
mkdir -p "$results"

prefs_backup="$results/playerprefs-backup.plist"
had_prefs=0
if defaults export "$PREFS_DOMAIN" "$prefs_backup" 2>/dev/null; then had_prefs=1; fi
restore_prefs() {
  if [ "$had_prefs" = 1 ]; then
    defaults import "$PREFS_DOMAIN" "$prefs_backup"
  else
    defaults delete "$PREFS_DOMAIN" 2>/dev/null || true
  fi
}
trap restore_prefs EXIT

run_suite() {
  local platform="$1" project="$2"
  local xml="$results/$platform-results.xml" log="$results/$platform.log"
  local args=(-batchmode -projectPath "$project" -runTests -testPlatform "$platform" -testResults "$xml" -logFile "$log")
  [ -n "$filter" ] && args+=(-testFilter "$filter")
  rm -f "$xml"
  if [ "$platform" = PlayMode ] && [ -n "$screenshots" ]; then
    mkdir -p "$screenshots"
    TREADSHRED_SCREENSHOTS="$screenshots" "$UNITY" "${args[@]}" || true
  else
    "$UNITY" -nographics "${args[@]}" || true
  fi
}

sync_clone() {
  [ -d "$CLONE" ] || cp -cR "$PROJECT" "$CLONE"
  for dir in Assets Packages ProjectSettings; do
    rsync -a --delete "$PROJECT/$dir/" "$CLONE/$dir/"
  done
}

start=$(date +%s)
platforms=()
case "$suite" in
  editmode) platforms=(EditMode); run_suite EditMode "$PROJECT" ;;
  playmode) platforms=(PlayMode); run_suite PlayMode "$PROJECT" ;;
  all)
    platforms=(EditMode PlayMode)
    sync_clone
    run_suite EditMode "$PROJECT" &
    run_suite PlayMode "$CLONE" &
    wait
    ;;
esac
echo "Unity tests finished in $(( $(date +%s) - start ))s"

python3 - "$results" "${platforms[@]}" <<'EOF'
import os, re, sys
import xml.etree.ElementTree as ET
results, platforms = sys.argv[1], sys.argv[2:]
ok = True
for platform in platforms:
    xml = os.path.join(results, platform + "-results.xml")
    log = os.path.join(results, platform + ".log")
    if not os.path.exists(xml):
        ok = False
        print(f"{platform}: no results (see {log})")
        if os.path.exists(log):
            for line in sorted({l.strip() for l in open(log, errors="replace") if "error CS" in l})[:10]:
                print("  " + line)
        continue
    root = ET.parse(xml).getroot()
    print(f"{platform}: {root.get('passed')} of {root.get('total')} passed, {root.get('failed')} failed")
    for case in root.iter("test-case"):
        if case.get("result") != "Passed":
            ok = False
            message = case.find(".//message")
            text = (message.text or "").strip() if message is not None else ""
            print(f"  FAILED {case.get('classname').split('.')[-1]}.{case.get('name')}")
            for line in text.splitlines()[:40]:
                print("    " + line)
sys.exit(0 if ok else 1)
EOF
