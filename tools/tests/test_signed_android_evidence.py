"""Hermetic signed Android artifact-to-rendered-screenshot provenance regressions.

These tests DO NOT substitute for the actual Android emulator screenshot gate.
"""
from __future__ import annotations

import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
from verify_signed_android_evidence import verify


class SignedAndroidEvidenceContract(unittest.TestCase):
    SHA = "a" * 40

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.apk = self.base / "LouderMe-v0.1.12-sideload.apk"
        self.apk.write_bytes(b"release-signed-exact-fixture" * 25_000)
        self.manifest = self.base / "manifest.json"
        apk_sha = hashlib.sha256(self.apk.read_bytes()).hexdigest()
        self.doc = {
            "schema": "michelslab-rendered-ui-v1",
            "source_commit": self.SHA,
            "artifact_sha256": {"android": apk_sha},
            "screenshots": [],
        }
        for viewport in ("compact", "wide"):
            for surface in ("home", "about"):
                file = self.base / f"{surface}-{viewport}.png"
                file.write_bytes((f"{surface} {viewport} genuine pixels".encode()) * 175)
                self.doc["screenshots"].append({
                    "platform": "android",
                    "viewport": viewport,
                    "surface": surface,
                    "path": file.name,
                    "capture_method": "installed-android-emulator",
                    "candidate_artifact_sha256": apk_sha,
                    "sha256": hashlib.sha256(file.read_bytes()).hexdigest(),
                })
        self.save()

    def save(self):
        self.manifest.write_text(json.dumps(self.doc), encoding="utf-8")

    def reject(self, mutate):
        mutate()
        self.save()
        with self.assertRaises((ValueError, OSError)):
            verify(self.manifest, self.apk, self.SHA)

    def test_real_signed_candidate_matches_all_expected_sources(self):
        verify(self.manifest, self.apk, self.SHA)

    def test_fails_closed_on_forged_signed_apk_evidence(self):
        mutations = [
            ("APK modified after screenshot", lambda: self.apk.write_bytes(b"swapped APK" * 70_000)),
            ("other source commit", lambda: self.doc.update({"source_commit": "b" * 40})),
            ("wrong source schema", lambda: self.doc.update({"schema": "legacy"})),
            ("old unsigned screenshot digest", lambda: self.doc["screenshots"][0].update(
                {"candidate_artifact_sha256": "b" * 64})),
            ("manifest signed artifact digest forged", lambda: self.doc["artifact_sha256"].update(
                {"android": "a" * 64})),
            ("missing viewport screenshot", lambda: self.doc["screenshots"].pop()),
            ("duplicate Home conceals About", lambda: self.doc["screenshots"][1].update(
                {"surface": "home"})),
            ("fake static screenshot", lambda: self.doc["screenshots"][0].update(
                {"capture_method": "mock-image"})),
            ("wrong application platform", lambda: self.doc["screenshots"][0].update(
                {"platform": "windows-desktop"})),
            ("wrong image bytes", lambda: (self.base / "home-compact.png").write_bytes(b"tampered")),
            ("missing image file", lambda: (self.base / "home-compact.png").unlink()),
            ("path traversal", lambda: self.doc["screenshots"][0].update(
                {"path": "../different-file.png"})),
        ]
        for label, mutation in mutations:
            self.setUp()
            with self.subTest(label=label):
                self.reject(mutation)

    def test_reject_nonfinal_apk_and_invalid_source_argument(self):
        with self.assertRaises(ValueError):
            verify(self.manifest, self.apk, "short")
        renamed = self.apk.with_name("LouderMe-v0.1.12-sideload-debug.apk")
        self.apk.rename(renamed)
        with self.assertRaises(ValueError):
            verify(self.manifest, renamed, self.SHA)

    def test_final_signed_visual_workflow_enforces_exact_source(self):
        workflow = (ROOT / ".github/workflows/android-ci-release.yml").read_text()
        self.assertIn("--source-sha \"$GITHUB_SHA\"", workflow)
        self.assertIn("test_signed_android_evidence.py", workflow)
        self.assertIn("verify_signed_android_evidence.py", workflow)
        self.assertIn("signed_visual_qa:", workflow)
        self.assertIn("needs: [validate, release_guard, sign_sideload]", workflow)


if __name__ == "__main__":
    unittest.main()
