#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/Users/craig/Unity/Hub/Editor/6000.3.24f1-arm64/Unity.app/Contents/MacOS/Unity}"
IOS_DIR="$ROOT_DIR/Builds/iOSDevice"
ARCHIVE_DIR="$IOS_DIR/archive-appstore"
ARCHIVE_PATH="$ARCHIVE_DIR/TreadShred.xcarchive"
EXPORT_DIR="$IOS_DIR/appstore"
EXPORT_OPTIONS="$IOS_DIR/ExportOptions-AppStore.plist"
IOS_BUNDLE_ID="com.tgts.tanksvstank"

: "${TANK_TEAM_ID:?Set TANK_TEAM_ID to the Apple Developer Team ID}"
: "${TANK_CODE_SIGN_IDENTITY:?Set TANK_CODE_SIGN_IDENTITY to the exact Apple Distribution certificate fingerprint}"
: "${TANK_PROVISIONING_PROFILE_SPECIFIER:?Set TANK_PROVISIONING_PROFILE_SPECIFIER to the App Store profile name or UUID}"

mkdir -p "$ARCHIVE_DIR" "$EXPORT_DIR"
"$UNITY_BIN" -batchmode -quit -nographics -projectPath "$ROOT_DIR" \
  -executeMethod TankBuildAutomation.BuildIosFromCommandLine \
  -logFile "$ROOT_DIR/tank-ios-appstore-unity.log"
pod install --project-directory="$IOS_DIR"

rm -rf "$ARCHIVE_PATH" "$EXPORT_DIR"/TreadShred.app "$EXPORT_DIR"/TreadShred.ipa
xcodebuild -workspace "$IOS_DIR/Unity-iPhone.xcworkspace" \
  -scheme Unity-iPhone -configuration Release -sdk iphoneos \
  -archivePath "$ARCHIVE_PATH" archive

cp "$ROOT_DIR/scripts/ExportOptions-AppStore.plist" "$EXPORT_OPTIONS"
/usr/libexec/PlistBuddy -c "Set :teamID $TANK_TEAM_ID" "$EXPORT_OPTIONS"
/usr/libexec/PlistBuddy -c "Set :signingStyle manual" "$EXPORT_OPTIONS"
/usr/libexec/PlistBuddy -c "Add :signingCertificate string $TANK_CODE_SIGN_IDENTITY" "$EXPORT_OPTIONS"
/usr/libexec/PlistBuddy -c "Add :provisioningProfiles dict" "$EXPORT_OPTIONS"
/usr/libexec/PlistBuddy -c "Add :provisioningProfiles:${IOS_BUNDLE_ID} string $TANK_PROVISIONING_PROFILE_SPECIFIER" "$EXPORT_OPTIONS"
xcodebuild -exportArchive -archivePath "$ARCHIVE_PATH" \
  -exportPath "$EXPORT_DIR" -exportOptionsPlist "$EXPORT_OPTIONS"

mv "$EXPORT_DIR"/*.ipa "$EXPORT_DIR/TreadShred.ipa"
echo "IPA=$EXPORT_DIR/TreadShred.ipa"
