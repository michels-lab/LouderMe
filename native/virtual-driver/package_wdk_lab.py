#!/usr/bin/env python3
"""Package a real WDK-validated SysVAD derivative for disposable-VM handoff.

This tool never builds, signs, installs, trusts or executes a kernel driver.
It accepts ONLY the three outputs of validate_lab_package.ps1 and pinned
MS-PL derivation metadata, and emits a checksummed *unsigned catalog* ZIP.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import zipfile
from pathlib import Path

from prepare_sysvad import HARDWARE_ID, PINNED_UPSTREAM, SERVICE_NAME

PACKAGE_FILES = (
    "LouderMeVirtualRenderLab.inf",
    "LouderMeVirtualRenderLab.cat",
    "TabletAudioSample.sys",
)
EXTRA_FILES = (
    "LOUDERME_LAB_MANIFEST.json",
    "LICENSE-MS-PL.txt",
    "LAB_PACKAGE_PROVENANCE.json",
    "README-DO-NOT-INSTALL.txt",
    "SHA256SUMS.txt",
)
ZIP_FILES = PACKAGE_FILES + EXTRA_FILES
ZIP_PREFIX = "LouderMe-VirtualRender-LAB-UNSIGNED/"
FIXED_TIME = (2026, 1, 1, 0, 0, 0)
MAX_FILE_SIZE = 32 * 1024 * 1024

NOTICE = """LOUDERME EXPERIMENTAL WINDOWS KERNEL DRIVER — UNSIGNED CATALOG

This package is NOT an installer and is NOT approved or signed for
Microsoft Store, production Windows, or a personal/work computer.
The original Microsoft SysVAD derivative is lab-only (MS-PL license
included). Do not run SetupAPI, pnputil, devcon or change Windows boot
security settings based on this ZIP.

This package cannot be loaded as a trusted kernel driver. A separate,
explicitly authorized lab-signing workflow, VM trust configuration and
disposable VM snapshot are required before any install experiment.
Never upload private keys, locally signed drivers or endpoint IDs to CI.

Only the manual run_disposable_lab.ps1 tool may perform approved
installation/removal inside an isolated, snapshot-backed VM. It demands
a separately trusted lab-signed catalog and direct user consent.

