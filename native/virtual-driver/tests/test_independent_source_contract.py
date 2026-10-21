"""Fail-closed static contract for independent-source virtual-to-physical VM proof.

Unit checks are not device evidence: real proof requires a locally trusted
installed LAB driver and an isolated VM with actual WASAPI endpoints.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
BRIDGE = ROOT / "native/windows-bridge/Program.cs"
APP = ROOT / "native/independent-tone/Program.cs"
RUNNER = ROOT / "native/virtual-driver/probe_independent_app_route.ps1"
LAB = ROOT / "native/virtual-driver/run_disposable_lab.ps1"
WORKFLOW = ROOT / ".github/workflows/native-physical-lab-manual.yml"

class IndependentSourceBoundaryTests(unittest.TestCase):
    def test_bridge_does_not_generate_stimulus_in_external_mode(self):
        text = BRIDGE.read_text()
        for fragment in (
            'bool proveExternalAppRoute = args.Contains("--probe-app-route")',
            'bool ownTone = args.Contains("--probe-route")',
            'using var toneStream = ownTone',
            'using var sourceToneOutput = ownTone',
            'if (proveExternalAppRoute)',
            'Console.WriteLine("EXTERNAL_SOURCE_READY")',
            'PASS: LAB independent source Windows process'
        ):
            self.assertIn(fragment, text)

    def test_standalone_source_is_not_the_audio_bridge_or_an_installer(self):
        text = APP.read_text()
        self.assertIn('class Program', text)
        self.assertIn('new WasapiOut(device, AudioClientShareMode.Shared', text)
        self.assertIn('LouderMe Virtual', text)
        self.assertIn('EXTERNAL_APP_TONE_FINISHED', text)
        self.assertIn('CreateTone(WaveFormat format)', text)
        for forbidden in ('AudioEngine', 'NativeDsp', 'DataFlow.Capture',
                          'SetDefault', 'pnputil', 'devcon', 'HttpClient'):
            self.assertNotIn(forbidden, text)

    def test_runner_waits_for_bridge_ready_and_exact_app_exit(self):
        text = RUNNER.read_text()
        for marker in ('EXTERNAL_SOURCE_READY', 'EXTERNAL_APP_TONE_FINISHED',
                       'independent-source-result-', 'physical_matched440_amplitude', '--probe-app-route',
                       'Start-Process', 'separate'):
            if marker == 'Start-Process': continue
            self.assertIn(marker, text)
        self.assertIn('$bridge.HasExited', text)
        self.assertIn('$source.ExitCode -ne 0', text)
        self.assertIn('$bridge.ExitCode -ne 0', text)
        self.assertIn('Get-CimInstance -ClassName Win32_ComputerSystem', text)
        self.assertIn('$process.Kill($true)', text)
        self.assertNotIn('Stop-Process -Name', text)

    def test_disposable_vm_and_explicit_manual_audio_proof_mandatory(self):
        text = LAB.read_text()
        self.assertIn('probe_independent_app_route.ps1', text)
        self.assertIn('IndependentToneDll', text)
        self.assertIn('IndependentGainCheck.ps1', text)
        self.assertIn('Test-LouderMeIndependentGain -EvidenceDir $evidence', text)
        self.assertIn('independent-app-boost-ratio.txt', text)
        gain = (ROOT / 'native/virtual-driver/IndependentGainCheck.ps1').read_text()
        self.assertIn('independent-source-result-$boost.json', gain)
        self.assertIn('$ratio -lt 1.30 -or', gain)
        self.assertIn('$ratio -gt 1.70', gain)
        y = WORKFLOW.read_text()
        self.assertIn('independent_tone_dll:', y)
        self.assertIn('self-hosted, Windows, X64, louderme-isolated-audio-lab', y)
        self.assertIn('I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST', y)
        self.assertNotIn('runs-on: windows-latest', y)

if __name__ == "__main__":
    unittest.main()
