#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OTA_DIR="$ROOT_DIR/Builds/iOSDevice/ota"
IPA_PATH="${1:-$OTA_DIR/TreadShred.ipa}"
PORT="${PORT:-8765}"
HTTP_LOG="$OTA_DIR/http.log"
TUNNEL_LOG="$OTA_DIR/cloudflared.log"
BUNDLE_ID="com.cglendenning.tanksvstank"
IPA_INFO_DIR=""

if [[ ! -f "$IPA_PATH" ]]; then
  echo "Missing signed IPA: $IPA_PATH" >&2
  exit 1
fi
if ! command -v cloudflared >/dev/null 2>&1; then
  echo "cloudflared is required to publish the OTA link" >&2
  exit 1
fi

mkdir -p "$OTA_DIR"
if [[ "$IPA_PATH" != "$OTA_DIR/TreadShred.ipa" ]]; then
  cp "$IPA_PATH" "$OTA_DIR/TreadShred.ipa"
fi
python3 -m http.server "$PORT" --bind 127.0.0.1 --directory "$OTA_DIR" >"$HTTP_LOG" 2>&1 &
HTTP_PID=$!
cleanup() {
  kill "$HTTP_PID" 2>/dev/null || true
  if [[ -n "${TUNNEL_PID:-}" ]]; then kill "$TUNNEL_PID" 2>/dev/null || true; fi
  if [[ -n "$IPA_INFO_DIR" && -d "$IPA_INFO_DIR" ]]; then rm -rf "$IPA_INFO_DIR"; fi
}
trap cleanup EXIT INT TERM

IPA_INFO_DIR="$(mktemp -d)"
unzip -q -o "$OTA_DIR/TreadShred.ipa" -d "$IPA_INFO_DIR"
APP_INFO_PLIST="$(find "$IPA_INFO_DIR/Payload" -maxdepth 2 -name Info.plist -print -quit)"
if [[ -z "$APP_INFO_PLIST" ]]; then
  echo "Unable to find Info.plist inside the signed IPA" >&2
  exit 1
fi
EXPORTED_BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP_INFO_PLIST")"
BUNDLE_VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$APP_INFO_PLIST")"
if [[ "$EXPORTED_BUNDLE_ID" != "$BUNDLE_ID" ]]; then
  echo "Bundle ID mismatch: expected $BUNDLE_ID, got $EXPORTED_BUNDLE_ID" >&2
  exit 1
fi

cloudflared tunnel --url "http://127.0.0.1:${PORT}" --no-autoupdate >"$TUNNEL_LOG" 2>&1 &
TUNNEL_PID=$!
TUNNEL_URL=""
for _ in $(seq 1 60); do
  TUNNEL_URL="$(rg -o 'https://[a-z0-9-]+\.trycloudflare\.com' "$TUNNEL_LOG" | head -1 || true)"
  [[ -n "$TUNNEL_URL" ]] && break
  sleep 1
done
if [[ -z "$TUNNEL_URL" ]]; then
  echo "Cloudflare tunnel did not provide a public URL. See $TUNNEL_LOG" >&2
  exit 1
fi
TUNNEL_HOST="${TUNNEL_URL#https://}"
TUNNEL_IP=""
for _ in $(seq 1 60); do
  TUNNEL_IP="$(dscacheutil -q host -a name "$TUNNEL_HOST" 2>/dev/null | awk '$1 == "ip_address:" && $2 ~ /^[0-9.]+$/ {print $2; exit}' || true)"
  if [[ -z "$TUNNEL_IP" ]]; then
    TUNNEL_IP="$(curl -k -fsS --max-time 5 -H 'accept: application/dns-json' "https://1.1.1.1/dns-query?name=${TUNNEL_HOST}&type=A" 2>/dev/null | rg -o '"data":"[0-9.]+"' | head -1 | sed 's/.*"data":"//;s/"//' || true)"
  fi
  [[ -n "$TUNNEL_IP" ]] && break
  sleep 1
done
if [[ -z "$TUNNEL_IP" ]]; then
  echo "Cloudflare tunnel hostname did not resolve publicly: $TUNNEL_HOST" >&2
  exit 1
fi

IPA_SIZE="$(stat -f '%z' "$OTA_DIR/TreadShred.ipa")"
cat > "$OTA_DIR/manifest.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>items</key><array><dict>
<key>assets</key><array>
<dict><key>kind</key><string>software-package</string><key>url</key><string>${TUNNEL_URL}/TreadShred.ipa</string><key>size</key><real>$IPA_SIZE</real></dict>
</array>
<key>metadata</key><dict>
<key>bundle-identifier</key><string>${BUNDLE_ID}</string>
<key>bundle-version</key><string>${BUNDLE_VERSION}</string>
<key>kind</key><string>software</string>
<key>title</key><string>Tread Shred</string>
</dict></dict></array>
</dict></plist>
EOF

for URL in "${TUNNEL_URL}/manifest.plist" "${TUNNEL_URL}/TreadShred.ipa"; do
  for _ in $(seq 1 20); do
    if curl --resolve "$TUNNEL_HOST:443:$TUNNEL_IP" --http1.1 -fsS --max-time 60 -o /dev/null "$URL" >/dev/null 2>&1; then break; fi
    sleep 1
  done
  curl --resolve "$TUNNEL_HOST:443:$TUNNEL_IP" --http1.1 -fsS --max-time 60 -o /dev/null "$URL" >/dev/null
done

echo "OTA_URL=itms-services://?action=download-manifest&url=${TUNNEL_URL}/manifest.plist"
echo "MANIFEST_URL=${TUNNEL_URL}/manifest.plist"
echo "IPA_URL=${TUNNEL_URL}/TreadShred.ipa"
echo "SHA256=$(shasum -a 256 "$OTA_DIR/TreadShred.ipa" | awk '{print $1}')"
echo "Serving from $OTA_DIR; keep this process running while installing."
wait "$TUNNEL_PID"
