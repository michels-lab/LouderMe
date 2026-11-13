#!/usr/bin/env python3
"""Create a Windows Desktop Bridge MSIX manifest from Partner Center identity.

The output is a packaging *candidate*. Never guess the official Partner Center
Identity Name or Publisher from the studio's display name.
"""
from __future__ import annotations

import argparse
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = "http://schemas.microsoft.com/appx/manifest/foundation/windows10"
UAP = "http://schemas.microsoft.com/appx/manifest/uap/windows10"
RESCAP = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
ET.register_namespace("", ROOT)
ET.register_namespace("uap", UAP)
ET.register_namespace("rescap", RESCAP)

ASSETS = (
    "Square44x44Logo.png",
    "Square150x150Logo.png",
    "Square310x310Logo.png",
    "Wide310x150Logo.png",
    "StoreLogo.png",
)


def create_manifest(identity_name: str, publisher: str,
                    product_version: str, output: Path, preflight: bool = False) -> None:
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9.-]{2,49}", identity_name):
        raise ValueError("Package Identity Name must be obtained from Partner Center")
    if not publisher.startswith("CN=") or len(publisher) < 4:
        raise ValueError("Publisher must be the exact Partner Center publisher DN")
    if "Preflight" in identity_name and not preflight:
        raise ValueError("Preflight identity cannot be submitted to Microsoft Store")
    if not re.fullmatch(r"\d{1,5}\.\d{1,5}\.\d{1,5}\.0", product_version):
        raise ValueError("MSIX version must be a four-part x.y.z.0 version")
    if any(int(part) > 65535 for part in product_version.split(".")):
        raise ValueError("MSIX version segments must be <= 65535")
    root = ET.Element(f"{{{ROOT}}}Package", {"IgnorableNamespaces": "uap rescap"})
    ET.SubElement(root, f"{{{ROOT}}}Identity", {
        "Name": identity_name, "Publisher": publisher,
        "Version": product_version, "ProcessorArchitecture": "x64"
    })
    props = ET.SubElement(root, f"{{{ROOT}}}Properties")
    ET.SubElement(props, f"{{{ROOT}}}DisplayName").text = "LouderMe"
    ET.SubElement(props, f"{{{ROOT}}}PublisherDisplayName").text = "Michel's Lab"
    ET.SubElement(props, f"{{{ROOT}}}Logo").text = r"Assets\StoreLogo.png"
    deps = ET.SubElement(root, f"{{{ROOT}}}Dependencies")
    ET.SubElement(deps, f"{{{ROOT}}}TargetDeviceFamily", {
        "Name": "Windows.Desktop",
        "MinVersion": "10.0.19041.0", "MaxVersionTested": "10.0.26100.0"
    })
    langs = ET.SubElement(root, f"{{{ROOT}}}Resources")
    ET.SubElement(langs, f"{{{ROOT}}}Resource", {"Language": "en-us"})
    ET.SubElement(langs, f"{{{ROOT}}}Resource", {"Language": "es-mx"})
    apps = ET.SubElement(root, f"{{{ROOT}}}Applications")
    app = ET.SubElement(apps, f"{{{ROOT}}}Application", {
        "Id": "LouderMe", "Executable": "LouderMe.exe",
        "EntryPoint": "Windows.FullTrustApplication"
    })
    visual = ET.SubElement(app, f"{{{UAP}}}VisualElements", {
        "DisplayName": "LouderMe", "Description": "LouderMe audio booster and equalizer",
        "Square150x150Logo": r"Assets\Square150x150Logo.png",
        "Square44x44Logo": r"Assets\Square44x44Logo.png",
        "BackgroundColor": "#0C1422"
    })
    ET.SubElement(visual, f"{{{UAP}}}DefaultTile", {
        "Square310x310Logo": r"Assets\Square310x310Logo.png",
        "Wide310x150Logo": r"Assets\Wide310x150Logo.png"
    })
    capabilities = ET.SubElement(root, f"{{{ROOT}}}Capabilities")
    ET.SubElement(capabilities, f"{{{RESCAP}}}Capability", {"Name": "runFullTrust"})
    output.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(root).write(output, encoding="utf-8", xml_declaration=True)


def check_manifest(output: Path, stage: Path,
                   identity_name: str, publisher: str, version: str) -> None:
    doc = ET.parse(output)
    root = doc.getroot()
    ident = root.find(f"{{{ROOT}}}Identity")
    app = root.find(f"{{{ROOT}}}Applications/{{{ROOT}}}Application")
    cap = root.find(f"{{{ROOT}}}Capabilities/{{{RESCAP}}}Capability")
    if ident is None or ident.get("Name") != identity_name or (
            ident.get("Publisher") != publisher or ident.get("Version") != version):
        raise AssertionError("MSIX identity/version mismatch")
    if app is None or app.get("Executable") != "LouderMe.exe" or (
            app.get("EntryPoint") != "Windows.FullTrustApplication"):
        raise AssertionError("Missing full-trust WinForms package entrypoint")
    if cap is None or cap.get("Name") != "runFullTrust":
        raise AssertionError("FullTrust permission missing from MSIX")
    if not (stage / "LouderMe.exe").is_file():
        raise AssertionError("Missing compiled LouderMe.exe payload")
    if not (stage / "native" / "LouderMeAudioBridge.exe").is_file():
        raise AssertionError("Missing independent LouderMe native bridge diagnostic")
    for asset in ASSETS:
        p = stage / "Assets" / asset
        if not p.is_file() or p.stat().st_size < 100:
            raise AssertionError(f"Missing official-derived MSIX asset: {asset}")


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--identity-name", required=True)
    p.add_argument("--publisher", required=True)
    p.add_argument("--version", required=True)
    p.add_argument("--stage", required=True, type=Path)
    p.add_argument("--preflight", action="store_true")
    args = p.parse_args()
    manifest = args.stage / "AppxManifest.xml"
    create_manifest(args.identity_name, args.publisher,
                    args.version, manifest, args.preflight)
    check_manifest(manifest, args.stage,
                   args.identity_name, args.publisher, args.version)
    print(f"PASS: MSIX fullTrust manifest and payload for {args.identity_name} "
          f"{args.version} (preflight={args.preflight}; not Store acceptance)")


if __name__ == "__main__":
    main()