The kernel *.sys may contain a WDK-generated test signature; the
driver *package* catalog was produced by Inf2Cat and remains unsigned.
A signed kernel binary alone NEVER makes the INF/CAT installable.
No real application PCM, physical audio, driver installation, audio
amplification or Microsoft certification has been demonstrated here.
"""


def digest(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def read_regular(path: Path, minimum: int, maximum: int = MAX_FILE_SIZE) -> bytes:
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Missing, symbolic or nonregular source file: {path.name}")
    length = path.stat().st_size
    if length < minimum or length > maximum:
        raise ValueError(f"Unexpected WDK package payload size: {path.name}")
    return path.read_bytes()


def check_manifest(raw: bytes) -> dict:
    try:
        m = json.loads(raw)
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise ValueError("Invalid pinned SysVAD derivation manifest") from exc
    if not isinstance(m, dict):
        raise ValueError("Invalid SysVAD manifest object")
    expected = {
        "lab_only": True,
        "upstream_repository": "microsoft/Windows-driver-samples",
        "upstream_commit": PINNED_UPSTREAM,
        "hardware_id": HARDWARE_ID,
        "service": SERVICE_NAME,
        "driver_target_platform": "Desktop",
        "render_endpoints": 1,
        "capture_endpoints": 0,
        "package_file_references": ["tabletaudiosample.sys"],
        "recording_to_file_enabled": False,
        "sample_tone_generator_enabled": False,
        "sample_keyword_detector_included": False,
        "lab_render_loopback_relay_source": True,
        "lab_frame_accurate_dma_clock": True,
        "driver_signed": False,
        "driver_installed": False,
        "real_audio_verified": False,
    }
    for k, value in expected.items():
        if type(m.get(k)) is not type(value) or m[k] != value:
            raise ValueError(f"SysVAD manifest contract changed: {k}")
    if not isinstance(m.get("patched_files"), dict) or len(m["patched_files"]) < 9:
        raise ValueError("Missing source derivation fingerprints")
    return m


def check_inf(raw: bytes) -> None:
    encoding = "utf-16" if raw.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8-sig"
    try:
        inf = raw.decode(encoding)
    except UnicodeError as exc:
        raise ValueError("Driver INF encoding invalid") from exc
    required = (HARDWARE_ID, SERVICE_NAME, "LouderME Virtual Speaker",
                "TabletAudioSample.sys", "LouderMeVirtualRenderLab.cat")
    for value in required:
        if value.casefold() not in inf.casefold():
            raise ValueError(f"Driver INF missing package contract: {value}")
    if any(value in inf for value in (
        "Root\\sysvad_ComponentizedAudioSample", "TODO-Set-Provider",
        "$KMDFVERSION$", "KeywordDetectorContosoAdapter.sys",
    )):
        raise ValueError("Driver INF contains unapproved SysVAD demo identity")
    # The actual INF/CAT validity was established by StampInf, InfVerif /h
    # and Inf2Cat before this packaging script is run.
    if not re.search(r"\bKmdfLibraryVersion\s*=\s*1\.33\b", inf):
        raise ValueError("Wrong pinned KMDF version")


def check_payload(files: dict[str, bytes]) -> dict:
    if set(files) != set(PACKAGE_FILES) | {
        "LOUDERME_LAB_MANIFEST.json", "LICENSE-MS-PL.txt"
    }:
        raise ValueError("Kernel input set differs from exact approved files")
    if not files["TabletAudioSample.sys"].startswith(b"MZ"):
        raise ValueError("Real linked x64 PE/SYS missing MZ header")
    if len(files["TabletAudioSample.sys"]) < 10000:
        raise ValueError("Driver SYS is not a linked binary")
    check_inf(files["LouderMeVirtualRenderLab.inf"])
    manifest = check_manifest(files["LOUDERME_LAB_MANIFEST.json"])
    if b"The Microsoft Public License (MS-PL)" not in files["LICENSE-MS-PL.txt"]:
        raise ValueError("Original Microsoft sample MS-PL license missing")
    if not files["LouderMeVirtualRenderLab.cat"]:
        raise ValueError("Inf2Cat catalog absent")
    return manifest


def staged_input(package: Path, manifest: Path, license_path: Path) -> dict[str, bytes]:
    if not package.is_dir() or package.is_symlink():
        raise ValueError("WDK package directory must be a real directory")
    found = {x.name for x in package.iterdir()}
    if found != set(PACKAGE_FILES):
        raise ValueError(f"Unexpected WDK package files: {sorted(found)}")
    result = {name: read_regular(package / name, 400 if name.endswith(".inf")
                                  else 100 if name.endswith(".cat") else 10000)
              for name in PACKAGE_FILES}
    result["LOUDERME_LAB_MANIFEST.json"] = read_regular(manifest, 300)
    result["LICENSE-MS-PL.txt"] = read_regular(license_path, 200)
    check_payload(result)
    return result


def build_zip(package: Path, manifest: Path, license_path: Path, output: Path,
              source_sha: str, run_id: str) -> None:
    if not re.fullmatch(r"[0-9a-f]{40}", source_sha):
        raise ValueError("Commit SHA must be an exact 40-character lowercase digest")
    if not re.fullmatch(r"[0-9]{5,20}", run_id):
        raise ValueError("Run ID must be supplied by GitHub Actions")
    source = staged_input(package, manifest, license_path)
    meta = check_payload(source)
    provenance = {
        "package_kind": "LouderMe UNSIGNED catalog isolated VM kernel LAB ONLY",
        "upstream_commit": meta["upstream_commit"],
        "louderme_source_sha": source_sha,
        "github_actions_run_id": run_id,
        "hardware_id": HARDWARE_ID,
        "render_endpoints": 1,
        "capture_endpoints": 0,
        "source_checksums_sha256": {name: digest(source[name]) for name in sorted(source)},
        "infverif_h_passed_upstream_in_workflow": True,
        "inf2cat_passed_upstream_in_workflow": True,
        "catalog_signed": False,
        "package_installed": False,
        "real_audio_verified": False,
        "global_audio_amplification_verified": False,
        "microsoft_store_approved": False,
    }
    source["LAB_PACKAGE_PROVENANCE.json"] = (
        json.dumps(provenance, indent=2, sort_keys=True) + "\n"
    ).encode()
    source["README-DO-NOT-INSTALL.txt"] = NOTICE.encode()
    sums = "".join(f"{digest(source[name])}  {name}\n" for name in sorted(source))
    source["SHA256SUMS.txt"] = sums.encode()
    if set(source) != set(ZIP_FILES):
        raise ValueError("Unexpected unsigned driver artifact file list")
    if output.exists():
        raise FileExistsError("Refusing to overwrite existing WDK driver artifact")
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w") as z:
        for name in sorted(source):
            info = zipfile.ZipInfo(ZIP_PREFIX + name, FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o600 << 16
            z.writestr(info, source[name], compresslevel=8)
    verify_zip(output)
    sidecar = output.with_suffix(output.suffix + ".sha256")
    if sidecar.exists():
        raise FileExistsError("Refusing to overwrite WDK package checksum")
    sidecar.write_text(f"{digest(output.read_bytes())}  {output.name}\n", encoding="ascii")


def verify_zip(path: Path) -> None:
    with zipfile.ZipFile(path, "r") as z:
        names = z.namelist()
        expected = sorted(ZIP_PREFIX + name for name in ZIP_FILES)
        if len(names) != len(set(names)) or sorted(names) != expected:
            raise ValueError("Unsigned lab ZIP contains missing, extra or duplicate entries")
        if z.testzip() is not None:
            raise ValueError("Corrupted unsigned driver ZIP")
        files = {name: z.read(ZIP_PREFIX + name) for name in ZIP_FILES}
    check_payload({name: files[name] for name in (
        *PACKAGE_FILES, "LOUDERME_LAB_MANIFEST.json", "LICENSE-MS-PL.txt")})
    try:
        provenance = json.loads(files["LAB_PACKAGE_PROVENANCE.json"])
    except (UnicodeError, json.JSONDecodeError) as exc:
        raise ValueError("Unsigned driver provenance corrupt") from exc
    for key in ("catalog_signed", "package_installed", "real_audio_verified",
                "global_audio_amplification_verified", "microsoft_store_approved"):
        if provenance.get(key) is not False:
            raise ValueError(f"Invalid source artifact release-status claim: {key}")
    if provenance.get("upstream_commit") != PINNED_UPSTREAM or (
        provenance.get("hardware_id") != HARDWARE_ID
    ):
        raise ValueError("Wrong pinned driver artifact identity")
    sums = files["SHA256SUMS.txt"].decode("ascii").splitlines()
    expected = [f"{digest(files[name])}  {name}" for name in sorted(
        n for n in ZIP_FILES if n != "SHA256SUMS.txt")]
    if sums != expected:
        raise ValueError("Unsigned driver artifact has invalid file SHA256 evidence")
    if provenance.get("source_checksums_sha256") != {
        name: digest(files[name])
        for name in sorted((*PACKAGE_FILES, "LOUDERME_LAB_MANIFEST.json",
                            "LICENSE-MS-PL.txt"))
    }:
        raise ValueError("Original WDK payload diverges from provenance")


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--package-dir", type=Path)
    p.add_argument("--manifest", type=Path)
    p.add_argument("--license", type=Path)
    p.add_argument("--output-zip", type=Path)
    p.add_argument("--source-sha")
    p.add_argument("--run-id")
    p.add_argument("--verify-zip", type=Path)
    args = p.parse_args()
    if args.verify_zip is not None:
        verify_zip(args.verify_zip)
        print("PASS: exact unsigned WDK driver lab ZIP payload and SHA-256 provenance")
        return
    if any(value is None for value in (
        args.package_dir, args.manifest, args.license,
        args.output_zip, args.source_sha, args.run_id
    )):
        p.error("Package creation needs --package-dir, --manifest, --license, "
                "--output-zip, --source-sha and --run-id")
    build_zip(args.package_dir, args.manifest, args.license,
              args.output_zip, args.source_sha, args.run_id)
    print("PASS: genuine three-file lab-only WDK package archived, checksum verified; "
          "catalog UNSIGNED, no driver installed, Store not approved")


if __name__ == "__main__":
    main()
