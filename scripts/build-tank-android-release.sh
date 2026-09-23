#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/Users/craig/Unity/Hub/Editor/6000.3.24f1-arm64/Unity.app/Contents/MacOS/Unity}"

: "${TREAD_SHRED_ANDROID_KEYSTORE:?Set the Android upload keystore path}"
: "${TREAD_SHRED_ANDROID_KEY_ALIAS:?Set the Android upload key alias}"
: "${TREAD_SHRED_ANDROID_KEYSTORE_PASSWORD:?Set the Android upload keystore password}"
: "${TREAD_SHRED_ANDROID_KEY_PASSWORD:?Set the Android upload key password}"

export TREAD_SHRED_ANDROID_RELEASE=1
mkdir -p "$ROOT_DIR/Builds/Android"
"$UNITY_BIN" -batchmode -quit -nographics -projectPath "$ROOT_DIR" \
  -executeMethod TankBuildAutomation.BuildAndroidBundleFromCommandLine \
  -logFile "$ROOT_DIR/tank-android-release-unity.log"

echo "AAB=$ROOT_DIR/Builds/Android/TreadShred.aab"
