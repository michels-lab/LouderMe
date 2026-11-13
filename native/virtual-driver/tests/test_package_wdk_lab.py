"""No-WDK adversarial tests for an unsigned, immutable SysVAD lab handoff."""
from __future__ import annotations

import json
import pathlib
import sys
import tempfile
import unittest
import warnings
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import package_wdk_lab as pkg  # noqa: E402


def valid_manifest() -> dict:
    return {
        "lab_only": True,
        "upstream_repository": "microsoft/Windows-driver-samples",
        "upstream_commit": pkg.PINNED_UPSTREAM,
        "hardware_id": pkg.HARDWARE_ID,
        "service": pkg.SERVICE_NAME,
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
        "patched_files": {f"file-{i}": {"sha256": "a" * 64} for i in range(10)},
    }


class PinnedWdkArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.temp.name)
        self.package = self.root / "package"
        self.package.mkdir()
        self.inf = self.package / pkg.PACKAGE_FILES[0]
        self.cat = self.package / pkg.PACKAGE_FILES[1]
        self.sys = self.package / pkg.PACKAGE_FILES[2]
        self.manifest = self.root / "original_manifest.json"
        self.license = self.root / "LICENSE"
        self.output = self.root / "LouderMe-Virtual-LAB-UNSIGNED.zip"
        self.sys.write_bytes(b"MZ" + bytes(range(256)) * 48)
        self.cat.write_bytes(b"unsigned Inf2Cat fixture" * 20)
        self.inf.write_text(
            ("[Version]\nCatalogFile=LouderMeVirtualRenderLab.cat\n"
             "KmdfLibraryVersion=1.33\n[Manufacturer]\n" +
             f"{pkg.HARDWARE_ID}\n" +
             f"{pkg.SERVICE_NAME}\n" +
             "LouderME Virtual Speaker\n"
             "TabletAudioSample.sys\n") * 5,
            encoding="utf-8")
        self.manifest.write_text(json.dumps(valid_manifest()), encoding="utf-8")
        self.license.write_text(
            "The Microsoft Public License (MS-PL)\n" + "Original license text\n" * 30,
            encoding="utf-8")

    def tearDown(self):
        self.temp.cleanup()

    def build(self):
        pkg.build_zip(
            self.package, self.manifest, self.license,
            self.output, "0" * 40, "43329002049")

    def tamper(self, name, altered):
        with zipfile.ZipFile(self.output) as z:
            old = {n: z.read(n) for n in z.namelist()}
        old[pkg.ZIP_PREFIX + name] = altered
        self.output.unlink()
        with zipfile.ZipFile(self.output, "w") as z:
            for n, data in old.items():
                z.writestr(n, data)

    def test_real_shape_creates_checksummed_uninstallable_lab_handoff(self):
        self.build()
        pkg.verify_zip(self.output)
        sidecar = self.output.with_suffix(".zip.sha256")
        self.assertIn(pkg.digest(self.output.read_bytes()), sidecar.read_text())
        with zipfile.ZipFile(self.output) as z:
            items = z.namelist()
            self.assertEqual(len(items), len(pkg.ZIP_FILES))
            self.assertEqual(len(items), len(set(items)))
            proof = json.loads(z.read(pkg.ZIP_PREFIX + "LAB_PACKAGE_PROVENANCE.json"))
            self.assertFalse(proof["catalog_signed"])
            self.assertFalse(proof["real_audio_verified"])
            self.assertFalse(proof["microsoft_store_approved"])
            self.assertEqual(proof["github_actions_run_id"], "43329002049")
            self.assertIn("NOT an installer", z.read(
                pkg.ZIP_PREFIX + "README-DO-NOT-INSTALL.txt").decode())
        with self.assertRaises(FileExistsError):
            self.build()

    def test_missing_extra_or_symlinked_kernel_files_refused(self):
        self.sys.unlink()
        with self.assertRaises(ValueError):
            self.build()
        self.sys.write_bytes(b"MZ" + b"A" * 10001)
        (self.package / "evil.exe").write_bytes(b"MZ")
        with self.assertRaises(ValueError):
            self.build()
        (self.package / "evil.exe").unlink()
        self.sys.unlink()
        try:
            self.sys.symlink_to(self.cat)
        except (NotImplementedError, OSError):
            return
        with self.assertRaises(ValueError):
            self.build()

    def test_no_unsigned_sys_or_wrong_pinned_source_accepted(self):
        self.sys.write_bytes(b"bad-sys" * 1500)
        with self.assertRaises(ValueError):
            self.build()
        self.sys.write_bytes(b"MZ" + b"\x00" * 10000)
        manifest = valid_manifest()
        manifest["upstream_commit"] = "f" * 40
        self.manifest.write_text(json.dumps(manifest))
        with self.assertRaises(ValueError):
            self.build()

    def test_forbid_demo_identity_or_stale_inf(self):
        self.inf.write_text(
            self.inf.read_text().replace(
                pkg.HARDWARE_ID, r"Root\sysvad_ComponentizedAudioSample"))
        with self.assertRaises(ValueError):
            self.build()

    def test_false_claims_forbidden_in_manifest(self):
        for key in (
            "driver_signed", "driver_installed", "real_audio_verified",
            "recording_to_file_enabled", "sample_tone_generator_enabled",
        ):
            with self.subTest(key=key):
                manifest = valid_manifest()
                manifest[key] = True
                self.manifest.write_text(json.dumps(manifest))
                with self.assertRaises(ValueError):
                    self.build()

    def test_invalid_commit_or_missing_run_refused(self):
        with self.assertRaises(ValueError):
            pkg.build_zip(self.package, self.manifest, self.license,
                          self.output, "not-a-commit", "43329002049")
        with self.assertRaises(ValueError):
            pkg.build_zip(self.package, self.manifest, self.license,
                          self.output, "0" * 40, "")
        self.assertFalse(self.output.exists())

    def test_modified_inf_or_missing_license_not_accepted(self):
        self.inf.write_text(self.inf.read_text().replace("KmdfLibraryVersion=1.33",
                                                         "KmdfLibraryVersion=1.11"))
        with self.assertRaises(ValueError):
            self.build()
        self.inf.write_text(
            self.inf.read_text().replace("KmdfLibraryVersion=1.11",
                                         "KmdfLibraryVersion=1.33"))
        self.license.write_text("wrong license" * 50)
        with self.assertRaises(ValueError):
            self.build()

    def test_corrupted_or_forged_zip_signatures_and_status_rejected(self):
        self.build()
        with zipfile.ZipFile(self.output) as z:
            provenance = json.loads(z.read(
                pkg.ZIP_PREFIX + "LAB_PACKAGE_PROVENANCE.json"))
        provenance["microsoft_store_approved"] = True
        self.tamper("LAB_PACKAGE_PROVENANCE.json", json.dumps(provenance).encode())
        with self.assertRaises(ValueError):
            pkg.verify_zip(self.output)
        self.output.unlink()
        self.output.with_suffix(".zip.sha256").unlink()
        self.build()
        with zipfile.ZipFile(self.output) as z:
            old = z.read(pkg.ZIP_PREFIX + "TabletAudioSample.sys")
        self.tamper("TabletAudioSample.sys", old + b"\x00")
        with self.assertRaises(ValueError):
            pkg.verify_zip(self.output)

    def test_extra_and_duplicate_zip_members_rejected(self):
        self.build()
        with zipfile.ZipFile(self.output, "a") as z:
            z.writestr("evil/extra.exe", b"MZ")
        with self.assertRaises(ValueError):
            pkg.verify_zip(self.output)
        self.output.unlink()
        self.output.with_suffix(".zip.sha256").unlink()
        self.build()
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", UserWarning)
            with zipfile.ZipFile(self.output, "a") as z:
                z.writestr(pkg.ZIP_PREFIX + "TabletAudioSample.sys", b"MZ")
        with self.assertRaises(ValueError):
            pkg.verify_zip(self.output)


if __name__ == "__main__":
    unittest.main()
