# Operación Granada — physical WASAPI 100% / 150% native gain A/B (2026-10-30)

## Context and scope

LouderMe's experimental `--probe-direct-dsp` from merged PR #130 can generate a bounded C++-processed signal on a manually selected physical Windows WASAPI output and observe the same endpoint's loopback. A single 440-Hz identity is not evidence that changing Boost makes a real difference. This PR adds an **opt-in two-phase physical-loopback gain comparison** without the unavailable virtual driver.

**Exact claim if a real operator-controlled hardware test passes:** at an explicitly selected physical Windows output, a native C++ DSP 150% stimulus produced ~1.5 times the matched 440-Hz WASAPI-loopback amplitude of the 100% stimulus. This is **NOT** independent-app/global audio interception, acoustic loudness/SPL, isolated-source causality, signed driver deployment, or a consumer-system Boost guarantee.

## Lab usage

First build `louderme_audio_c.dll` and `LouderMeAudioBridge.dll` using the documented Windows bridge build; ensure the native DLL is alongside the executable. List devices, set the output speaker volume low and turn off unrelated media:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-direct-gain --output-id "<EXACT_PHYSICAL_ID>" --confirm-experimental
```

The command runs the existing direct physical monitor twice on the **same selected physical output**, first at Boost 100%, then 150%, with a 500-ms rest; each phase plays a 2-second native DSP-generated 440-Hz signal surrounded by 250-ms quiet padding and records up to a four-second WASAPI loopback measurement. Ctrl+C interrupts without accepting evidence. No default endpoint changes, device install, microphone, disk PCM or network recording occur.

Only two individually valid matched-frequency phase measurements are compared. Their monitor-frame durations must differ by at most 10%, the matched 440-Hz amplitude ratio must fall between 1.25 and 1.75, and identity/coherence must remain valid. Identical output under 100/150, reversed gain, nonfinite ratio, short/missing measurements or different-duration captures all fail closed. Output prints only numerical amplitudes, frame counts, ratio and an evidence code, not device IDs or user audio.

## Implementation and regression

- `native/windows-bridge/DirectPhysicalGainComparison.cs`: pure numeric gain evidence contract + hardware orchestration of the existing physical tone path.
- `DirectPhysicalDspProbe.Run` accepts an optional callback with the **already validated** real physical loopback tone evidence, never executes it for a failed tone. The existing CLI behaves as before.
- `Program.cs`: `--probe-direct-gain --output-id ... --confirm-experimental` explicit isolated command. No EQ toggles or auto-retry for comparable measurements.
- `--check-direct-gain` generates both phase PCM buffers through the **real C++ native DSP**, verifies the synthetic amplitude gain and tests adversarial invalid pairs **without playing sound**. The Windows managed/native workflow executes it.
- No Android UI, desktop stable user controls, release pipeline, versions, or production driver are changed.

## Acceptance blockers

1. The **exact PR HEAD SHA** must build the .NET Windows bridge, run `--check-direct-gain` and the existing native DSP/PCM/queue tests. Source CI is not physical loopback evidence.
2. An operator should run the direct A/B on a real Windows PC and review the evidence and subjective listening at low physical speaker volume.
3. The signed, properly installed virtual endpoint and test VM independent-source → virtual PCM → native DSP → physical WASAPI path, with before/after no-source controls, are still essential before #46/#48 can close.
4. A 100%/150% physical A/B can be confounded by unrelated local audio unless the test device is isolated, and WASAPI loopback measures software PCM, not speakers' acoustic pressure.
5. No stable release is authorized in this block.
