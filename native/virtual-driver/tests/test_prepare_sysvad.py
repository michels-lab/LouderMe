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

    def test_relay_uses_bounded_per_miniport_nonpaged_fifo(self):
        header = (HERE.parent / "LouderMeLabPcmRelay.h").read_text()
        self.assertIn("KSPIN_LOCK m_Lock", header)
        self.assertIn("Capacity = 65520", header)
        self.assertIn("format->Format.cbSize >= 22", header)
        self.assertNotIn("sizeof(WAVEFORMATEXTENSIBLE) -", header)
        self.assertIn("KSDATAFORMAT_SUBTYPE_IEEE_FLOAT", header)
        self.assertIn("KSDATAFORMAT_SUBTYPE_PCM", header)
        self.assertIn("bytes % format->Format.nBlockAlign != 0", header)
        self.assertIn("m_HaveFormat = FALSE", header)
        self.assertIn("nAvgBytesPerSec", header)
        self.assertIn("POOL_FLAG_NON_PAGED", header)
        self.assertIn("KeAcquireSpinLock", header)
        self.assertIn("RtlSecureZeroMemory", header)
        self.assertIn("RtlZeroMemory(output, bytes)", header)
        self.assertIn("m_Count -= take", header)
        self.assertIn("m_Count += bytes", header)
        self.assertIn("SameFormat(format)", header)
        self.assertIn("m_Tag != WAVE_FORMAT_EXTENSIBLE", header)
        self.assertNotIn("CreateFile", header)
        self.assertNotIn("KeWaitFor", header)
        self.assertNotIn("GenerateSine", header)

    def test_relay_kernel_derivation_rejects_upstream_drift(self):
        sample_header = (
            "#define _SYSVAD_MINWAVERT_H_\n"
            "    CONSTRICTOR_OPTION                  m_LoopbackProtection;\n"
            "public:\n    DECLARE_PROPERTYHANDLER(Get_SoundDetectorSupportedPatterns);"
        )
        result = MODULE.patch_minwavert_header(sample_header)
        self.assertIn('#include "LouderMeLabPcmRelay.h"', result)
        self.assertIn("LouderMeLabPcmRelay                 m_LouderMeRelay", result)
        self.assertIn("m_LoopbackProtection != CONSTRICTOR_OPTION_DISABLE", result)
        self.assertIn("m_MixDrmRights.CopyProtect", result)
        self.assertIn("m_ulLoopbackAllocated == 0", result)
        self.assertIn("m_LouderMeRelay.Reset();", result)
        self.assertEqual(result.count("m_LouderMeRelay.Reset();"), 2)
        self.assertIn("m_LouderMeRelay.Publish(pcm, bytes, format);", result)
        self.assertIn("m_LouderMeRelay.Consume(pcm, bytes, format);", result)
        self.assertIn("m_SystemStreams[0] != stream", result)
        self.assertIn("RtlZeroMemory(pcm, bytes)", result)
        with self.assertRaises(ValueError):
            MODULE.patch_minwavert_header(sample_header.replace(
                "m_LoopbackProtection;", "m_InvalidLoopbackProtection;"
            ))

    def test_kernel_stream_publishes_only_consumed_render_and_reads_loopback(self):
        before = (
            "NTSTATUS CMiniportWaveRTStream::Init\n{\n"
            "            ntStatus = m_ToneGenerator.Init(toneFrequency, toneAmplitudeDouble, toneDCOffsetDouble, toneInitialPhaseDouble, m_pWfExt);\n"
            "}\n"
            "VOID CMiniportWaveRTStream::UpdatePosition\n{\n"
            "        if (!g_DoNotCreateDataFiles)\n"
            "}\n"
            "VOID CMiniportWaveRTStream::WriteBytes\n{\n"
            "            m_ToneGenerator.GenerateSine(m_pDmaBuffer + bufferOffset, runWrite);"
            "}\n"
        )
        actual = MODULE.patch_minwavertstream_cpp(before)
        self.assertIn("PublishLouderMeRender(", actual)
        self.assertIn("FillLouderMeLoopback(", actual)
        self.assertIn("if (!m_pMiniport->IsLoopbackPin(Pin_))", actual)
        self.assertIn("ntStatus = m_ToneGenerator.Init(", actual)
        self.assertEqual(actual.count("ntStatus = m_ToneGenerator.Init("), 1)
        self.assertLess(actual.index("if (!m_pMiniport->IsLoopbackPin(Pin_))"),
                        actual.index("ntStatus = m_ToneGenerator.Init("))
        self.assertIn("m_pMiniport->IsSystemRenderPin(m_ulPin)", actual)
        self.assertIn("m_pMiniport->IsLoopbackPin(m_ulPin)", actual)
        self.assertIn("ByteDisplacement - remaining", actual)
        self.assertIn("m_ulDmaBufferSize != 0", actual)
        self.assertIn("m_ulDmaBufferSize % m_pWfExt->Format.nBlockAlign == 0", actual)
        self.assertIn("m_ullLinearPosition % m_pWfExt->Format.nBlockAlign == 0", actual)
        self.assertIn("ByteDisplacement % m_pWfExt->Format.nBlockAlign == 0", actual)
        with self.assertRaises(ValueError):
            MODULE.patch_minwavertstream_cpp(before.replace(
                "m_ToneGenerator.GenerateSine", "m_UnknownGenerator"
            ))
        with self.assertRaises(ValueError):
            MODULE.patch_minwavertstream_cpp(before.replace(
                "m_ToneGenerator.Init", "m_UnknownCaptureInit"
            ))

    def test_relay_is_lifetime_managed_and_not_global(self):
        before = (
            "CMiniportWaveRT::~CMiniportWaveRT\n{\n"
            "    if (m_LoopbackStreams)\n{}\n"
            "CMiniportWaveRT::Init\n{\n"
            "    m_LoopbackProtection                = CONSTRICTOR_OPTION_DISABLE;\n"
            "}\n"
            "CMiniportWaveRT::StreamClosed\n{\n"
            "        FREE_PIN_INSTANCE_RESOURCES(m_ulLoopbackAllocated);\n"
            "    else if (IsSystemRenderPin(_Pin))\n"
            "    {\n"
            "        FREE_PIN_INSTANCE_RESOURCES(m_ulSystemAllocated);\n"
            "}\n//=============================================================================\n"
        )
        result = MODULE.patch_minwavert_cpp(before)
        self.assertIn("m_LouderMeRelay.Initialize()", result)
        self.assertIn("m_LouderMeRelay.Shutdown()", result)
        self.assertEqual(result.count("m_LouderMeRelay.Reset()"), 2)
        self.assertLess(result.index("m_LouderMeRelay.Shutdown()"),
                        result.index("CMiniportWaveRT::Init"))
        with self.assertRaises(ValueError):
            MODULE.patch_minwavert_cpp(before.replace(
                "FREE_PIN_INSTANCE_RESOURCES(m_ulLoopbackAllocated)",
                "FREE_PIN_INSTANCE_RESOURCES(m_ulOffloadAllocated)"
            ))

    def test_target_platform_is_explicitly_desktop(self):
        sample = "<DriverTargetPlatform>Universal</DriverTargetPlatform>" * 4
        updated = MODULE.patch_project_for_desktop(sample)
        self.assertEqual(updated.count("<DriverTargetPlatform>Desktop</DriverTargetPlatform>"), 4)
        self.assertNotIn("Universal</DriverTargetPlatform>", updated)
        with self.assertRaises(ValueError):
            MODULE.patch_project_for_desktop(sample[:-1])

    def test_reference_inf_removed_unshipped_demo_components(self):
        # Keep the structural contract covered even without WDK tooling.
        src = (HERE.parent / "prepare_sysvad.py").read_text()
        self.assertIn("prune_reference_inf", src)
        self.assertIn('"package_file_references": ["tabletaudiosample.sys"]', src)
        self.assertIn("keyworddetectorcontosoadapter.dll", src.lower())
        self.assertIn("required_interface_count = 5", src)

    def test_mspl_text_is_required(self):
        source = (HERE.parent / "prepare_sysvad.py").read_text()
        self.assertIn("The Microsoft Public License (MS-PL)", source)
        self.assertIn("PINNED_UPSTREAM", source)


if __name__ == "__main__":
    unittest.main()
