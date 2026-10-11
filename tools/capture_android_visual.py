#!/usr/bin/env python3
"""Capture real LouderMe Home/About UI from an installed Android emulator APK.

No mock screenshots. This runs after Gradle builds the exact APK under test,
uses the real Android window hierarchy to click About, and binds PNG bytes to
the installed APK digest and current source commit.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

PACKAGE = "com.michelslab.louderme"
SIZES = {"compact": (720, 1280), "wide": (1080, 1920)}
RECT = re.compile(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]")


def adb(*arguments: str, binary: bool = False):
    try:
        result = subprocess.run(["adb", *arguments], check=True, capture_output=True,
                                timeout=90)
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as exc:
        stdout = getattr(exc, "stdout", b"") or b""
        stderr = getattr(exc, "stderr", b"") or b""
        raise RuntimeError(
            f"ADB {arguments!r} failed: stdout={stdout.decode(errors='replace')!r} "
            f"stderr={stderr.decode(errors='replace')!r}"
        ) from exc
    return result.stdout if binary else result.stdout.decode(errors="replace")


def install_when_package_manager_ready(apk: Path) -> None:
    # sys.boot_completed can precede a usable package manager on a cold CI AVD.
    last_error = None
    for attempt in range(1, 5):
        try:
            adb("wait-for-device")
            packages = adb("shell", "cmd", "package", "list", "packages")
            if "package:" not in packages:
                raise RuntimeError("Android package manager has not initialized")
            print(f"Android package manager ready; install attempt {attempt}/4", flush=True)
            result = adb("install", "-r", str(apk.resolve()))
            if "Success" not in result:
                raise RuntimeError(f"Unexpected installer output: {result!r}")
            return
        except RuntimeError as error:
            last_error = error
            print(f"Install attempt {attempt}/4 failed: {error}", flush=True)
            if attempt < 4:
                time.sleep(5)
    raise RuntimeError(f"Cannot install real built APK after retries: {last_error}")


def sha(file: Path):
    return hashlib.sha256(file.read_bytes()).hexdigest()


def hierarchy() -> ET.Element:
    adb("shell", "uiautomator", "dump", "/sdcard/louderme-qa.xml")
    return ET.fromstring(adb("exec-out", "cat", "/sdcard/louderme-qa.xml"))


def click_labeled(root: ET.Element, label: str) -> None:
    for node in root.iter("node"):
        if node.get("text") == label or node.get("content-desc") == label:
            match = RECT.fullmatch(node.get("bounds", ""))
            if match:
                left, top, right, bottom = map(int, match.groups())
                if right > left and bottom > top:
                    adb("shell", "input", "tap", str((left + right) // 2),
                        str((top + bottom) // 2))
                    return
    raise RuntimeError(f"Cannot find a displayed '{label}' control in real UI hierarchy")


def snapshot(out: Path, viewport: str, surface: str, digest: str, size: tuple[int, int]):
    png = out / f"{surface}-{viewport}.png"
    png.write_bytes(adb("exec-out", "screencap", "-p", binary=True))
    if len(png.read_bytes()) < 3000:
        raise RuntimeError(f"Suspiciously small screenshot: {png}")
    # Pixel format and dimensions are verified separately by visual_release_gate.py.
    return {
        "platform": "android", "surface": surface, "viewport": viewport,
        "width": size[0], "height": size[1], "path": png.name,
        "sha256": sha(png), "capture_method": "installed-android-emulator",
        "candidate_artifact_sha256": digest,
    }


def capture(apk: Path, source: str, out: Path):
    if not re.fullmatch(r"[0-9a-fA-F]{40}", source):
        raise ValueError("Source commit must be exactly 40 hexadecimal characters")
    if not apk.is_file() or apk.stat().st_size < 500_000:
        raise ValueError("Real built APK is missing or too small")
    out.mkdir(parents=True, exist_ok=True)
    digest = sha(apk)
    install_when_package_manager_ready(apk)
    shots = []
    try:
        for viewport, size in SIZES.items():
            adb("shell", "wm", "size", f"{size[0]}x{size[1]}")
            adb("shell", "wm", "density", "320")
            adb("shell", "am", "force-stop", PACKAGE)
            adb("shell", "am", "start", "-W", "-n", PACKAGE + "/.MainActivity")
            time.sleep(3)
            home = hierarchy()
            if not any(n.get("text") == "About" for n in home.iter("node")):
                raise RuntimeError("Installed LouderMe Home has no labeled About action")
            shots.append(snapshot(out, viewport, "home", digest, size))
            click_labeled(home, "About")
            time.sleep(2)
            about = hierarchy()
            texts = [n.get("text", "") for n in about.iter("node")]
            if "Back" not in texts or "About" not in texts:
                raise RuntimeError("Native About screen did not open after real About tap")
            shots.append(snapshot(out, viewport, "about", digest, size))
            adb("shell", "input", "keyevent", "KEYCODE_BACK")
    finally:
        adb("shell", "wm", "size", "reset")
        adb("shell", "wm", "density", "reset")
        adb("shell", "am", "force-stop", PACKAGE)
    result = {
        "schema": "michelslab-rendered-ui-v1", "source_commit": source,
        "artifact_sha256": {"android": digest}, "screenshots": shots,
        "human_visual_review": {"decision": "PENDING", "reviewer": None},
        "physical_device_acceptance": "NOT TESTED",
    }
    (out / "manifest.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print("Captured real installed Android APK Home/About at compact and wide resolutions.")
    print("No assertion of physical-device, Play, signed release APK, or owner review.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()
    capture(args.apk, args.source_sha, args.out)
