#!/usr/bin/env bash
# Store and ad video footage for Tread Shred (D-031), recorded from the real game on an iOS simulator.
#
#   scripts/capture-store-video.sh capture <simulator-udid> <TreadShred.app> <clips-dir>
#   scripts/capture-store-video.sh compose <clips-dir> <out-dir>
#   scripts/capture-store-video.sh screenshots <simulator-udid> <TreadShred.app> <WxH> <out-dir>
#   scripts/capture-store-video.sh clips <clips-dir> <out-dir>
#   scripts/capture-store-video.sh value-ad <clips-dir> <opening.mp4> <endcard.jpg> <out-dir>
#
# Each scene runs the game's own store capture mode (TreadShredStoreCaptureMode, enabled through its
# PlayerPrefs key) and is recorded with `simctl io recordVideo`, so every frame is genuine gameplay.
# The key is removed afterwards so the simulator returns to normal play.
# compose cuts the clips into the AppLovin creatives (15 s and 30 s, 16:9 and 9:16), an App Store
# app preview (1920x886, real capture only, under 30 s) and a Google Play promo video, with the game's
# music. TRAILER_IN sets where the action starts in trailer.mp4 (seconds after the countdown).
# clips cuts the in-game sales clips (D-034, R-SALES-CLIPS) from captures of modes 18-22: 5 s, 640x360,
# silent, centred 16:9. CLIP_STARTS sets where each clip starts in its capture, as name:seconds.
# value-ad cuts the value-equation creative (P-6, D-031-AC-09) in portrait, the shape AppLovin accepts:
# the dream outcome, the time delay cut (a screen recording of the real first-minute opening; OPENING_IN
# is where GET READY appears in it), the first win, the cost removed, then the end card image.
set -euo pipefail

BUNDLE_ID="com.tgts.tanksvstank"
KEY="TreadShredStoreCaptureMode"
# mode:seconds:name — modes are the constants in TreadShredStoreCaptureMode.cs.
read -r -a SCENES <<<"${CAPTURE_SCENES:-17:40:trailer 11:7:upgrades 6:6:mission-select}"

capture() {
  local sim="$1" app="$2" out="$3"
  mkdir -p "$out"
  xcrun simctl boot "$sim" 2>/dev/null || true
  xcrun simctl bootstatus "$sim" -b >/dev/null
  # A fresh install each run: capture modes change upgrade levels and boosts.
  xcrun simctl uninstall "$sim" "$BUNDLE_ID" 2>/dev/null || true
  xcrun simctl install "$sim" "$app"
  local prefs
  prefs="$(xcrun simctl get_app_container "$sim" "$BUNDLE_ID" data)/Library/Preferences/$BUNDLE_ID"
  for scene in "${SCENES[@]}"; do
    IFS=: read -r mode seconds name <<<"$scene"
    xcrun simctl terminate "$sim" "$BUNDLE_ID" 2>/dev/null || true
    xcrun simctl spawn "$sim" defaults write "$prefs" "$KEY" -int "$mode"
    xcrun simctl launch "$sim" "$BUNDLE_ID" >/dev/null
    sleep 8   # scene load and the capture driver's setup
    xcrun simctl io "$sim" recordVideo --codec=h264 --force "$out/$name.mp4" >/dev/null 2>&1 &
    local recorder=$!
    sleep "$seconds"
    kill -INT "$recorder"
    wait "$recorder" 2>/dev/null || true
    echo "recorded $name ($seconds s)"
  done
  xcrun simctl terminate "$sim" "$BUNDLE_ID" 2>/dev/null || true
  xcrun simctl spawn "$sim" defaults delete "$prefs" "$KEY" 2>/dev/null || true
}

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
FONT="$ROOT/Assets/Art/BlackOpsOne-Regular.ttf"
MUSIC="$ROOT/Assets/Music/InGame.mp3"
ENCODE=(-c:v libx264 -profile:v high -pix_fmt yuv420p -r 30 -crf 18 -preset medium)

add() { awk "BEGIN { print $1 + $2 }"; }

