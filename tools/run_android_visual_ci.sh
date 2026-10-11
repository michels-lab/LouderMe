#!/usr/bin/env bash
set -euo pipefail
: "${GITHUB_SHA:?Exact source commit required}"
APK="$(find visual-input -type f -name '*sideload-debug.apk' -print -quit)"
test -n "$APK" && test -s "$APK"
python3 tools/capture_android_visual.py --apk "$APK" \
  --source-sha "$GITHUB_SHA" --out artifacts/visual/android
