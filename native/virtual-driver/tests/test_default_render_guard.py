"""Read-only default-audio-route protection: static safeguards, no PnP/driver operations."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
LAB = (ROOT / "native/virtual-driver/run_disposable_lab.ps1").read_text()
GUARD = (ROOT / "native/virtual-driver/DefaultRenderGuard.ps1").read_text()
FIXTURES = (ROOT / "native/virtual-driver/test_DefaultRenderGuard.ps1").read_text()
BRIDGE = (ROOT / "native/windows-bridge/Program.cs").read_text()
NATIVE_CI = (ROOT / ".github/workflows/native-audio-engine-ci.yml").read_text()

class DefaultRenderVMGuardTests(unittest.TestCase):
    def test_source_reads_exact_default_multimedia_endpoint_and_hashes_id(self):
        for required in (
            '--default-render-fingerprint',
            'GetDefaultAudioEndpoint(',
            'DataFlow.Render, Role.Multimedia',
            'SHA256.HashData(Encoding.UTF8.GetBytes(defaultRender.ID))',
            'DEFAULT_RENDER_SHA256=',
            'args.Length != 1',
        ):
            self.assertIn(required, BRIDGE)
        self.assertNotIn('SetDefaultAudioEndpoint(', BRIDGE)

    def test_installation_preflight_and_cleanup_are_fail_closed(self):
        for required in (
            "DefaultRenderGuard.ps1",
            "Get-LouderMeDefaultRenderFingerprint -BridgeDll $BridgeDll",
            "Assert-LouderMeDefaultRenderUnchanged",
            "'driver installation'",
            "'completed audio probes'",
            "'driver cleanup'",
            "DEFAULT_RENDER_SHA256_BEFORE=",
            "default-render-identity-after-cleanup.txt",
            "restore VM snapshot",
            "Write-LouderMeLabTextOnce",
        ):
            self.assertIn(required, LAB)
        first_check = LAB.index("$defaultBefore = Get-LouderMeDefaultRenderFingerprint")
        first_install = LAB.index("& $DevconExe install")
        self.assertLess(first_check, first_install)
        after_install = LAB.index("$defaultAfterInstall = Get-LouderMeDefaultRenderFingerprint")
        first_probe = LAB.index("'--probe-virtual'")
        self.assertLess(after_install, first_probe)
        self.assertIn("} finally {", LAB)
        finally_start = LAB.index("} finally {")
        self.assertGreater(LAB.index("$defaultAfterCleanup", finally_start), finally_start)
        # A concurrent root-device addition must not be wiped out by a blanket
        # device removal command; only run-owned instance can be removed.
        self.assertIn("Where-Object { $_.InstanceId -ceq $createdInstance }", LAB)
        self.assertIn("Ambiguous partial LAB installs", LAB)

    def test_parser_and_ci_are_hardware_free(self):
        self.assertIn(r"\\ADEFAULT_RENDER_SHA256=", GUARD)
        self.assertIn("Test-LouderMeDefaultRenderFingerprint", GUARD)
        self.assertIn("Test-LouderMeDefaultRenderFingerprint", FIXTURES)
        self.assertIn("14 false-positive controls", FIXTURES)
        self.assertIn("test_DefaultRenderGuard.ps1", NATIVE_CI)
        self.assertIn("run_disposable_lab.ps1", NATIVE_CI)
        self.assertNotIn("pnputil /remove-device", GUARD)
        self.assertNotIn("SetDefault", GUARD)
        self.assertNotIn("Get-PnpDevice", GUARD)

if __name__ == "__main__":
    unittest.main()
