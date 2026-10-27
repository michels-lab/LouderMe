"""Hermetic Windows release candidate identity tests: no device, installer or images."""
from __future__ import annotations

import copy
import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import verify_windows_shipping_provenance as provenance


class WindowsShippingBinaryProvenanceTests(unittest.TestCase):
    SOURCE = "a" * 40

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.installed = self.base / "LouderMe.exe"
        self.portable = self.base / "LouderMe-Portable-v0.1.18.exe"
        self.setup = self.base / "LouderMe-Setup-v0.1.18.exe"
        self.visual = self.base / "manifest.json"
        self.out = self.base / "shipping-provenance.json"
        self.installed.write_bytes(b"actual installed LouderMe exe" * 20)
        self.portable.write_bytes(self.installed.read_bytes())
        self.setup.write_bytes(b"actual installation package" * 60)
        self.recompute_sidecars()
        digest = provenance.hash_file(self.installed)
        self.doc = {
            "schema": "michelslab-rendered-ui-v1",
            "source_commit": self.SOURCE,
            "artifact_sha256": {"windows-desktop": digest},
            "screenshots": [
                {"platform": "windows-desktop", "viewport": viewport, "surface": surface,
                 "capture_method": "running-desktop-app",
                 "candidate_artifact_sha256": digest}
                for viewport in ("compact", "wide")
                for surface in ("home", "about", "equalizer")
            ],
        }
        self.write_manifest()

    def recompute_sidecars(self):
        for path in (self.portable, self.setup):
            Path(str(path) + ".sha256").write_text(
                f"{provenance.hash_file(path)}  {path.name}\n", encoding="ascii")

    def write_manifest(self):
        self.visual.write_text(json.dumps(self.doc), encoding="utf-8")

    def verify(self, sha=None):
        return provenance.verify(self.installed, self.portable, self.setup, self.visual,
                                 sha or self.SOURCE, self.out)

    def reject(self, label, mutation):
        mutation()
        with self.subTest(label=label):
            with self.assertRaises(ValueError, msg=label):
                self.verify()
            self.assertFalse(self.out.exists(), f"Bad proof was written: {label}")

    def test_exact_candidate_produces_immutable_digests_and_explicit_limits(self):
        result = self.verify()
        self.assertTrue(result["installed_executable_matches_portable"])
        self.assertEqual(result["installed_executable_sha256"], result["shipped_portable_sha256"])
        self.assertEqual(result["tested_installer_sha256"], provenance.hash_file(self.setup))
        self.assertEqual(result["source_commit"], self.SOURCE)
        self.assertEqual(result["screenshots"], 6)
        self.assertEqual(result["post_release_owner_visual_review"], "PENDING")
        self.assertEqual(result["physical_audio_device_test"], "NOT TESTED")
        self.assertEqual(json.loads(self.out.read_text()), result)

    def test_forged_shipping_binaries_sidecars_and_source_fail_closed(self):
        for label, mutation in (
            ("installed differs from portable", lambda: self.installed.write_bytes(b"tampered installed EXE")),
            ("portable differs from installed", lambda: self.portable.write_bytes(b"swapped portable EXE")),
            ("installer substituted", lambda: self.setup.write_bytes(b"swapped Setup EXE")),
            ("sidecar removed", lambda: Path(str(self.setup) + ".sha256").unlink()),
            ("sidecar renamed digest", lambda: Path(str(self.portable) + ".sha256").write_text("0"*64+"  "+self.portable.name)),
            ("manifest artifact digest mismatch",
             lambda: self.doc["artifact_sha256"].update({"windows-desktop": "f" * 64})),
            ("manifest source mismatch", lambda: self.doc.update({"source_commit": "b" * 40})),
            ("one screenshot wrong runtime", lambda: self.doc["screenshots"][0].update(
                {"candidate_artifact_sha256": "c" * 64})),
            ("visual coverage incomplete", lambda: self.doc["screenshots"].pop()),
            ("visual coverage duplicate", lambda: self.doc["screenshots"][0].update(
                {"surface": "about"})),
            ("fake offline screenshot", lambda: self.doc["screenshots"][0].update(
                {"capture_method": "static-mock"})),
            ("wrong capture platform", lambda: self.doc["screenshots"][0].update(
                {"platform": "android"})),
        ):
            self.setUp()
            mutation()
            self.write_manifest()
            with self.subTest(label=label):
                with self.assertRaises(ValueError):
                    self.verify()
                self.assertFalse(self.out.exists())

    def test_source_and_release_version_also_must_match(self):
        with self.assertRaises(ValueError):
            self.verify("not-a-full-source-sha")
        self.setup.rename(self.base / "LouderMe-Setup-v0.1.19.exe")
        with self.assertRaises(ValueError):
            self.verify()

    def test_both_windows_workflows_gate_exact_final_identity(self):
        for name in ("build-louderme-desktop.yml", "release-louderme-desktop.yml"):
            body = (ROOT / ".github/workflows" / name).read_text()
            with self.subTest(workflow=name):
                self.assertIn("verify_windows_shipping_provenance.py", body)
                self.assertIn("test_windows_shipping_provenance.py", body)
                self.assertIn("--installed-exe $installedExe", body)
                self.assertIn("--source-sha $env:GITHUB_SHA", body)
                self.assertIn("visual-windows/shipping-provenance.json", body)


if __name__ == "__main__":
    unittest.main()
