#!/usr/bin/env python3
"""Build and verify an isolated-VM TEST KIT from real WDK + .NET + C++ CI output.

No driver installation, code signing, certificate creation, boot-policy changes,
or audio endpoints are accessed. This deliberately CANNOT produce a release.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import zipfile
from pathlib import Path, PurePosixPath

from package_wdk_lab import verify_zip as verify_unsigned_wdk_zip

PREFIX = "LouderMe-Windows-VM-TEST-LAB/"
INNER = "driver/LouderMe-VirtualRender-LAB-UNSIGNED.zip"
SIDECAR = INNER + ".sha256"
README = "LAB-VM-README.txt"
MANIFEST = "TESTKIT-MANIFEST.json"
SUMS = "TESTKIT-SHA256SUMS.txt"
MUST_HAVE = {
    INNER, SIDECAR, README,
    "bridge/LouderMeAudioBridge.dll",
    "bridge/LouderMeAudioBridge.deps.json",
    "bridge/LouderMeAudioBridge.runtimeconfig.json",
    "bridge/louderme_audio_c.dll",
    "source/LouderMeIndependentTone.dll",
    "source/LouderMeIndependentTone.deps.json",
    "source/LouderMeIndependentTone.runtimeconfig.json",
    "lab/SignedLabCatalogGuard.ps1",
    "lab/prepare_signed_vm_lab.ps1",
    "lab/run_disposable_lab.ps1",
    "lab/LabProcessGuard.ps1",
    "lab/LabInputIntegrity.ps1",
    "lab/DefaultRenderGuard.ps1",
    "lab/LabEvidenceDirectory.ps1",
    "lab/IndependentGainCheck.ps1",
    "lab/VirtualSourceSilenceCheck.ps1",
    "lab/VirtualSourceCompletionCheck.ps1",
    "lab/VirtualSpeakerMapCheck.ps1",
    "lab/PositiveSourceCheck.ps1",
    "lab/NoSourceControlCheck.ps1",
    "lab/probe_independent_virtual_pcm.ps1",
    "lab/probe_independent_app_route.ps1",
}
FLAGS = ("driver_installed", "driver_signed_for_production",
         "real_audio_verified", "microsoft_store_approved")
FIXED_TIME = (2026, 1, 1, 0, 0, 0)
MAX_ENTRY = 35 * 1024 * 1024


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def expected_sidecar(raw: bytes, name: str) -> bytes:
    return f"{sha(raw)}  {name}\n".encode("ascii")


def check_file_name(name: str) -> None:
    p = PurePosixPath(name)
    if (not name or name.startswith("/") or "\\" in name or
            len(name) > 220 or p.as_posix() != name or
            any(x in {"", ".", ".."} for x in name.split("/"))):
        raise ValueError("Unsafe kit entry path")
    if name in (INNER, SIDECAR, README, MANIFEST, SUMS):
        return
    if name.startswith("bridge/") and p.suffix.lower() in (".dll", ".json", ".exe"):
        return
    if name.startswith("source/") and p.suffix.lower() in (".dll", ".json", ".exe"):
        return
    if name.startswith("lab/") and p.suffix.lower() == ".ps1":
        return
    raise ValueError(f"Unexpected kit entry: {name}")


def collect(stage: Path) -> dict[str, bytes]:
    if stage.is_symlink() or not stage.is_dir():
        raise ValueError("Kit staging directory must be a real directory")
    result = {}
    for entry in stage.rglob("*"):
        if entry.is_symlink():
            raise ValueError("Symbolic link refused in VM kit")
        if entry.is_dir():
            continue
        if not entry.is_file() or entry.stat().st_size > MAX_ENTRY:
            raise ValueError("Nonregular/oversized VM kit payload")
        name = entry.relative_to(stage).as_posix()
        check_file_name(name)
        result[name] = entry.read_bytes()
    return result


def validate_contents(files: dict[str, bytes], *, complete: bool) -> None:
    required = set(MUST_HAVE) | ({MANIFEST, SUMS} if complete else set())
    if not required.issubset(files):
        raise ValueError(f"Incomplete VM kit payload: {sorted(required - files.keys())}")
    lowered = [x.casefold() for x in files]
    if len(lowered) != len(set(lowered)):
        raise ValueError("Case alias collision in VM kit")
    for name, value in files.items():
        check_file_name(name)
        if not value or len(value) > MAX_ENTRY:
            raise ValueError(f"Empty or oversized kit payload: {name}")
    if files[SIDECAR].strip() != expected_sidecar(
            files[INNER], "LouderMe-VirtualRender-LAB-UNSIGNED.zip").strip():
        raise ValueError("Embedded WDK artifact SHA256 sidecar mismatches the driver")
    # Reuse the same verifier that guarded the original full-WDK unsigned ZIP.
    # Check its *actual bytes*, not a path that might be swapped after parsing.
    import io
    with zipfile.ZipFile(io.BytesIO(files[INNER])) as z:
        from package_wdk_lab import ZIP_FILES, ZIP_PREFIX, check_payload, digest
        expected = sorted(ZIP_PREFIX + x for x in ZIP_FILES)
        names = z.namelist()
        if len(names) != len(set(names)) or sorted(names) != expected:
            raise ValueError("Nested WDK ZIP has unexpected/missing members")
        if z.testzip() is not None:
            raise ValueError("Nested WDK ZIP corrupted")
        inner = {x: z.read(ZIP_PREFIX + x) for x in ZIP_FILES}
        check_payload({x: inner[x] for x in (
            "LouderMeVirtualRenderLab.inf", "LouderMeVirtualRenderLab.cat",
            "TabletAudioSample.sys", "LOUDERME_LAB_MANIFEST.json",
            "LICENSE-MS-PL.txt")})
        proof = json.loads(inner["LAB_PACKAGE_PROVENANCE.json"])
        if (proof.get("catalog_signed") is not False or
                proof.get("package_installed") is not False or
                proof.get("real_audio_verified") is not False or
                proof.get("microsoft_store_approved") is not False):
            raise ValueError("Nested driver falsely claims installation/signing")
        for n in ("LouderMeVirtualRenderLab.inf", "TabletAudioSample.sys"):
            if proof.get("source_checksums_sha256", {}).get(n) != digest(inner[n]):
                raise ValueError("WDK inner hash/provenance failed")
    if complete:
        try:
            manifest = json.loads(files[MANIFEST])
        except (ValueError, UnicodeError) as exc:
            raise ValueError("VM kit manifest is not valid JSON") from exc
        if manifest.get("kind") != "VM_OFFLINE_UNSIGNED_DRIVER_NATIVE_DSP_TEST_ONLY":
            raise ValueError("Wrong VM kit distribution kind")
        if any(manifest.get(k) is not False for k in FLAGS):
            raise ValueError("VM kit manifest misrepresents release readiness")
        if not re.fullmatch(r"[0-9a-f]{40}", manifest.get("source_sha", "")):
            raise ValueError("Missing concrete source commit")
        if not re.fullmatch(r"[0-9]{5,20}", manifest.get("actions_run_id", "")):
            raise ValueError("Missing GitHub Actions run")
        hashes = {x: sha(v) for x, v in files.items() if x not in (MANIFEST, SUMS)}
        if manifest.get("payload_sha256") != hashes:
            raise ValueError("VM kit manifest payload checksum mismatch")
        checksum_text = files[SUMS].decode("ascii")
        lines = [f"{digest}  {name}" for name, digest in sorted(hashes.items())]
        lines.append(f"{sha(files[MANIFEST])}  {MANIFEST}")
        if checksum_text.splitlines() != lines:
            raise ValueError("VM kit SHA256SUMS does not match payload")


def build(stage: Path, output: Path, source_sha: str, run_id: str) -> None:
    if not re.fullmatch(r"[0-9a-f]{40}", source_sha):
        raise ValueError("Need full lowercase source SHA")
    if not re.fullmatch(r"[0-9]{5,20}", run_id):
        raise ValueError("Need real GitHub Actions run identifier")
    if output.exists() or output.with_suffix(output.suffix + ".sha256").exists():
        raise FileExistsError("Refuse to overwrite a previous VM kit or its hash")
    files = collect(stage)
    if MANIFEST in files or SUMS in files:
        raise ValueError("CI must generate its own kit metadata, never accept staged metadata")
    validate_contents(files, complete=False)
    hashes = {k: sha(v) for k, v in sorted(files.items())}
    metadata = {
        "kind": "VM_OFFLINE_UNSIGNED_DRIVER_NATIVE_DSP_TEST_ONLY",
        "source_sha": source_sha,
        "actions_run_id": run_id,
        **{k: False for k in FLAGS},
        "payload_sha256": hashes,
    }
    files[MANIFEST] = (json.dumps(metadata, indent=2, sort_keys=True) + "\n").encode()
    sums = [f"{h}  {name}" for name, h in sorted(hashes.items())]
    sums.append(f"{sha(files[MANIFEST])}  {MANIFEST}")
    files[SUMS] = ("\n".join(sums) + "\n").encode("ascii")
    validate_contents(files, complete=True)
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as z:
        for name in sorted(files):
            info = zipfile.ZipInfo(PREFIX + name, FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o600 << 16
            z.writestr(info, files[name], compress_type=zipfile.ZIP_DEFLATED)
    verify(output)
    output.with_suffix(output.suffix + ".sha256").write_bytes(
        expected_sidecar(output.read_bytes(), output.name))


def verify(path: Path) -> None:
    if path.is_symlink() or not path.is_file() or path.stat().st_size > 80 * 1024 * 1024:
        raise ValueError("Invalid offline VM kit")
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        if len(names) != len(set(names)) or z.testzip() is not None:
            raise ValueError("Duplicate or damaged VM kit member")
        if not all(x.startswith(PREFIX) for x in names):
            raise ValueError("Untrusted VM kit ZIP prefix")
        files = {}
        for info in z.infolist():
            if info.file_size < 1 or info.file_size > MAX_ENTRY:
                raise ValueError("Invalid VM kit member size")
            name = info.filename[len(PREFIX):]
            check_file_name(name)
            if name in files:
                raise ValueError("Duplicate VM kit path")
            files[name] = z.read(info)
    validate_contents(files, complete=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--stage", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--source-sha", type=str)
    parser.add_argument("--run-id", type=str)
    parser.add_argument("--verify", type=Path)
    args = parser.parse_args()
    if args.verify:
        verify(args.verify)
        print("PASS: complete offline Windows VM driver+bridge+DSP kit verified (NOT signed/installed).")
        return
    if not all([args.stage, args.output, args.source_sha, args.run_id]):
        parser.error("Building requires --stage, --output, --source-sha, --run-id")
    build(args.stage, args.output, args.source_sha, args.run_id)
    print("PASS: source-pinned unsigned WDK+native audio VM kit built; NOT signed or installed.")


if __name__ == "__main__":
    main()
