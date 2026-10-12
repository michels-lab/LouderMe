#!/usr/bin/env python3
"""Ensure all installed-emulator evidence refers to the final signed APK bytes."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


def verify(manifest: Path, signed_apk: Path) -> None:
    if not signed_apk.is_file():
        raise ValueError("Final release-signed LouderMe APK is missing")
    data = json.loads(manifest.read_text(encoding="utf-8"))
    actual = hashlib.sha256(signed_apk.read_bytes()).hexdigest()
    if data.get("artifact_sha256", {}).get("android") != actual:
        raise ValueError("Installed screenshot evidence does not identify FINAL signed APK")
    shots = data.get("screenshots", [])
    if len(shots) < 4:
        raise ValueError("Missing signed-APK Home/About captures for two viewports")
    if any(shot.get("candidate_artifact_sha256") != actual for shot in shots):
        raise ValueError("One or more screenshots belong to a different APK")
    if len({(shot.get("viewport"), shot.get("surface")) for shot in shots}) < 4:
        raise ValueError("Duplicate/missing signed-APK capture surfaces")
    print("PASS: Installed Android release-APK screenshots match FINAL signed APK SHA-256")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--signed-apk", type=Path, required=True)
    args = parser.parse_args()
    verify(args.manifest, args.signed_apk)
