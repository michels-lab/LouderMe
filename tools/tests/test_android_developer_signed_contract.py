"""Safety contract for the opt-in separately signed Android Developer APK.

This checks source guardrails only; installed signed APK evidence must come
from a successful manual GitHub Actions workflow with a pinned owner signer.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class SignedDeveloperContract(unittest.TestCase):
    def test_public_source_can_never_dispatch_owner_signing(self):
        self.assertFalse(
            (ROOT / ".github/workflows/developer-android-signed-owner-qa.yml").exists(),
            "Dedicated owner key must never be used from the PUBLIC LouderMe source repo",
        )
        self.assertTrue(
            (ROOT / "infra/private-developer-signer/.github/workflows/developer-android-signed-owner-qa.yml").is_file()
        )

    def test_signed_qa_is_manual_private_and_no_public_release(self):
        source = (ROOT / "infra/private-developer-signer/.github/workflows/developer-android-signed-owner-qa.yml").read_text()
        for required in (
            "workflow_dispatch:",
            "repository: michels-lab/LouderMe",
            "inputs.source_commit_sha", "git merge-base --is-ancestor", "github.ref == 'refs/heads/main'",
            "github.event.repository.private == true", "contents: read",
            "LOUDERME_DEVELOPER_KEYSTORE_BASE64",
            "LOUDERME_DEVELOPER_STORE_PASSWORD",
            "LOUDERME_DEVELOPER_KEY_ALIAS",
            "LOUDERME_DEVELOPER_KEY_PASSWORD",
            "LOUDERME_DEVELOPER_CERT_SHA256",
            "assembleDeveloperRelease", "apksigner", "sha256sum --check",
            "--upgrade-apk", "actual_inplace_upgrade",
            "needs.verify_installed_upgrade.result == 'success'",
            "LouderMe-Developer-QA-PASSED-", "retention-days: 7",
        ):
            self.assertIn(required, source, required)
        for forbidden in ("  push:", "  schedule:", "gh release create",
                          "gh release upload", "secrets.LOUDERME_SIDELOAD_"):
            self.assertNotIn(forbidden, source, forbidden)

    def test_developer_versions_do_not_replace_public_versions(self):
        source = (ROOT / "app/build.gradle.kts").read_text()
        self.assertIn("developerVersionCodeOverride", source)
        self.assertIn("applicationIdSuffix = \".developer\"", source)
        name = re.search(r'\\bversionName\\s*=\\s*"([0-9]+\\.[0-9]+\\.[0-9]+)"', source)
        code = re.search(r'\\bversionCode\\s*=\\s*([0-9]+)\\b', source)
        self.assertIsNotNone(name, "Public versionName must be explicit")
        self.assertIsNotNone(code, "Public versionCode must be explicit")
        self.assertGreater(int(code.group(1)), 13)
        self.assertGreater(tuple(map(int, name.group(1).split("."))), (0, 1, 12))
        self.assertIn("developerVersionCodeOverride?.let { versionCode = it }", source)
        self.assertNotRegex(source,
                            r'versionCode\\s*=\\s*developerVersionCodeOverride\\s*\\?:\\s*[0-9]+')
        self.assertIn('buildConfigField("boolean", "DEVELOPER_EDITION", "true")', source)

    def test_regression_developer_flavor_cannot_fallback_to_historical_public_code(self):
        source = (ROOT / "app/build.gradle.kts").read_text()
        # Original implementation pinned v0.1.12 even after public Gradle
        # version advances. This is the exact behavior we must not restore.
        broken = source.replace(
            "developerVersionCodeOverride?.let { versionCode = it }",
            "versionCode = developerVersionCodeOverride ?: 13")
        self.assertNotEqual(source, broken)
        self.assertRegex(broken,
                         r'versionCode\\s*=\\s*developerVersionCodeOverride\\s*\\?:\\s*13')
        self.assertNotRegex(source,
                            r'versionCode\\s*=\\s*developerVersionCodeOverride\\s*\\?:\\s*13')

    def test_real_installed_upgrade_must_not_uninstall_or_reset_annotations(self):
        source = (ROOT / "tools/capture_android_developer.py").read_text()
        self.assertIn('adb("install", "-r", str(upgrade_apk.resolve()))', source)
        self.assertIn("upgraded_code <= prior_code", source)
        self.assertIn("developer-note-after-signed-upgrade", source)
        self.assertNotIn('adb("uninstall"', source)

if __name__ == "__main__":
    unittest.main()
