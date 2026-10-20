#!/usr/bin/env python3
"""Installed Android Developer Edition end-to-end test (no mock screens or audio).

Proves independent installation, live section selection, owner note persistence,
and reviewed local export using the actual Compose runtime on an emulator.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import time
import xml.etree.ElementTree as ET
from pathlib import Path

from capture_android_visual import (
    adb, hierarchy, install_when_package_manager_ready,
    dismiss_system_launcher_anr, point_for_label,
)

PACKAGE = "com.michelslab.louderme.developer"
PUBLIC_PACKAGE = "com.michelslab.louderme"
_launcher_recovered = False
MAIN_ACTIVITY = "com.michelslab.louderme.MainActivity"
NOTE = "GranadaQAanchor20261020"
RECT = re.compile(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]")


def element_text(node: ET.Element) -> str:
    return (node.get("text") or "") + " " + (node.get("content-desc") or "")


def wait_for_text(text: str, *, contains: bool = False, timeout: int = 50) -> ET.Element:
    global _launcher_recovered
    until = time.monotonic() + timeout
    last_labels: list[str] = []
    while time.monotonic() < until:
        try:
            root = hierarchy()
            nodes = [node for node in root.iter("node")
                     if (text in element_text(node) if contains
                         else text in ((node.get("text") or ""), (node.get("content-desc") or "")))]
            if nodes:
                return root
            # An unrelated cold Pixel Launcher ANR can cover a healthy app.
            # Recover that exact emulator-only dialog once, then insist on a
            # new real installed-app hierarchy. LouderMe ANRs still fail.
            if not _launcher_recovered and dismiss_system_launcher_anr(root):
                _launcher_recovered = True
                adb("shell", "am", "start", "-W", "-n", PACKAGE + "/" + MAIN_ACTIVITY)
                time.sleep(2)
                continue
            last_labels = [element_text(node)[:90] for node in root.iter("node")
                           if element_text(node).strip()][-22:]
        except RuntimeError:
            pass
        time.sleep(1)
    raise RuntimeError(f"Expected real Developer UI label {text!r} not present; last labels={last_labels}")


def click(root: ET.Element, label: str, *, contains: bool = False) -> None:
    parents = {child: parent for parent in root.iter() for child in parent}
    nodes = [node for node in root.iter("node")
             if ((label in element_text(node)) if contains
                 else label in ((node.get("text") or ""), (node.get("content-desc") or "")))]
    for node in nodes:
        candidate = node
        while candidate in parents and candidate.get("clickable") != "true":
            candidate = parents[candidate]
        # For outlined input labels the child itself has a useful bound.
        if label == "Your comment, verbatim":
            candidate = node
        match = RECT.fullmatch(candidate.get("bounds", ""))
        if match:
            x1, y1, x2, y2 = map(int, match.groups())
            if x2 > x1 and y2 > y1:
                adb("shell", "input", "tap", str((x1 + x2) // 2), str((y1 + y2) // 2))
                return
    raise RuntimeError(f"No physically tappable native label {label!r} was found")


def is_visible_label(root: ET.Element, label: str) -> bool:
    return any(label in ((node.get("text") or ""), (node.get("content-desc") or ""))
               for node in root.iter("node"))


def open_inspector(*, attempts: int = 3, panel_wait_s: float = 7.0) -> ET.Element:
    """Open the real Developer panel with bounded, observed native taps.

    Android occasionally drops the first touch during a cold Compose draw.
    Only retry when the requested panel is genuinely absent AND the real
    developer launch button is visible. Never interpret a tap as a pass.
    """
    expected = "Select section / Add note"
    launcher = "DEV · Audit / Notes"
    for _ in range(attempts):
        root = wait_for_text(launcher)
        if is_visible_label(root, expected):
            return root
        click(root, launcher)
        deadline = time.monotonic() + panel_wait_s
        while time.monotonic() < deadline:
            root = hierarchy()
            if is_visible_label(root, expected):
                return root
            if not is_visible_label(root, launcher):
                # A different system UI or non-developer screen now covers it.
                # Do not keep tapping blind on an unknown native surface.
                raise RuntimeError("Developer inspector navigation covered by unexpected UI")
            time.sleep(0.75)
    raise RuntimeError(
        "Actual Developer inspector did not open after bounded live UI taps")


def screenshot(folder: Path, name: str, digest: str) -> dict:
    picture = folder / f"{name}.png"
    picture.write_bytes(adb("exec-out", "screencap", "-p", binary=True))
    if picture.stat().st_size < 3000:
        raise RuntimeError(f"Screenshot {picture} was implausibly small")
    return {"screen": name, "path": picture.name,
            "sha256": hashlib.sha256(picture.read_bytes()).hexdigest(),
            "apk_sha256": digest, "evidence": "installed emulator real pixels"}


def run(apk: Path, sha: str, out: Path, public_apk: Path | None = None) -> None:
    if not re.fullmatch(r"[0-9a-f]{40}", sha, re.I):
        raise ValueError("Exact source SHA required")
    out.mkdir(parents=True, exist_ok=True)
    digest = hashlib.sha256(apk.read_bytes()).hexdigest()
    install_when_package_manager_ready(apk)
    if public_apk is not None:
        if not public_apk.is_file():
            raise ValueError("Baseline public APK is missing")
        adb("install", "-r", str(public_apk.resolve()))
        installed = adb("shell", "pm", "list", "packages")
        installed_packages = set(installed.splitlines())
        if "package:" + PUBLIC_PACKAGE not in installed_packages or "package:" + PACKAGE not in installed_packages:
            raise RuntimeError("Public and Developer APKs were not both independently installed")
    adb("shell", "am", "force-stop", PACKAGE)
    adb("shell", "am", "start", "-W", "-n", PACKAGE + "/" + MAIN_ACTIVITY)
    screenshots = []
    try:
        root = wait_for_text("DEV · Audit / Notes")
        # Capture the *actual* app control bounds while Home is accessible.
        # The inspection touch shield intentionally hides underlying semantics,
        # so querying that label AFTER activation is incorrect.
        volume_hit = point_for_label(root, "Media volume · 0–100%")
        screenshots.append(screenshot(out, "developer-home", digest))
        root = open_inspector()
        screenshots.append(screenshot(out, "developer-audit", digest))
        click(root, "Select section / Add note")
        wait_for_text("INSPECT MODE · tap a real app section (Back cancels)")
        # Tap the real Home component measured *before* the shield covered it.
        # We never hardcode geometry, infer coordinates from screenshots, or
        # fire the underlying volume slider event during inspection.
        adb("shell", "input", "tap", str(volume_hit[0]), str(volume_hit[1]))
        root = wait_for_text("Observation · Media volume")
        screenshots.append(screenshot(out, "developer-anchored-note", digest))
        click(root, "Your comment, verbatim")
        adb("shell", "input", "text", NOTE)
        root = wait_for_text("Save note")
        click(root, "Save note")
        root = open_inspector()
        root = wait_for_text(NOTE, contains=True)
        screenshots.append(screenshot(out, "developer-saved-note", digest))

        # Restart the ACTUAL installed application to prove persistence.
        adb("shell", "am", "force-stop", PACKAGE)
        adb("shell", "am", "start", "-W", "-n", PACKAGE + "/" + MAIN_ACTIVITY)
        root = wait_for_text("DEV · Audit / Notes")
        click(root, "DEV · Audit / Notes")
        root = wait_for_text(NOTE, contains=True)
        root = wait_for_text("Run Audit")
        click(root, "Run Audit")
        wait_for_text("Read-only findings:", contains=True)
        root = wait_for_text("Export JSON")
        click(root, "Export JSON")
        wait_for_text("Review and redact export")
        wait_for_text(NOTE, contains=True)
        screenshots.append(screenshot(out, "developer-reviewed-export", digest))
        # Do not invoke ACTION_SEND: actual export remains deliberate user intent.

    except Exception:
        # Keep actual unqualified pixels on failure; do not forge a passing manifest.
        try:
            (out / "diagnostic-developer-failure.png").write_bytes(
                adb("exec-out", "screencap", "-p", binary=True))
        except RuntimeError:
            pass
        raise
    finally:
        adb("shell", "am", "force-stop", PACKAGE)

    output = {"schema": "michelslab-developer-installed-v1", "source_commit": sha,
              "developer_package": PACKAGE, "apk_sha256": digest,
              "coinstalled_public_package": PUBLIC_PACKAGE if public_apk is not None else None,
              "device_evidence": "Android emulator, not owner physical phone",
              "verified_workflow": [
                  "independent developer package launched",
                  "real Compose inspector and section selection",
                  "verbatim owner annotation saved and displayed",
                  "annotation survived process restart",
                  "read-only audit launched",
                  "JSON review contained verbatim note"
              ],
              "not_proven": ["acoustic audio gain", "external app output",
                             "physical phone behavior", "owner acceptance"],
              "screenshots": screenshots}
    (out / "manifest.json").write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print("PASS: actual installed Android Developer Edition QA completed; no sound/action simulation.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--apk", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--public-apk", type=Path, default=None)
    args = parser.parse_args()
    run(args.apk, args.source_sha, args.out, args.public_apk)
