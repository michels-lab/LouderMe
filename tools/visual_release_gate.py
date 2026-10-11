#!/usr/bin/env python3
"""Fail-closed rendered-UI evidence check for Michel's Lab shipping candidates.

This verifies image integrity, coverage and source/artifact binding before publication.
Owner review is post-release; optional review validation never authorizes publication.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

from PIL import Image, ImageStat, UnidentifiedImageError

HEX = re.compile(r"^[0-9a-fA-F]{64}$")
SCHEMA = "michelslab-rendered-ui-v1"
BOT = ("bot", "agent", "automation", "copilot", "chatgpt", "gpt-")


def check_evidence(manifest: Path, source_sha: str, required_platforms: list[str],
                   required_surfaces: list[str], require_review: bool = False) -> list[str]:
    errors = []
    if not re.fullmatch(r"[0-9a-fA-F]{40}", source_sha):
        return ["Source SHA must be a full 40-character commit hash"]
    try:
        doc = json.loads(manifest.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        return [f"Cannot read visual evidence manifest: {exc}"]
    if doc.get("schema") != SCHEMA:
        errors.append(f"Invalid evidence schema: expected {SCHEMA}")
    if doc.get("source_commit", "").lower() != source_sha.lower():
        errors.append("Visual evidence is not bound to exact candidate commit")
    artifacts = doc.get("artifact_sha256") or {}
    shots = doc.get("screenshots") or []
    if not isinstance(shots, list) or not shots:
        return errors + ["No rendered-app screenshots supplied"]
    coverage = defaultdict(set)
    viewport_sizes = defaultdict(set)
    hashes = set()
    base = manifest.parent.resolve()
    for platform in required_platforms:
        if not HEX.fullmatch(str(artifacts.get(platform, ""))):
            errors.append(f"Missing real candidate artifact SHA-256 for {platform}")
    for idx, shot in enumerate(shots):
        prefix = f"screenshot[{idx}]"
        if not isinstance(shot, dict):
            errors.append(f"{prefix}: must be an object")
            continue
        platform, surface = shot.get("platform"), shot.get("surface")
        if not isinstance(platform, str) or not isinstance(surface, str):
            errors.append(f"{prefix}: platform/surface missing")
            continue
        if shot.get("capture_method") not in (
            "installed-android-emulator", "installed-android-device",
            "installed-windows-desktop", "running-desktop-app",
            "running-browser", "installed-ios-simulator", "installed-ios-device",
        ):
            errors.append(f"{prefix}: screenshot capture must originate from running app")
        if shot.get("candidate_artifact_sha256") != artifacts.get(platform):
            errors.append(f"{prefix}: screenshot not tied to release artifact digest")
        rel = shot.get("path", "")
        file = (base / rel).resolve() if isinstance(rel, str) else base
        if not isinstance(rel, str) or not rel or not file.is_relative_to(base):
            errors.append(f"{prefix}: invalid/out-of-root screenshot path")
            continue
        try:
            data = file.read_bytes()
            actual = hashlib.sha256(data).hexdigest()
            if actual != str(shot.get("sha256", "")).lower():
                errors.append(f"{prefix}: screenshot checksum mismatch")
            if actual in hashes:
                errors.append(f"{prefix}: duplicate screenshot bytes; likely reused surface/viewport")
            hashes.add(actual)
            if len(data) < 3000:
                errors.append(f"{prefix}: screenshot is implausibly small")
            with Image.open(file) as raw:
                raw.verify()
            with Image.open(file) as raw:
                width, height = raw.size
                if width < 320 or height < 320:
                    errors.append(f"{prefix}: insufficient screenshot dimensions {width}x{height}")
                rgb = raw.convert("RGB")
                sample = rgb.resize((80, 80))
                spread = ImageStat.Stat(sample).stddev
                if max(spread) < 9.0:
                    errors.append(f"{prefix}: visually near-uniform/blank screenshot")
                if (shot.get("width"), shot.get("height")) != (width, height):
                    errors.append(f"{prefix}: declared dimensions differ from actual image")
                viewport_sizes[platform].add((width, height))
        except (OSError, UnidentifiedImageError, ValueError) as exc:
            errors.append(f"{prefix}: image decode/validation failed: {exc}")
            continue
        coverage[platform].add(surface)
    for platform in required_platforms:
        for surface in required_surfaces:
            if surface not in coverage[platform]:
                errors.append(f"{platform}: missing rendered {surface} screenshot")
        if len(viewport_sizes[platform]) < 2:
            errors.append(f"{platform}: at least two distinct viewport-size screenshots required")
    review = doc.get("human_visual_review") or {}
    if review.get("blocking_findings"):
        errors.append("Visual reviewer recorded blocking defects")
    if require_review:
        name = str(review.get("reviewer", "")).strip()
        if review.get("decision") != "approved":
            errors.append("Human visual reviewer has not approved candidate images")
        if not name or any(part in name.lower() for part in BOT):
            errors.append("Human reviewer identity missing or automated")
        if review.get("source_commit", "").lower() != source_sha.lower():
            errors.append("Visual review is stale relative to candidate commit")
        if not review.get("reviewed_at") or not str(review.get("evidence_url", "")).startswith("https://"):
            errors.append("Visual review date or immutable evidence URL is missing")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--platforms", required=True, help="Comma-separated shipped UI platforms")
    parser.add_argument("--surfaces", default="home,about",
                        help="Comma-separated mandatory + changed UI surfaces")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--capture-only", action="store_true",
                      help="Compatibility alias for default automated pre-release validation")
    mode.add_argument("--validate-post-release-review", action="store_true",
                      help="Audit completed owner review after publication; never a publishing prerequisite")
    args = parser.parse_args()
    platforms = [p.strip() for p in args.platforms.split(",") if p.strip()]
    surfaces = [p.strip() for p in args.surfaces.split(",") if p.strip()]
    if not platforms or not {"home", "about"}.issubset(surfaces):
        parser.error("Must declare shipped platforms and include home,about")
    errors = check_evidence(args.manifest, args.source_sha, platforms, surfaces,
                            require_review=args.validate_post_release_review)
    for problem in errors:
        print("::error::" + problem, file=sys.stderr)
    if errors:
        print(f"RENDERED UI RELEASE GATE: FAIL ({len(errors)} issue(s))")
        return 1
    print("RENDERED UI RELEASE GATE: PASS — image integrity/coverage confirmed.")
    print("POST-RELEASE REVIEW: VERIFIED" if args.validate_post_release_review
          else "OWNER REVIEW: separate post-release obligation; not certified by this check.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

