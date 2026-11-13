"""Device-free adversarial tests of the single offline Windows VM test kit."""
import json
import pathlib
import shutil
import sys
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from test_package_wdk_lab import PinnedWdkArtifactTests
import build_vm_testkit as vm


class VmKitTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.tmp.name)
        self.wdk = PinnedWdkArtifactTests()
        self.wdk.setUp()
        self.wdk.build()
        self.stage = self.root / "stage"
        self.stage.mkdir()
        self.kit = self.root / "LouderMe-Windows-VM-TEST-LAB.zip"
        for n in vm.MUST_HAVE:
            f = self.stage / n
            f.parent.mkdir(parents=True, exist_ok=True)
            if n == vm.INNER:
                f.write_bytes(self.wdk.output.read_bytes())
            elif n == vm.SIDECAR:
                f.write_bytes(vm.expected_sidecar(
                    self.wdk.output.read_bytes(), "LouderMe-VirtualRender-LAB-UNSIGNED.zip"))
            elif n.endswith(".json"):
                f.write_text('{"runtimeOptions": {"tfm": "net10.0"}}')
            elif n.endswith(".dll"):
                f.write_bytes(b"MZ" + n.encode())
            else:
                f.write_text("VM lab fixture " + n)

    def tearDown(self):
        self.wdk.tearDown()
        self.tmp.cleanup()

    def build(self):
        vm.build(self.stage, self.kit, "0" * 40, "43329002049")

    def alter(self, name, payload):
        with zipfile.ZipFile(self.kit) as z:
            files = {x: z.read(x) for x in z.namelist()}
        files[vm.PREFIX + name] = payload
        self.kit.unlink()
        with zipfile.ZipFile(self.kit, "w") as z:
            for n, raw in files.items():
                z.writestr(n, raw)

    def test_real_unsigned_wdk_and_native_bridge_bundle_contract(self):
        self.build()
        vm.verify(self.kit)
        with zipfile.ZipFile(self.kit) as z:
            manifest = json.loads(z.read(vm.PREFIX + vm.MANIFEST))
        self.assertFalse(manifest["real_audio_verified"])
        self.assertFalse(manifest["driver_signed_for_production"])
        self.assertEqual(manifest["actions_run_id"], "43329002049")
        self.assertTrue(self.kit.with_suffix(".zip.sha256").is_file())
        with self.assertRaises(FileExistsError):
            self.build()

    def test_mutated_native_dsp_and_manifest_claims_are_refused(self):
        self.build()
        self.alter("bridge/louderme_audio_c.dll", b"MZ tampered")
        with self.assertRaises(ValueError):
            vm.verify(self.kit)
        self.kit.unlink()
        self.kit.with_suffix(".zip.sha256").unlink()
        self.build()
        with zipfile.ZipFile(self.kit) as z:
            info = json.loads(z.read(vm.PREFIX + vm.MANIFEST))
        info["real_audio_verified"] = True
        self.alter(vm.MANIFEST, json.dumps(info).encode())
        with self.assertRaises(ValueError):
            vm.verify(self.kit)

    def test_sidecar_or_unsigned_driver_tampering_is_refused(self):
        sidecar = self.stage / vm.SIDECAR
        sidecar.write_text("0" * 64 + "  LouderMe-VirtualRender-LAB-UNSIGNED.zip")
        with self.assertRaises(ValueError):
            self.build()
        sidecar.write_bytes(vm.expected_sidecar(
            self.wdk.output.read_bytes(), "LouderMe-VirtualRender-LAB-UNSIGNED.zip"))
        nested = self.stage / vm.INNER
        nested.write_bytes(b"not a genuine unsigned WDK ZIP")
        with self.assertRaises((ValueError, zipfile.BadZipFile)):
            self.build()

    def test_missing_real_source_or_own_dsp_refused(self):
        (self.stage / "source/LouderMeIndependentTone.dll").unlink()
        with self.assertRaises(ValueError):
            self.build()
        (self.stage / "source/LouderMeIndependentTone.dll").write_bytes(b"MZ")
        (self.stage / "bridge/louderme_audio_c.dll").unlink()
        with self.assertRaises(ValueError):
            self.build()

    def test_extra_executable_symlink_path_and_case_alias_refused(self):
        extra = self.stage / "evil.exe"
        extra.write_bytes(b"MZ")
        with self.assertRaises(ValueError):
            self.build()
        extra.unlink()
        alias = self.stage / "bridge/louderme_aUdio_c.DLL"
        alias.write_bytes(b"MZ")
        with self.assertRaises(ValueError):
            self.build()
        alias.unlink()
        sym = self.stage / "lab/junction.ps1"
        try:
            sym.symlink_to(self.stage / "lab/run_disposable_lab.ps1")
        except OSError:
            return
        with self.assertRaises(ValueError):
            self.build()

    def test_unexpected_zip_members_or_traversal_rejected(self):
        self.build()
        with zipfile.ZipFile(self.kit, "a") as z:
            z.writestr(vm.PREFIX + "../escape.exe", b"MZ")
        with self.assertRaises(ValueError):
            vm.verify(self.kit)


if __name__ == "__main__":
    unittest.main()
