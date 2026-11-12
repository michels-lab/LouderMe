#!/usr/bin/env python3
"""Validate a real unsigned/signed Google Play AAB without accepting an APK.

This is an offline preflight, not a Play Console approval or upload. A signed
candidate additionally needs 'jarsigner -verify' in the release workflow.
"""
from __future__ import annotations

import argparse
import re
import zipfile
from pathlib import Path

PUBLIC_PACKAGE = "com.michelslab.louderme"


def verify_bundle(bundle: Path, gradle: str, main_manifest: str,
                  sideload_manifest: str, expect_signed: bool) -> tuple[str, int]:
    identity = re.search(r'\bapplicationId\s*=\s*"([^"]+)"', gradle)
    name = re.search(r'\bversionName\s*=\s*"(\d+\.\d+\.\d+)"', gradle)
    code = re.search(r'\bversionCode\s*=\s*(\d+)\b', gradle)
    target = re.search(r'\btargetSdk\s*=\s*(\d+)\b', gradle)
    if not all((identity, name, code, target)):
        raise AssertionError("Missing public applicationId/version/targetSdk")
    if identity.group(1) != PUBLIC_PACKAGE or int(target.group(1)) < 35:
        raise AssertionError("Incorrect public Play package identity or targetSdk")
    if not re.search(r'create\("play"\)', gradle):
        raise AssertionError("Play product flavor is absent")
    if "REQUEST_INSTALL_PACKAGES" in main_manifest:
        raise AssertionError("Install-packages permission leaked into common manifest")
    if "REQUEST_INSTALL_PACKAGES" not in sideload_manifest:
        raise AssertionError("Direct-only install-packages permission was removed")
    if 'foregroundServiceType="specialUse"' not in main_manifest:
        raise AssertionError("Play specialUse foreground-service declaration missing")
    if not bundle.is_file():
        raise AssertionError(f"Missing real AAB: {bundle}")
    with zipfile.ZipFile(bundle) as z:
        if z.testzip() is not None:
            raise AssertionError("Corrupted AAB ZIP member")
        entries = set(z.namelist())
        if not {"base/manifest/AndroidManifest.xml", "base/resources.pb"}.issubset(entries):
            raise AssertionError("Not a valid base-module Android App Bundle")
        if not any(n.startswith("base/dex/classes") and n.endswith(".dex")
                   for n in entries):
            raise AssertionError("Bundle contains no application classes")
        signed = any(n.upper().startswith("META-INF/") and
                     n.upper().endswith(".SF") for n in entries) and any(
                         n.upper().startswith("META-INF/") and
                         n.upper().endswith((".RSA", ".EC", ".DSA"))
                         for n in entries)
    if expect_signed != signed:
        raise AssertionError(
            "AAB signing state differs from the requested release channel")
    return name.group(1), int(code.group(1))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--bundle", required=True, type=Path)
    parser.add_argument("--gradle", type=Path, default=Path("app/build.gradle.kts"))
    parser.add_argument("--main-manifest", type=Path,
                        default=Path("app/src/main/AndroidManifest.xml"))
    parser.add_argument("--sideload-manifest", type=Path,
                        default=Path("app/src/sideload/AndroidManifest.xml"))
    parser.add_argument("--expect-signed", action="store_true")
    args = parser.parse_args()
    version, code = verify_bundle(
        args.bundle, args.gradle.read_text(encoding="utf-8"),
        args.main_manifest.read_text(encoding="utf-8"),
        args.sideload_manifest.read_text(encoding="utf-8"),
        args.expect_signed)
    print(f"PASS: Play AAB package={PUBLIC_PACKAGE} version={version} "
          f"versionCode={code} signed={args.expect_signed}")


if __name__ == "__main__":
    main()
