"""Offline security contracts. NEVER read or create real Android signing credentials."""
from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from audit_android_signing_artifacts import (
    verify_workflow, suspicious_artifacts, fetch_artifacts, audit_legacy_id
)

WORKFLOW = (ROOT / ".github/workflows/android-ci-release.yml").read_text()


class AndroidSigningArtifactHygieneTests(unittest.TestCase):
    def test_current_signing_keeps_pinned_cert_secrets_and_no_vault_upload(self):
        verify_workflow(WORKFLOW)
        for rejected in (
            WORKFLOW.replace("EXPECTED_CERT_SHA256=", "UNPINNED_CERT_SHA256="),
            WORKFLOW.replace("LOUDERME_SIDELOAD_KEY_ALIAS", "WRONG_KEY_NAME"),
            WORKFLOW.replace("Erase ephemeral keystore after signing", "Key never removed"),
            WORKFLOW + "\n      - name: Leak private JKS\n        uses: actions/upload-artifact@v4\n        with:\n          path: signing/louderme-sideload.jks\n",
            WORKFLOW + "\n      - name: Upload password recovery\n        uses: actions/upload-artifact@v4\n        with:\n          path: signing/github-actions-secrets.txt\n",
            WORKFLOW + "\n      - name: Backup old artifact\n        uses: actions/upload-artifact@v4\n        with:\n          path: credentials/secret-backup.zip\n",
            WORKFLOW + "\n      - name: Fake normal vault\n        run: echo signing/legacy-vault\n",
        ):
            with self.subTest(fingerprint=rejected[-60:]):
                with self.assertRaises(ValueError):
                    verify_workflow(rejected)

    def test_secret_vault_name_scanner_does_not_flag_normal_signed_release(self):
        candidates = [
            {"id": 11, "name": "LouderMe-Android-v0.1.12", "expired": False},
            {"id": 12, "name": "LouderMe-v0.1.12-stable-sideload", "expired": False},
            {"id": 13, "name": "LouderMe-Android-Signed-Rendered-UI", "expired": False},
            {"id": 14, "name": "signing-vault-private", "expired": False},
            {"id": 15, "name": "KEYSTORE_BACKUP", "expired": False},
            {"id": 16, "name": "github-actions-secrets.txt", "expired": False},
            {"id": 17, "name": "legacy-vault-old", "expired": True},
        ]
        self.assertEqual({14, 15, 16}, {x["id"] for x in suspicious_artifacts(candidates)})

    def test_all_pages_are_read_and_forged_partial_or_unauthorized_scan_rejected(self):
        urls = []
        def api(path):
            urls.append(path)
            if path.endswith("&page=1"): return 200, {"artifacts": [{"name":"normal"}] * 100}
            if path.endswith("&page=2"): return 200, {"artifacts": [{"name":"signing-vault"}]}
            return 404, {}
        result = fetch_artifacts("michels-lab/LouderMe", api)
        self.assertEqual(101, len(result))
        self.assertEqual(1, len(suspicious_artifacts(result)))
        self.assertEqual(2, len(urls))
        with self.assertRaises(ValueError):
            fetch_artifacts("../different/repository", api)
        with self.assertRaises(RuntimeError):
            fetch_artifacts("michels-lab/LouderMe", lambda _: (403, {}))
        with self.assertRaises(RuntimeError):
            fetch_artifacts("michels-lab/LouderMe", lambda _: (200, {"artifacts": []}), 0)

    def test_owner_controlled_legacy_artifact_id_never_downloads_archive(self):
        calls = []
        def api(path):
            calls.append(path)
            if path.endswith("/11223344556"): return 200, {"expired": False}
            if path.endswith("/11223344557"): return 200, {"expired": True}
            if path.endswith("/11223344558"): return 404, {}
            return 403, {}
        repo = "michels-lab/LouderMe"
        self.assertTrue(audit_legacy_id(repo, "11223344556", api))
        self.assertFalse(audit_legacy_id(repo, "11223344557", api))
        self.assertFalse(audit_legacy_id(repo, "11223344558", api))
        self.assertFalse(audit_legacy_id(repo, "", api))
        for invalid in ("xyz", "0", "-1", "1;rm", "1" * 22):
            with self.assertRaises(ValueError):
                audit_legacy_id(repo, invalid, api)
        with self.assertRaises(RuntimeError):
            audit_legacy_id(repo, "11223344559", api)
        self.assertEqual(4, len(calls))
        self.assertFalse(any(path.endswith("/zip") for path in calls))


if __name__ == "__main__":
    unittest.main()
