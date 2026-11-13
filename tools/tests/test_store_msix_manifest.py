"""MSIX preflight manifest contract; no certificate or Store account required."""
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "desktop/tools"))
from create_store_manifest import ASSETS, check_manifest, create_manifest  # noqa: E402

ID = "MichelsLab.LouderMe.Preflight"
PUBLISHER = "CN=LouderMe Store Preflight"
VERSION = "0.1.19.0"


class StoreManifestTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.stage = Path(self.tmp.name)
        (self.stage / "Assets").mkdir()
        (self.stage / "LouderMe.exe").write_bytes(b"realexe-fixture")
        (self.stage / "native").mkdir()
        (self.stage / "native" / "LouderMeAudioBridge.exe").write_bytes(b"realbridge-fixture")
        (self.stage / "native" / "louderme_audio_c.dll").write_bytes(b"realdsp-fixture")
        for name in ASSETS:
            (self.stage / "Assets" / name).write_bytes(b"P" * 200)
        self.manifest = self.stage / "AppxManifest.xml"

    def tearDown(self):
        self.tmp.cleanup()

    def test_preflight_is_full_trust_and_contains_all_tiles(self):
        create_manifest(ID, PUBLISHER, VERSION, self.manifest, preflight=True)
        check_manifest(self.manifest, self.stage, ID, PUBLISHER, VERSION)

    def test_placeholder_never_masquerades_as_partner_center_identity(self):
        with self.assertRaises(ValueError):
            create_manifest(ID, PUBLISHER, VERSION, self.manifest)
        with self.assertRaises(ValueError):
            create_manifest("MichelsLab.LouderMe", "NOT-A-DN", VERSION, self.manifest)
        with self.assertRaises(ValueError):
            create_manifest("Other&Name", PUBLISHER, VERSION, self.manifest)
        with self.assertRaises(ValueError):
            create_manifest(ID, PUBLISHER, "0.1.19", self.manifest, preflight=True)

    def test_missing_runtime_or_tile_must_fail(self):
        create_manifest(ID, PUBLISHER, VERSION, self.manifest, preflight=True)
        (self.stage / "Assets" / "StoreLogo.png").unlink()
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, ID, PUBLISHER, VERSION)
        (self.stage / "Assets" / "StoreLogo.png").write_bytes(b"Q" * 200)
        (self.stage / "LouderMe.exe").unlink()
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, ID, PUBLISHER, VERSION)
        (self.stage / "LouderMe.exe").write_bytes(b"realexe-fixture")
        (self.stage / "native" / "LouderMeAudioBridge.exe").unlink()
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, ID, PUBLISHER, VERSION)
        (self.stage / "native" / "LouderMeAudioBridge.exe").write_bytes(b"realbridge-fixture")
        (self.stage / "native" / "louderme_audio_c.dll").unlink()
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, ID, PUBLISHER, VERSION)

    def test_wrong_identity_cannot_pass_attestation(self):
        create_manifest(ID, PUBLISHER, VERSION, self.manifest, preflight=True)
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, "MichelsLab.Different", PUBLISHER, VERSION)
        with self.assertRaises(AssertionError):
            check_manifest(self.manifest, self.stage, ID, PUBLISHER, "0.1.20.0")


if __name__ == "__main__":
    unittest.main()
