#!/usr/bin/env python3
"""Bind the exact installed Windows EXE, shipped portable EXE and tested Setup EXE.

Evidence here is from a real installer-run smoke and already-captured actual
WinForms screenshots; a hash cannot establish executable signing or audible SPL.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path

HEX40 = re.compile(r"^[0-9a-fA-F]{40}$")
HEX64 = re.compile(r"^[0-9a-f]{64}$")
PORTABLE = re.compile(r"^LouderMe-Portable-v(?P<version>\d+\.\d+\.\d+)\.exe$")
INSTALLER = re.compile(r"^LouderMe-Setup-v(?P<version>\d+\.\d+\.\d+)\.exe$")


def hash_file(path: Path) -> str:
    if not path.is_file() or path.stat().st_size < 1:
        raise ValueError("Required candidate Windows binary is missing/empty")
    with path.open("rb") as data:
        digest = hashlib.sha256()
        for chunk in iter(lambda: data.read(1024 * 1024), b""):
            digest.update(chunk)
        return digest.hexdigest()


def check_sidecar(binary: Path, digest: str) -> None:
    sidecar = Path(str(binary) + ".sha256")
    try:
        content = sidecar.read_text(encoding="ascii").strip()
    except (OSError, UnicodeError) as error:
        raise ValueError(f"Missing or invalid checksum sidecar for {binary.name}") from error
    if content != f"{digest}  {binary.name}":
        raise ValueError(f"Checksum sidecar does not identify actual {binary.name} bytes")


def verify(installed: Path, portable: Path, setup: Path, manifest: Path,
           source_sha: str, output: Path) -> dict:
    if not HEX40.fullmatch(source_sha):
        raise ValueError("Exact forty-character source SHA is required")
    if installed.name != "LouderMe.exe":
        raise ValueError("The installed candidate must be the actual LouderMe.exe")
    portable_version = PORTABLE.fullmatch(portable.name)
    setup_version = INSTALLER.fullmatch(setup.name)
    if not portable_version or not setup_version or (
        portable_version["version"] != setup_version["version"]
    ):
        raise ValueError("Setup and portable binary must share one final semantic version")

    installed_hash = hash_file(installed)
    portable_hash = hash_file(portable)
    setup_hash = hash_file(setup)
    check_sidecar(portable, portable_hash)
    check_sidecar(setup, setup_hash)
    if installed_hash != portable_hash:
        raise ValueError("The actually installed and visually captured EXE differs from shipped portable EXE")
    if setup_hash == portable_hash:
        raise ValueError("Setup and portable are suspiciously identical binaries")

    try:
        visual = json.loads(manifest.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise ValueError("Windows live-capture manifest missing/invalid") from error
    if not isinstance(visual, dict) or visual.get("schema") != "michelslab-rendered-ui-v1" or (
        visual.get("source_commit", "").lower() != source_sha.lower()
    ):
        raise ValueError("Windows screenshot manifest does not identify the candidate commit")
    if visual.get("artifact_sha256", {}).get("windows-desktop") != installed_hash:
        raise ValueError("Visual manifest captured a DIFFERENT Windows executable")
    images = visual.get("screenshots")
    if not isinstance(images, list) or len(images) != 6:
        raise ValueError("Six actual installed WinForms screenshots are mandatory")
    coverage = set()
    for shot in images:
        if not isinstance(shot, dict) or (
            shot.get("platform") != "windows-desktop" or
            shot.get("capture_method") != "running-desktop-app" or
            shot.get("candidate_artifact_sha256") != installed_hash
        ):
            raise ValueError("Screenshot was not captured from exact installed Windows binary")
        viewport, surface = shot.get("viewport"), shot.get("surface")
        if viewport not in {"compact", "wide"} or surface not in {"home", "about", "equalizer"}:
            raise ValueError("Unsupported or missing installed screenshot viewport/surface")
        key = (viewport, surface)
        if key in coverage:
            raise ValueError("Duplicate WinForms screenshot replaces actual missing surface")
        coverage.add(key)
    if coverage != {(v, s) for v in ("compact", "wide") for s in ("home", "about", "equalizer")}:
        raise ValueError("Missing full compact/wide installed Home/About/EQ coverage")

    result = {
        "schema": "louderme-windows-shipping-provenance-v1",
        "source_commit": source_sha.lower(),
        "version": portable_version["version"],
        "installed_executable_sha256": installed_hash,
        "shipped_portable_sha256": portable_hash,
        "tested_installer_sha256": setup_hash,
        "installed_executable_matches_portable": True,
        "installed_executable_verified_by": "actual tested Inno Setup installation",
        "screenshot_manifest_sha256": hash_file(manifest),
        "screenshots": 6,
        "automated_binary_and_visual_identity": "PASS",
        "windows_executable_signature": "NOT CHECKED",
        "post_release_owner_visual_review": "PENDING",
        "physical_audio_device_test": "NOT TESTED",
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--installed-exe", required=True, type=Path)
    parser.add_argument("--portable-exe", required=True, type=Path)
    parser.add_argument("--installer-exe", required=True, type=Path)
    parser.add_argument("--visual-manifest", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    report = verify(args.installed_exe, args.portable_exe, args.installer_exe,
                    args.visual_manifest, args.source_sha, args.output)
    print("PASS: installed Windows screenshot EXE matches published Portable byte for byte;")
    print("      tested Setup hash and both sidecars bound to original source SHA.")
    print("      Signature, human review, and physical acoustic tests: NOT CERTIFIED.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
