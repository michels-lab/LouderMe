"""Offline negative and positive tests for real Play AAB guard."""
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
from verify_play_bundle import verify_bundle  # noqa: E402

GRADLE = (ROOT / "app/build.gradle.kts").read_text(encoding="utf-8")
MAIN = (ROOT / "app/src/main/AndroidManifest.xml").read_text(encoding="utf-8")
SIDELOAD = (ROOT / "app/src/sideload/AndroidManifest.xml").read_text(encoding="utf-8")
CONTENTS = (
    "base/manifest/AndroidManifest.xml",
    "base/resources.pb",
    "base/dex/classes.dex",
)


class PlayBundlePreflightTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.path = Path(self.tmp.name) / "release.aab"

    def tearDown(self):
        self.tmp.cleanup()

    def bundle(self, entries=CONTENTS):
        with zipfile.ZipFile(self.path, "w") as z:
            for entry in entries:
                z.writestr(entry, b"test payload")

    def check(self, gradle=GRADLE, main=MAIN, side=SIDELOAD, signed=False):
        return verify_bundle(self.path, gradle, main, side, signed)

    def test_unsigned_real_bundle_shape_passes(self):
        self.bundle()
        version, code = self.check()
        self.assertRegex(version, r"^\d+\.\d+\.\d+$")
        self.assertGreater(code, 0)

    def test_signed_bundle_requires_both_jar_signature_parts(self):
        self.bundle(CONTENTS + ("META-INF/UPLOAD.SF", "META-INF/UPLOAD.RSA"))
        self.assertIsInstance(self.check(signed=True)[1], int)
        with self.assertRaises(AssertionError):
            self.check(signed=False)

    def test_unsigned_must_not_pass_signed_gate(self):
        self.bundle()
        with self.assertRaises(AssertionError):
            self.check(signed=True)

    def test_apk_or_missing_base_or_dex_are_rejected(self):
        for entries in (
            ("AndroidManifest.xml", "classes.dex"),
            ("base/manifest/AndroidManifest.xml", "base/resources.pb"),
            ("base/dex/classes.dex", "base/resources.pb"),
        ):
            with self.subTest(entries=entries):
                self.bundle(entries)
                with self.assertRaises(AssertionError):
                    self.check()

    def test_wrong_package_and_sdk_rejected(self):
        self.bundle()
        for changed in (
            GRADLE.replace('applicationId = "com.michelslab.louderme"',
                           'applicationId = "com.other.app"'),
            GRADLE.replace("targetSdk = 36", "targetSdk = 29"),
            GRADLE.replace('create("play")', 'create("another")'),
        ):
            self.assertNotEqual(changed, GRADLE)
            with self.assertRaises(AssertionError):
                self.check(gradle=changed)

    def test_sideload_install_permission_never_leaks_into_play(self):
        self.bundle()
        with self.assertRaises(AssertionError):
            self.check(main=MAIN + "REQUEST_INSTALL_PACKAGES")
        with self.assertRaises(AssertionError):
            self.check(side=SIDELOAD.replace("REQUEST_INSTALL_PACKAGES", "NO"))

    def test_missing_bundle_fails_closed(self):
        with self.assertRaises(AssertionError):
            self.check()


if __name__ == "__main__":
    unittest.main()
