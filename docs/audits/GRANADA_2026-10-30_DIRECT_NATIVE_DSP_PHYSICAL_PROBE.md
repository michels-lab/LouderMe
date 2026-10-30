# Operación Granada — native DSP to selected physical WASAPI output (2026-10-30)

**Issue:** #46/#48. **Scope:** experimental Windows bridge code only, not the stable Windows/Android products. This is a functional decomposed hardware diagnostic, not an alternative to finishing system-wide audio routing.

## Why this block is needed

The current virtual→native DSP→physical proof cannot run on a normal Windows PC unless LouderMe's unsigned/experimental virtual render endpoint has already been installed and the VM permits it. Previously the bridge's real physical WASAPI output and loopback could not be exercised separately from that driver. The new **direct** experiment isolates the native C++ DSP plus the actual selected physical playback device. If it fails on an isolated lab PC, the problem is not necessarily inside SysVAD or the virtual loopback; it can be in physical render setup, PCM format, or DSP. If it passes, that still does **not** prove the virtual or third-party routing stages.

## Commands (experimental; opt-in, audible, local only)

Build C++ `louderme_audio_c.dll` and `LouderMeAudioBridge.dll` as documented in `native/windows-bridge/README.md`. No output device is auto-selected or changed.

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-direct-dsp --output-id "<EXACT_PHYSICAL_RENDER_ID>" --boost 100 --confirm-experimental
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-direct-dsp --output-id "<EXACT_PHYSICAL_RENDER_ID>" --boost 150 --confirm-experimental
```

Optional `--eq-db 0,0,0,0,8,0,0` selects native EQ; the seven product frequencies and `[-10,+10]` dB limits remain unchanged. The bounded probe generates 2 seconds of 440 Hz at normalized input amplitude 0.025, with 250 ms silence before and after. Direct experimental Boost is strictly 100–150%, not the product's 250% maximum. **Set a safe speaker level first:** the application never changes the Windows volume. Ctrl+C cancels and refuses success. No system-wide capture, microphone, cloud, disk recording, driver install, privileged permission, or default-device change.

## Implementation

- `native/windows-bridge/DirectPhysicalDspProbe.cs`: construct PCM float32 internally, run the **real** native C++ gain/EQ engine via `NativeDsp`, convert to the selected physical mix via `PcmFormatAdapter`, play through NAudio `WasapiOut`, measure the *same* physical endpoint's `WasapiLoopbackCapture`. A local signal identity check uses `LoopbackToneVerifier`.
- The exact selected sink ID must exist once, be active and not be named LouderMe Virtual. Unsupported mix formats or >2 channels fail closed. No hidden device fallback. The probe starts the monitor before playback and awaits loopback stop before making a claim.
- Exit 0 means **a 440 Hz identity was detected in the physical endpoint's WASAPI loopback**, not that speakers made an audible sound, not that no unrelated sound was present, not proof of 100/150 amplitude ratio, not a signed virtual driver, not interception of Windows apps. Exit 4 means missing tone identity. Other failures abort without evidence.
- `--check-direct-dsp` calls the same PCM preparation code and real native DSP without devices at 44.1/48/96k PCM16/float32 mono/stereo, verifies expected frame counts, matched tone, an in-memory 100/150 amplitude ratio and rejection of unsafe gains, EQ and unsupported channels. Native Windows CI invokes this separately from `--check-route-tone`.

## Acceptance and blockers

1. Source exact-head native Windows workflow must compile the bridge, run `--check-direct-dsp` plus all existing regressions (CI is not hardware).
2. On an explicitly owner-consented Windows lab PC, run the direct physical probe at 100 and 150 with the same output and no other media; record results, monitor identity, errors and device compatibility. No assumption that output device audio is audible from WASAPI loopback alone.
3. Then in a **separate disposable test VM with a safely installed, trusted lab driver**, run the full independent source app → LouderMe virtual render → C++ DSP → selected physical WASAPI route, before/after silence controls and gain comparison. The direct probe cannot bypass missing SysVAD PCM routing.
4. No production virtual driver signing, installer, system-wide functionality, user listening acceptance or new stable release is claimed. Keep #46/#48 open.