# One segment at W x H over a blurred fill of the same footage. "fit" shows the whole game frame
# (menus, HUD); "zoom" fills more of a portrait frame with the action. Captions sit only in the empty
# band beside the game frame, never over it, sized to fit both the band and the width.
segment() {
  local src="$1" start="$2" dur="$3" caption="$4" w="$5" h="$6" mode="$7" out="$8" fg fh text="" size band y
  if [ "$mode" = zoom ] && [ "$h" -gt "$w" ]; then
    # Shifted right so the HUD at the frame edges is out of view, not half cut.
    fh=840; fg="scale=-2:$fh,crop=$w:$fh:(iw-$w)/2+70:0"
  elif [ "$mode" = fill ]; then
    fh="$h"; fg="scale=$w:$h:force_original_aspect_ratio=increase,crop=$w:$h"
  else
    fh=$(( (w * 1320 / 2868) / 2 * 2 )); fg="scale=$w:$fh"
  fi
  if [ -n "$caption" ]; then
    band=$(( (h - fh) / 2 ))
    size=$(( w * 92 / (${#caption} * 74) ))
    [ "$size" -gt $(( band * 62 / 100 )) ] && size=$(( band * 62 / 100 ))
    [ "$size" -gt $(( w / 14 )) ] && size=$(( w / 14 ))
    # Landscape bands are thin, so the caption sits in the lower one; portrait uses the upper band.
    if [ "$w" -gt "$h" ]; then y="h-$band+($band-text_h)/2"; else y="($band-text_h)/2"; fi
    text=",drawtext=fontfile=$FONT:text='$caption':fontcolor=white:borderw=5:bordercolor=black@0.85:fontsize=$size:x=(w-text_w)/2:y=$y"
  fi
  ffmpeg -loglevel error -y -ss "$start" -t "$dur" -i "$src" -filter_complex \
    "[0:v]fps=30,split[a][b];[a]scale=$w:$h:force_original_aspect_ratio=increase,crop=$w:$h,boxblur=24:2,eq=brightness=-0.22[bg];[b]$fg[fg];[bg][fg]overlay=(W-w)/2:(H-h)/2,setsar=1$text[v]" \
    -map "[v]" -an "${ENCODE[@]}" "$out"
}

end_card() {
  local w="$1" h="$2" out="$3"
  ffmpeg -loglevel error -y -f lavfi -i "color=c=0x04090C:s=${w}x${h}:d=3:r=30" -vf \
    "drawtext=fontfile=$FONT:text='TREAD SHRED':fontcolor=white:fontsize=$((w / 9)):x=(w-text_w)/2:y=h*0.38,drawtext=fontfile=$FONT:text='PLAY FREE':fontcolor=0xFF5C14:fontsize=$((w / 18)):x=(w-text_w)/2:y=h*0.56,setsar=1" \
    -an "${ENCODE[@]}" "$out"
}

# Joins segments, lays the game's music under them with a fade out, and writes an MP4 ready to upload.
assemble() {
  local out="$1"; shift
  local list total
  list="$(mktemp)"
  total=0
  for part in "$@"; do
    echo "file '$part'" >>"$list"
    total="$(add "$total" "$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$part")")"
  done
  ffmpeg -loglevel error -y -f concat -safe 0 -i "$list" -stream_loop -1 -i "$MUSIC" \
    -filter_complex "[1:a]atrim=0:$total,afade=t=out:st=$(add "$total" -1.5):d=1.5,volume=0.8[a]" \
    -map 0:v -map "[a]" -c:v copy -c:a aac -b:a 128k -ar 44100 -ac 2 -shortest -movflags +faststart "$out"
  rm -f "$list"
  echo "$(basename "$out"): $(ffprobe -v error -show_entries stream=width,height -show_entries format=duration -of csv=p=0 "$out" | tr '\n' ' ')"
}

# A still image held for a segment (the end card with the store badges).
image_segment() {
  local img="$1" dur="$2" w="$3" h="$4" out="$5"
  ffmpeg -loglevel error -y -loop 1 -t "$dur" -i "$img" -vf "fps=30,scale=$w:$h,setsar=1" -an "${ENCODE[@]}" "$out"
}

value_ad() {
  local clips="$1" opening="$2" endcard="$3" out="$4" at="${OPENING_IN:-9}" work w=1080 h=1920
  mkdir -p "$out"
  work="$(mktemp -d)"
  # The opening is a 2400x1080 phone recording: cropped to the trailer's 2868:1320 frame so "fit" keeps its shape.
  ffmpeg -loglevel error -y -i "$opening" -vf "crop=2346:1080:27:0,fps=30" -an "${ENCODE[@]}" "$work/opening.mp4"
  segment "$clips/trailer.mp4" 5.6 3 "BECOME UNSTOPPABLE" $w $h zoom "$work/dream-15.mp4"
  segment "$work/opening.mp4" "$at" 4 "IN BATTLE IN SECONDS" $w $h fit "$work/time-15.mp4"
  segment "$clips/trailer.mp4" 13.6 3 "WIN YOUR FIRST BATTLE" $w $h zoom "$work/win-15.mp4"
  segment "$clips/trailer.mp4" 9 2 "FREE. NO ADS." $w $h zoom "$work/free-15.mp4"
  image_segment "$endcard" 3 $w $h "$work/end-15.mp4"
  assemble "$out/ad-value-15s-9x16.mp4" "$work/dream-15.mp4" "$work/time-15.mp4" "$work/win-15.mp4" "$work/free-15.mp4" "$work/end-15.mp4"
  segment "$clips/trailer.mp4" 5 4 "BECOME UNSTOPPABLE" $w $h zoom "$work/dream-30.mp4"
  segment "$work/opening.mp4" "$(add "$at" -0.4)" 5 "IN BATTLE IN SECONDS" $w $h fit "$work/time-30.mp4"
  segment "$clips/trailer.mp4" 13 4 "WIN YOUR FIRST BATTLE" $w $h zoom "$work/win-30.mp4"
  segment "$clips/trailer.mp4" 9 3 "FREE. NO ADS." $w $h zoom "$work/free-30.mp4"
  segment "$clips/upgrades.mp4" 0.5 5 "UPGRADES THAT LAST" $w $h fit "$work/upgrades-30.mp4"
  segment "$clips/mission-select.mp4" 0.4 5 "40 MISSIONS. THREE TIERS." $w $h fit "$work/tiers-30.mp4"
  image_segment "$endcard" 4 $w $h "$work/end-30.mp4"
  assemble "$out/ad-value-30s-9x16.mp4" "$work/dream-30.mp4" "$work/time-30.mp4" "$work/win-30.mp4" "$work/free-30.mp4" \
    "$work/upgrades-30.mp4" "$work/tiers-30.mp4" "$work/end-30.mp4"
  rm -rf "$work"
}

compose() {
  local clips="$1" out="$2" in="${TRAILER_IN:-2}" work
  mkdir -p "$out"
  work="$(mktemp -d)"
  for shape in "landscape 1920 1080" "portrait 1080 1920"; do
    read -r name w h <<<"$shape"
    segment "$clips/trailer.mp4" "$in" 9 "DESTROY EVERY HOSTILE" "$w" "$h" zoom "$work/$name-hook.mp4"
    segment "$clips/trailer.mp4" "$(add "$in" 9)" 11 "" "$w" "$h" zoom "$work/$name-action.mp4"
    segment "$clips/upgrades.mp4" 0.5 4 "EVERY WIN UPGRADES YOUR TANK" "$w" "$h" fit "$work/$name-upgrades.mp4"
    segment "$clips/upgrades.mp4" 0.5 3 "EVERY WIN UPGRADES YOUR TANK" "$w" "$h" fit "$work/$name-upgrades-short.mp4"
    segment "$clips/mission-select.mp4" 0.5 3 "40 MISSIONS. THREE TIERS." "$w" "$h" fit "$work/$name-tiers.mp4"
    end_card "$w" "$h" "$work/$name-end.mp4"
    assemble "$out/ad-30s-$name.mp4" "$work/$name-hook.mp4" "$work/$name-action.mp4" "$work/$name-upgrades.mp4" "$work/$name-tiers.mp4" "$work/$name-end.mp4"
    assemble "$out/ad-15s-$name.mp4" "$work/$name-hook.mp4" "$work/$name-upgrades-short.mp4" "$work/$name-end.mp4"
  done
  # App Store app preview: genuine capture only, no captions or end card, under 30 seconds.
  segment "$clips/trailer.mp4" "$in" 22 "" 1920 886 fill "$work/preview-action.mp4"
  segment "$clips/upgrades.mp4" 0.5 4 "" 1920 886 fill "$work/preview-upgrades.mp4"
  segment "$clips/mission-select.mp4" 0.5 3 "" 1920 886 fill "$work/preview-tiers.mp4"
  assemble "$out/app-store-preview-1920x886.mp4" "$work/preview-action.mp4" "$work/preview-upgrades.mp4" "$work/preview-tiers.mp4"
  cp "$out/ad-30s-landscape.mp4" "$out/play-promo-1920x1080.mp4"
  rm -rf "$work"
}

# Store screenshots at an accepted App Store size: the start screen, two combat moments and the result
# screen from the trailer capture, then upgrades and mission select. All are genuine frames of the build.
screenshots() {
  local sim="$1" app="$2" size="$3" out="$4" prefs
  local w="${size%x*}" h="${size#*x}"
  mkdir -p "$out"
  xcrun simctl boot "$sim" 2>/dev/null || true
  xcrun simctl bootstatus "$sim" -b >/dev/null
  xcrun simctl install "$sim" "$app"
  prefs="$(xcrun simctl get_app_container "$sim" "$BUNDLE_ID" data)/Library/Preferences/$BUNDLE_ID"
  shot() {
    xcrun simctl io "$sim" screenshot "$out/raw-$1.png" >/dev/null 2>&1
    ffmpeg -loglevel error -y -i "$out/raw-$1.png" -vf "scale=$w:$h:force_original_aspect_ratio=increase,crop=$w:$h" "$out/$1.png"
    rm -f "$out/raw-$1.png"
  }
  launch() {
    xcrun simctl terminate "$sim" "$BUNDLE_ID" 2>/dev/null || true
    if [ "$1" = 0 ]; then xcrun simctl spawn "$sim" defaults delete "$prefs" "$KEY" 2>/dev/null || true
    else xcrun simctl spawn "$sim" defaults write "$prefs" "$KEY" -int "$1"; fi
    xcrun simctl launch "$sim" "$BUNDLE_ID" >/dev/null
  }
  launch 0;  sleep 8;  shot 1-start
  launch 17; sleep 14; shot 2-combat; sleep 3; shot 3-combat; sleep 13; shot 4-mission-clear
  launch 11; sleep 9;  shot 5-upgrades
  launch 6;  sleep 9;  shot 6-missions
  xcrun simctl terminate "$sim" "$BUNDLE_ID" 2>/dev/null || true
  xcrun simctl spawn "$sim" defaults delete "$prefs" "$KEY" 2>/dev/null || true
  echo "screenshots ($size): $(ls "$out" | tr '\n' ' ')"
}

clips() {
  local in="$1" out="$2" starts name start
  read -r -a starts <<<"${CLIP_STARTS:-overdrive:2 armored-redeploy:2 armor:2 firepower:2 speed:2}"
  mkdir -p "$out"
  for entry in "${starts[@]}"; do
    IFS=: read -r name start <<<"$entry"
    ffmpeg -loglevel error -y -ss "$start" -t 5 -i "$in/$name.mp4" \
      -vf "crop=ih*16/9:ih,scale=640:360:flags=lanczos,fps=30,setsar=1" -an \
      -c:v libx264 -profile:v main -pix_fmt yuv420p -crf 25 -preset slow -movflags +faststart "$out/$name.mp4"
    echo "$name.mp4: $(ffprobe -v error -show_entries format=duration,size -of csv=p=0 "$out/$name.mp4")"
  done
}

case "${1:-}" in
  capture) [ $# -eq 4 ] || { echo "usage: $0 capture <simulator-udid> <app> <clips-dir>" >&2; exit 2; }; capture "$2" "$3" "$4" ;;
  compose) [ $# -eq 3 ] || { echo "usage: $0 compose <clips-dir> <out-dir>" >&2; exit 2; }; compose "$2" "$3" ;;
  clips) [ $# -eq 3 ] || { echo "usage: $0 clips <clips-dir> <out-dir>" >&2; exit 2; }; clips "$2" "$3" ;;
  value-ad) [ $# -eq 5 ] || { echo "usage: $0 value-ad <clips-dir> <opening.mp4> <endcard.jpg> <out-dir>" >&2; exit 2; }; value_ad "$2" "$3" "$4" "$5" ;;
  screenshots) [ $# -eq 5 ] || { echo "usage: $0 screenshots <simulator-udid> <app> <WxH> <out-dir>" >&2; exit 2; }; screenshots "$2" "$3" "$4" "$5" ;;
  *) sed -n '2,16p' "$0" >&2; exit 2 ;;
esac
