#!/usr/bin/env python3
"""Derive a lab-only LouderME virtual render device from the pinned Microsoft SysVAD sample.

Microsoft's original driver sources remain under MS-PL; no driver or certificate
is installed, signed or redistributed by this script. It patches a freshly
checked-out upstream source tree in-place for a reproducible WDK build.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path

PINNED_UPSTREAM = "2dc3fd3a0cc84a2933f2194e7ec0871584979071"
HARDWARE_ID = r"Root\LouderMe_VirtualRender_Lab"
SERVICE_NAME = "louderme_virtualrender_lab"


def replace_exact(value: str, old: str, new: str, path: str) -> str:
    count = value.count(old)
    if count != 1:
        raise ValueError(f"{path}: expected exactly one occurrence of {old!r}, got {count}")
    return value.replace(old, new)


def patch_minipairs(text: str) -> str:
    # Hide all demo capture endpoints and all but one render endpoint.
    render = re.compile(
        r"(PENDPOINT_MINIPAIR\s+g_RenderEndpoints\[\]\s*=\s*\{)"
        r"[^}]*"
        r"(\};)", re.S
    )
    capture = re.compile(
        r"(PENDPOINT_MINIPAIR\s+g_CaptureEndpoints\[\]\s*=\s*\{)"
        r"[^}]*"
        r"(\};)", re.S
    )
    if len(render.findall(text)) != 1 or len(capture.findall(text)) != 1:
        raise ValueError("Upstream SysVAD endpoint array signatures changed")
    text = render.sub(r"\1\n    &SpeakerMiniports,\n\2", text)
    # Keep a 1-element C++ array for valid compilation, with a zero active count.
    text = capture.sub(r"\1\n    nullptr,\n\2", text)
    text = replace_exact(
        text,
        "#define g_cCaptureEndpoints (SIZEOF_ARRAY(g_CaptureEndpoints))",
        "static ULONG g_cCaptureEndpoints = 0; // LouderME lab: no capture devices",
        "minipairs.h"
    )
    # Use loopback-capable render rather than the sample offload option.
    start = text.index("SpeakerMiniports =")
    end = text.index("};", start)
    block = text[start:end]
    if "ENDPOINT_OFFLOAD_SUPPORTED" not in block:
        raise ValueError("Speaker miniport render configuration changed")
    text = text[:start] + block.replace(
        "ENDPOINT_OFFLOAD_SUPPORTED", "ENDPOINT_LOOPBACK_SUPPORTED", 1
    ) + text[end:]
    if "&SpeakerHpMiniports," in text[text.index("g_RenderEndpoints[]"):]:
        raise ValueError("Unexpected secondary render miniport")
    return text


def patch_inf(text: str, extension: bool) -> str:
    old_hwid = r"Root\sysvad_ComponentizedAudioSample"
    if old_hwid not in text:
        raise ValueError("Upstream SysVAD hardware ID changed")
    text = text.replace(old_hwid, HARDWARE_ID)
    if extension:
        # This extension package must NOT be installed in lab; it includes
        # vendor-independent sample APO components. Only keep source consistent.
        text = text.replace(
            'ExtendedFriendlyName = "SYSVAD (with APO Extensions)"',
            'ExtendedFriendlyName = "LouderME Virtual (Lab extension - NOT FOR INSTALL)"'
        )
        return text
    text = text.replace(
        "sysvad_componentizedaudiosample", SERVICE_NAME
    ).replace(
        "sysvad_ComponentizedAudioSample_Service_Inst",
        "LouderMe_VirtualRender_Service_Inst"
    )
    # The only interfaces we actually install are Speaker wave and topology.
    start = text.index("[SYSVAD_SA.NT.Interfaces]")
    end = text.index("[SYSVAD_SA.NT.Services]", start)
    section = text[start:end].splitlines(keepends=True)
    allow = ("%KSNAME_WaveSpeaker%", "%KSNAME_TopologySpeaker%")
    section = [
        line for line in section
        if not line.lstrip().startswith("AddInterface=") or
           any(key in line for key in allow)
    ]
    text = text[:start] + "".join(section) + text[end:]
    replacements = {
        'ProviderName = "TODO-Set-Provider"': 'ProviderName = "Michel\'s Lab"',
        'MfgName      = "TODO-Set-Manufacturer"': 'MfgName      = "Michel\'s Lab"',
        'SYSVAD_SA.DeviceDesc="Virtual Audio Device (WDM) - Tablet Sample"':
            'SYSVAD_SA.DeviceDesc="LouderME Virtual Audio (Lab)"',
        'SYSVAD_ComponentizedAudioSample.SvcDesc="Virtual Audio Device (WDM) - Tablet Sample Driver"':
            'SYSVAD_ComponentizedAudioSample.SvcDesc="LouderME Virtual Audio (Lab Driver)"',
        'SYSVAD.WaveSpeaker.szPname="SYSVAD Wave Speaker"':
            'SYSVAD.WaveSpeaker.szPname="LouderME Virtual Speaker"',
        'SYSVAD.TopologySpeaker.szPname="SYSVAD Topology Speaker"':
            'SYSVAD.TopologySpeaker.szPname="LouderME Virtual Topology Speaker"',
    }
    for old, new in replacements.items():
        text = replace_exact(text, old, new, "ComponentizedAudioSample.inx")
    if "Root\\sysvad_ComponentizedAudioSample" in text:
        raise ValueError("Old SysVAD ID remained")
    return text



def prune_reference_inf(text: str) -> str:
    """Keep only the render endpoint and kernel service actually used by LouderME.

    The stock TabletAudioSample INF installs Contoso keyword detector DLL,
    microphone category registry entries and dozens of unused interfaces.
    That DLL is not built or shipped in the LouderME package. Its references
    must be removed from source instead of bypassing package verification.
    """
    allowed = {
        "Version", "SourceDisksNames", "SourceDisksFiles",
        "SignatureAttributes", "SignatureAttributes.DRM",
        "Manufacturer", "SYSVAD.NT$ARCH$.10.0...22621",
        "DestinationDirs", "SYSVAD_SA.CopyList", "SYSVAD_SA.AddReg",
        "SYSVAD.I.WaveSpeaker", "SYSVAD.I.WaveSpeaker.AddReg",
        "SYSVAD.I.TopologySpeaker", "SYSVAD.I.TopologySpeaker.AddReg",
        "SYSVAD_SA.NT", "SYSVAD_SA.NT.Interfaces",
        "SYSVAD_SA.NT.Services", "LouderMe_VirtualRender_Service_Inst",
        "SYSVAD_SA.NT.HW", "AUDIOHW.AddReg",
        "SYSVAD_SA.NT.Wdf", "SYSVAD_SA_WdfSect", "Strings",
    }
    matches = list(re.finditer(r"(?m)^\[([^]\r\n]+)\][ \t]*\r?$", text))
    found = {m.group(1) for m in matches}
    if not allowed.issubset(found):
        raise ValueError(f"Missing required INF sections: {sorted(allowed - found)}")
    sections = []
    for index, match in enumerate(matches):
        section = match.group(1)
        if section not in allowed:
            continue
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        block = text[match.start():end]
        kept = [block.splitlines()[0]]
        for line in block.splitlines()[1:]:
            stripped = line.strip()
            low = stripped.lower()
            if section == "SYSVAD_SA.NT.Interfaces":
                # Do not advertise mic, headphone, HDMI or USB/Bluetooth sample endpoints.
                if not low.startswith("addinterface=") or (
                    "%ksname_wavespeaker%" not in low and
                    "%ksname_topologyspeaker%" not in low
                ):
                    continue
            if section == "SourceDisksFiles" and "keyworddetector" in low:
                continue
            if section == "SignatureAttributes" and "keyworddetector" in low:
                continue
            if section == "DestinationDirs" and "keyworddetector" in low:
                continue
            if section == "SYSVAD_SA.AddReg" and (
                "micarray" in low or "micincustom" in low
            ):
                continue
            if section == "SYSVAD.I.TopologySpeaker.AddReg" and (
                low.startswith("hkr,fx") or "capxsampleapo" in low
            ):
                # No Microsoft demo APO registrations in the LouderME driver.
                continue
            if section == "AUDIOHW.AddReg" and "hkr,,security" in low:
                # Do not inherit the reference sample's permissive WORLD_RW SDDL.
                continue
            if section == "SYSVAD_SA.NT" and low.startswith("copyfiles="):
                line = "CopyFiles=SYSVAD_SA.CopyList"
            if section == "SYSVAD_SA.NT" and low.startswith("addreg="):
                line = "AddReg=SYSVAD_SA.AddReg"
            if section == "Version" and low.startswith("catalogfile"):
                line = "CatalogFile = LouderMeVirtualRenderLab.cat"
            if section == "Strings" and (
                "keyworddetector" in low or
                "micarray" in low or "micin" in low
            ):
                continue
            kept.append(line)
        sections.append("\n".join(kept).rstrip() + "\n")

    final = "\n".join(sections)
    # Microsoft's sample uses a StampInf $ARCH$ placeholder, but -a x64
    # on current WDK yields NTx64, which InfVerif rejects. Model sections
    # for Windows x64 are canonically NTamd64.
    final = final.replace("NT$ARCH$.10.0...22621", "NTamd64.10.0...22621")
    if "NT$ARCH$" in final or "NTx64" in final:
        raise ValueError("Invalid unresolved Windows INF architecture decoration")
    required_interface_count = 5
    interfaces = final.split("[SYSVAD_SA.NT.Interfaces]", 1)[1].split("[", 1)[0]
    if interfaces.count("AddInterface=") != required_interface_count:
        raise ValueError("Expected exactly five speaker topology/render interfaces")
    if "keyworddetectorcontosoadapter.dll" in final.lower():
        raise ValueError("Unshipped Contoso keyword detector still referenced")
    if "CopyFiles=SYSVAD_SA.CopyList,KEYWORD" in final:
        raise ValueError("Unexpected keyword detector CopyFiles reference")
    if "HKR,,Security" in final:
        raise ValueError("Broad sample driver object SDDL retained")
    return final


def patch_adapter(text: str) -> str:
    # Never allow SysVAD's optional render-to-disk recording to be enabled
    # via a Windows registry value, and suppress demo-generated sine tones.
    if "DWORD g_DoNotCreateDataFiles = 1;" not in text:
        raise ValueError("SysVAD recording default unexpectedly changed")
    text = replace_exact(
        text, "DWORD g_DisableToneGenerator = 0;",
        "DWORD g_DisableToneGenerator = 1; // LouderME: no sample-generated audio",
        "adapter.cpp"
    )
    rows = text.splitlines(keepends=True)
    matches = [i for i, row in enumerate(rows) if 'L"DoNotCreateDataFiles"' in row]
    if len(matches) != 1:
        raise ValueError("Unexpected SysVAD recording registry controls")
    old_row = rows[matches[0]]
    ending = "\r\n" if old_row.endswith("\r\n") else "\n"
    rows[matches[0]] = (
        "        // LouderME privacy: recording-to-disk is permanently disabled."
        + ending
    )
    text = "".join(rows)
    if 'L"DoNotCreateDataFiles"' in text:
        raise ValueError("Render audio recording registry override remains")
    return text


def patch_project_for_desktop(text: str) -> str:
    # LouderME is explicitly a Windows desktop application; it does not
    # target Windows IoT/Core, mobile, or other Universal-driver platforms.
    # Microsoft's Desktop-driver model has its own INF/signing requirements
    # and is NOT a substitute for those installation validation gates.
    old = "<DriverTargetPlatform>Universal</DriverTargetPlatform>"
    found = text.count(old)
    if found != 4:
        raise ValueError(f"Expected four SysVAD project platform configs, found {found}")
    return text.replace(old, "<DriverTargetPlatform>Desktop</DriverTargetPlatform>")


def patch_resource(text: str) -> str:
    return replace_exact(
        text, '"Microsoft Virtual Audio Tablet Sample Driver"',
        '"LouderME Virtual Audio Lab Driver"', "TabletAudioSample.rc"
    )


def prepare(repo: Path, dry_run: bool = False) -> dict:
    repo = repo.resolve()
    root = repo / "audio" / "sysvad"
    if not (root / "sysvad.sln").is_file() or not (repo / "LICENSE").is_file():
        raise FileNotFoundError("Expected full Microsoft Windows-driver-samples checkout")
    sha = subprocess.check_output(
        ["git", "-C", str(repo), "rev-parse", "HEAD"], text=True
    ).strip()
    if sha != PINNED_UPSTREAM:
        raise ValueError(f"Unpinned upstream revision {sha}; require {PINNED_UPSTREAM}")
    license_text = (repo / "LICENSE").read_text(encoding="utf-8")
    if "The Microsoft Public License (MS-PL)" not in license_text:
        raise ValueError("Expected original MS-PL license in source tree")

    work = {
        "TabletAudioSample/minipairs.h": patch_minipairs,
        "TabletAudioSample/ComponentizedAudioSample.inx": lambda s: prune_reference_inf(patch_inf(s, False)),
        "TabletAudioSample/ComponentizedAudioSampleExtension.inx": lambda s: patch_inf(s, True),
        "TabletAudioSample/TabletAudioSample.rc": patch_resource,
        "adapter.cpp": patch_adapter,
        "TabletAudioSample/TabletAudioSample.vcxproj": patch_project_for_desktop
    }
    patched = {}
    for rel, patch in work.items():
        target = root / rel
        raw = target.read_bytes()
        # Some SysVAD INF templates use UTF-16 LE with BOM, not UTF-8.
        # Preserve their original encoding so Inf2Cat/StampInf can read them.
        if raw.startswith((b"\xff\xfe", b"\xfe\xff")):
            encoding = "utf-16"
        elif raw.startswith(b"\xef\xbb\xbf"):
            encoding = "utf-8-sig"
        else:
            encoding = "utf-8"
        original = raw.decode(encoding)
        updated = patch(original)
        if updated == original:
            raise ValueError(f"No patch applied for {rel}")
        encoded = updated.encode(encoding)
        patched[rel] = {"sha256": hashlib.sha256(encoded).hexdigest(), "encoding": encoding}
        if not dry_run:
            target.write_bytes(encoded)
    manifest = {
        "lab_only": True,
        "upstream_repository": "microsoft/Windows-driver-samples",
        "upstream_commit": sha,
        "hardware_id": HARDWARE_ID,
        "service": SERVICE_NAME,
        "driver_target_platform": "Desktop",
        "render_endpoints": 1,
        "capture_endpoints": 0,
        "package_file_references": ["tabletaudiosample.sys"],
        "sample_keyword_detector_included": False,
        "recording_to_file_enabled": False,
        "sample_tone_generator_enabled": False,
        "driver_signed": False,
        "driver_installed": False,
        "real_audio_verified": False,
        "patched_files": patched
    }
    if not dry_run:
        (root / "LICENSE-MS-PL.txt").write_text(license_text, encoding="utf-8")
        (root / "LOUDERME_LAB_MANIFEST.json").write_text(
            json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
        )
    return manifest


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--upstream-root", required=True, type=Path)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    print(json.dumps(prepare(args.upstream_root, args.dry_run), indent=2))


if __name__ == "__main__":
    main()
