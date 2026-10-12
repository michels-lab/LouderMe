#!/usr/bin/env bash
set -euo pipefail
: "${GITHUB_SHA:?Exact release-source SHA is mandatory}"
APK="$(find signed-input -type f -name 'LouderMe-v*-sideload.apk' -print -quit)"
test -n "$APK" && test -s "$APK"
python3 tools/capture_android_visual.py --apk "$APK" \
  --source-sha "$GITHUB_SHA" --out artifacts/visual/android-signed
