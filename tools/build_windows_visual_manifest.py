#!/usr/bin/env python3
"""Bind real running LouderMe WinForms screenshots to the built executable SHA.

This tool never generates images and accepts only the four output PNGs created
by the actual installed/published Windows executable --visual-evidence mode.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from PIL import Image


def hash_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def make_manifest(exe: Path, source_sha: str, output: Path) -> Path:
    if not re.fullmatch(r"[0-9a-fA-F]{40}", source_sha):
        raise ValueError("Invalid exact source commit")
    if not exe.is_file() or exe.stat().st_size < 500_000:
        raise ValueError("Built Windows candidate executable missing")
    digest = hash_file(exe)
    shots = []
    for viewport, dims in (("compact", (600, 520)), ("wide", (900, 700))):
        for surface in ("home", "about"):
            path = output / f"{surface}-{viewport}.png"
            if not path.is_file():
                raise ValueError(f"Missing actual running-form screenshot: {path}")
            with Image.open(path) as image:
                image.verify()
            with Image.open(path) as image:
                if image.size != dims:
                    raise ValueError(f"Wrong {viewport} real WinForms dimensions: {image.size}")
            shots.append({
                "platform": "windows-desktop", "surface": surface,
                "viewport": viewport, "path": path.name,
                "width": dims[0], "height": dims[1], "sha256": hash_file(path),
                "capture_method": "running-desktop-app",
                "candidate_artifact_sha256": digest,
            })
    manifest = {
        "schema": "michelslab-rendered-ui-v1",
        "source_commit": source_sha,
        "artifact_sha256": {"windows-desktop": digest},
        "screenshots": shots,
        "human_visual_review": {"decision": "PENDING", "reviewer": None},
        "physical_device_acceptance": "NOT TESTED",
    }
    output.mkdir(parents=True, exist_ok=True)
    target = output / "manifest.json"
    target.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print("Windows real WinForms Home/About image bytes bound to exact executable SHA.")
    return target


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    make_manifest(args.exe, args.source_sha, args.output)
