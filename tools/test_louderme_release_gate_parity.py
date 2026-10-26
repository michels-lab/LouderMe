#!/usr/bin/env python3
"""Fail closed when Windows stable-release checks lag behind the tested build.

This is a read-only source and release-gate regression test. It does NOT
pretend to compile an installer or validate physical audio.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / ".github/workflows/build-louderme-desktop.yml"
RELEASE = ROOT / ".github/workflows/release-louderme-desktop.yml"
ABOUT = ROOT / "desktop/src/LouderMeDesktop/AboutForm.cs"
PROJECT = ROOT / "desktop/src/LouderMeDesktop/LouderMeDesktop.csproj"
LOGO_SOURCE = ROOT / "desktop/src/LouderMeDesktop/Assets/MichelsLabCanonicalLockup.png"
EXPECTED_SHA = "d03e8549af2cbcb04c2329029a02f0cd078972ce1b1ff948ef66c3aebe7d10f4"
LEGACY_SHA = "7819ef5c7d1a5c338c67c9bbd517e5448724a5cf"


def check_gates(build: str, release: str, about: str, project: str) -> None:
    for label, script in (("build", build), ("stable release", release)):
        for required in (
            "python tools/test_louderme_desktop_logo.py",
            "MichelsLabCanonicalLockup.png",
            EXPECTED_SHA,
            "MichelsLabDarkLockup",
            "MichelDuarte.jpg",
            "Equalizer APO",
        ):
            if required not in script:
                raise AssertionError(f"{label} omits approved logo/About check: {required}")
        for forbidden in (LEGACY_SHA, "Assets/MichelsLabLockup.png",
                          "'MichelsLabLockup'"):
            if forbidden in script:
                raise AssertionError(f"{label} accepts obsolete or corrupted logo: {forbidden}")
    if "Assets.MichelsLabDarkLockup.png" not in about:
        raise AssertionError("Real Windows About form does not load approved studio artwork")
    if "MichelsLabCanonicalLockup.png" not in project or (
        "MichelsLabDarkLockup.png" not in project
    ):
        raise AssertionError("Approved source and dark-surface logo must be embedded")
    if "python tools/test_louderme_release_gate_parity.py" not in build or (
        "python tools/test_louderme_release_gate_parity.py" not in release
    ):
        raise AssertionError("Both build and stable publishing must enforce the same guard")
    for release_step in (
        "pwsh -File desktop/tools/verify_release_identity.ps1",
        "--check-apo-contract",
        "--check-developer-contract",
        "--developer-smoke-test",
        "tools/visual_release_gate.py",
        "windows-desktop --surfaces home,about,equalizer",
        "gh release view $tag",
        "gh release create $tag",
    ):
        if release_step not in release:
            raise AssertionError(f"Stable Windows release lost an important gate: {release_step}")
    version_match = re.search(r"<Version>(\d+\.\d+\.\d+)</Version>", project)
    if not version_match:
        raise AssertionError("Windows version must be explicit in project metadata")
    if f"desktop/releases/v{version_match.group(1)}.md" not in release and (
        '$notes = "desktop/releases/v$v.md"' not in release
    ):
        raise AssertionError("Publishing must use version-matching release notes")


def main() -> None:
    texts = [p.read_text(encoding="utf-8") for p in (BUILD, RELEASE, ABOUT, PROJECT)]
    check_gates(*texts)
    # Negative controls guard against repeating the exact regression that
    # caused the initial 0.1.17 publish job to fail after the WinForms build.
    mutants = [
        (1, texts[1].replace("MichelsLabCanonicalLockup.png",
                            "MichelsLabLockup.png")),
        (1, texts[1].replace(EXPECTED_SHA, LEGACY_SHA)),
        (1, texts[1].replace("MichelsLabDarkLockup", "MichelsLabLockup")),
        (1, texts[1].replace("tools/visual_release_gate.py",
                            "tools/fake_visual_gate.py")),
        (1, texts[1].replace("--check-developer-contract",
                            "--skip-developer-contract")),
    ]
    for index, candidate in mutants:
        altered = list(texts)
        altered[index] = candidate
        if altered[index] == texts[index]:
            raise AssertionError("Test mutation did not change its target")
        try:
            check_gates(*altered)
        except AssertionError:
            continue
        raise AssertionError("Unsafe release gate mutation was accepted")
    if not LOGO_SOURCE.is_file() or LOGO_SOURCE.stat().st_size != 526482:
        raise AssertionError("Approved Michel's Lab source PNG is missing")
    print("PASS: build/release branding parity and 5 negative publishing regressions")


if __name__ == "__main__":
    main()
