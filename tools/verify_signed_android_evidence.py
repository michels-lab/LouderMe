#!/usr/bin/env python3
"""Ensure real emulator screenshots and source SHA identify the FINAL signed APK.

Run AFTER visual_release_gate.py (image integrity and pixel coverage). This
extra gate binds actual final signed file bytes, image bytes and the source
commit to one candidate. It never claims acoustic or owner UI acceptance.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def verify(manifest: Path, signed_apk: Path, source_sha: str) -> None:
    if not re.fullmatch(r"[0-9a-fA-F]{40}", source_sha):
        raise ValueError("Full exact Android release source commit is mandatory")
    if not signed_apk.is_file() or signed_apk.stat().st_size < 500_000 or (
        not re.fullmatch(r"LouderMe-v\d+\.\d+\.\d+-sideload\.apk", signed_apk.name)
    ):
        raise ValueError("Final release-signed LouderMe APK is missing or unexpected")
    data = json.loads(manifest.read_text(encoding="utf-8"))
    if data.get("schema") != "michelslab-rendered-ui-v1" or (
        data.get("source_commit", "").lower() != source_sha.lower()
    ):
        raise ValueError("Signed APK screenshots do not match the release source SHA/schema")
    actual = digest(signed_apk)
    if data.get("artifact_sha256", {}).get("android") != actual:
        raise ValueError("Installed screenshot evidence does not identify FINAL signed APK")

    shots = data.get("screenshots")
    if not isinstance(shots, list) or len(shots) != 4:
        raise ValueError("Exactly four signed-APK Home/About screenshots required")
    coverage = set()
    base = manifest.parent.resolve()
    for shot in shots:
        if not isinstance(shot, dict) or shot.get("platform") != "android" or (
            shot.get("capture_method") not in
            ("installed-android-emulator", "installed-android-device")
        ) or shot.get("candidate_artifact_sha256") != actual:
            raise ValueError("Screenshot is from a different/uninstalled Android APK")
        viewport, surface = shot.get("viewport"), shot.get("surface")
        if viewport not in ("compact", "wide") or surface not in ("home", "about"):
            raise ValueError("Unexpected signed Android screenshot surface/viewport")
        if (viewport, surface) in coverage:
            raise ValueError("Repeated signed screenshot silently replaces a missing surface")
        coverage.add((viewport, surface))
        rel = shot.get("path")
        if not isinstance(rel, str) or not rel:
            raise ValueError("Signed screenshot filename is missing")
        path = (base / rel).resolve()
        if not path.is_relative_to(base) or not path.is_file():
            raise ValueError("Signed screenshot escaped evidence bundle or was deleted")
        if digest(path) != shot.get("sha256"):
            raise ValueError("Signed-APK screenshot bytes were altered after capture")
    if coverage != {("compact", "home"), ("compact", "about"),
                    ("wide", "home"), ("wide", "about")}:
        raise ValueError("Missing signed APK Home/About screenshot at a target viewport")
    print("PASS: Exact FINAL signed Android APK, source SHA and all four original screenshot bytes match.")
    print("OWNER VISUAL REVIEW and PHYSICAL AUDIO TEST remain separate/unverified.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--signed-apk", type=Path, required=True)
    parser.add_argument("--source-sha", required=True)
    args = parser.parse_args()
    verify(args.manifest, args.signed_apk, args.source_sha)
