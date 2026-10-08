"""Fast source-derivation guards; real Microsoft sample integration runs in CI."""
import importlib.util
import pathlib
import unittest

HERE = pathlib.Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location(
    "prepare_sysvad", HERE.parent / "prepare_sysvad.py"
)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class DriverSourceDerivationTests(unittest.TestCase):
    def test_only_one_render_and_no_capture(self):
        sample = (
            "SpeakerMiniports =\n{\n"
            "    ENDPOINT_OFFLOAD_SUPPORTED,\n};\n"
            "PENDPOINT_MINIPAIR  g_RenderEndpoints[] = {\n"
            " &SpeakerMiniports,\n &SpeakerHpMiniports,\n};\n"
            "#define g_cRenderEndpoints (SIZEOF_ARRAY(g_RenderEndpoints))\n"
            "PENDPOINT_MINIPAIR  g_CaptureEndpoints[] = {\n"
            " &MicInMiniports,\n &MicArray1Miniports,\n};\n"
            "#define g_cCaptureEndpoints (SIZEOF_ARRAY(g_CaptureEndpoints))\n"
        )
        actual = MODULE.patch_minipairs(sample)
        self.assertIn("ENDPOINT_LOOPBACK_SUPPORTED", actual)
        render = actual.split("g_RenderEndpoints[]", 1)[1].split("};", 1)[0]
        self.assertEqual(render.count("&SpeakerMiniports"), 1)
        self.assertNotIn("SpeakerHp", render)
        self.assertIn("g_cCaptureEndpoints = 0", actual)
        self.assertIn("nullptr", actual)

    def test_reject_upstream_endpoint_drift(self):
        with self.assertRaises(ValueError):
            MODULE.patch_minipairs("g_RenderEndpoints[] = {unknown};")

    def test_inf_keeps_only_speaker_interfaces_and_unique_hwid(self):
        sample = r'''%SYSVAD_SA.DeviceDesc%=SYSVAD_SA, Root\sysvad_ComponentizedAudioSample
[SYSVAD_SA.NT.Interfaces]
AddInterface=%KSCATEGORY_AUDIO%,%KSNAME_WaveSpeaker%,SYSVAD.I.WaveSpeaker
AddInterface=%KSCATEGORY_AUDIO%,%KSNAME_TopologySpeaker%,SYSVAD.I.TopologySpeaker
AddInterface=%KSCATEGORY_CAPTURE%,%KSNAME_WaveMicIn%,SYSVAD.I.WaveMicIn
[SYSVAD_SA.NT.Services]
AddService=sysvad_componentizedaudiosample,0x00000002,sysvad_ComponentizedAudioSample_Service_Inst
[sysvad_ComponentizedAudioSample_Service_Inst]
[Strings]
ProviderName = "TODO-Set-Provider"
MfgName      = "TODO-Set-Manufacturer"
SYSVAD_SA.DeviceDesc="Virtual Audio Device (WDM) - Tablet Sample"
SYSVAD_ComponentizedAudioSample.SvcDesc="Virtual Audio Device (WDM) - Tablet Sample Driver"
SYSVAD.WaveSpeaker.szPname="SYSVAD Wave Speaker"
SYSVAD.TopologySpeaker.szPname="SYSVAD Topology Speaker"
'''
        actual = MODULE.patch_inf(sample, False)
        self.assertIn(MODULE.HARDWARE_ID, actual)
        self.assertIn(MODULE.SERVICE_NAME, actual)
        self.assertIn("LouderME Virtual Speaker", actual)
        interfaces = actual.split("[SYSVAD_SA.NT.Interfaces]", 1)[1].split(
            "[SYSVAD_SA.NT.Services]", 1
        )[0]
        self.assertEqual(interfaces.count("AddInterface="), 2)
        self.assertNotIn("WaveMicIn", interfaces)

    def test_extension_is_explicitly_not_for_installation(self):
        sample = (
            r"Root\sysvad_ComponentizedAudioSample" + "\n"
            'ExtendedFriendlyName = "SYSVAD (with APO Extensions)"'
        )
        result = MODULE.patch_inf(sample, True)
        self.assertIn("NOT FOR INSTALL", result)
        self.assertIn(MODULE.HARDWARE_ID, result)

    def test_no_disk_recording_or_demo_generated_tone(self):
        sample = (
            "DWORD g_DoNotCreateDataFiles = 1;\n"
            "DWORD g_DisableToneGenerator = 0;\n"
            '    { NULL, L"DoNotCreateDataFiles", &g_DoNotCreateDataFiles },\n'
        )
        actual = MODULE.patch_adapter(sample)
        self.assertIn("g_DoNotCreateDataFiles = 1", actual)
        self.assertIn("g_DisableToneGenerator = 1", actual)
        self.assertNotIn('L"DoNotCreateDataFiles"', actual)

    def test_mspl_text_is_required(self):
        source = (HERE.parent / "prepare_sysvad.py").read_text()
        self.assertIn("The Microsoft Public License (MS-PL)", source)
        self.assertIn("PINNED_UPSTREAM", source)


if __name__ == "__main__":
    unittest.main()
