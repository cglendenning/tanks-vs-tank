#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Unity batch logs go in Logs/ (ignored), not the repository root.
mkdir -p "$ROOT_DIR/Logs"
UNITY_BIN="${UNITY_BIN:-/Users/craig/Unity/Hub/Editor/6000.3.27f2/Unity.app/Contents/MacOS/Unity}"

: "${TREAD_SHRED_ANDROID_KEYSTORE:?Set the Android upload keystore path}"
: "${TREAD_SHRED_ANDROID_KEY_ALIAS:?Set the Android upload key alias}"
: "${TREAD_SHRED_ANDROID_KEYSTORE_PASSWORD:?Set the Android upload keystore password}"
: "${TREAD_SHRED_ANDROID_KEY_PASSWORD:?Set the Android upload key password}"

# D-029-AC-05: refuse to build a store release unless the game meets the specification.
"$ROOT_DIR/scripts/spec-compliance-gate.sh"

export TREAD_SHRED_ANDROID_RELEASE=1
mkdir -p "$ROOT_DIR/Builds/Android"
"$UNITY_BIN" -batchmode -quit -nographics -projectPath "$ROOT_DIR" \
  -executeMethod TankBuildAutomation.BuildAndroidBundleFromCommandLine \
  -logFile "$ROOT_DIR/Logs/tank-android-release-unity.log"

echo "AAB=$ROOT_DIR/Builds/Android/TreadShred.aab"
