# LouderME Windows virtual-to-physical audio bridge — experimental M2

This **is NOT a Windows audio driver**. This host application connects an
explicitly selected, already installed LouderME virtual *render* endpoint to
an independently selected real playback endpoint using WASAPI shared-mode
loopback/capture and a Michel's Lab-owned C++ DSP DLL. It does **not** install
or create the virtual endpoint; that WDK/SysVAD work is a separate milestone.

## Status

- Versioned C ABI (`native/audio-engine/include/louderme/AudioEngineC.h`) for
  gain, seven-band EQ and in-place float32 PCM processing.
- A guarded .NET 10 WASAPI bridge that does not touch the system's default
  device, does not modify audio settings, and refuses capture from ordinary
  physical output devices to avoid feedback/double playback.
- Requires active virtual endpoint named `LouderMe Virtual` and a distinct
  physical render endpoint. If there is no working signed virtual device,
  the bridge cannot process any actual system audio.
- Experimental streaming converter now accepts PCM16 or IEEE float32 on
  **either** endpoint, with independently selected mix sample rates of
  8–192 kHz and ratios between 1:4 and 4:1. Native C++ DSP continues to
  operate on private interleaved float32 samples; the physical WASAPI buffer
  receives its own negotiated PCM16/float32 encoding and sample rate.
- Uses **stateful linear interpolation** across capture callbacks, bounded
  buffering and fail-closed callback errors. This is a functional prototype,
  **not** production-quality anti-aliased resampling or a measured latency
  guarantee. Source/physical channel counts must still match (1–8).
- No microphone access, no disk recording or networking. Console reports
  processed/dropped frames, not PCM content.
- This is a proof of managed/native interop, not safe for regular unattended
  daily use; the physical bridge still needs real hardware evaluation,
  underrun/latency/format tests and driver lifecycle/rollback support.

## Commands (Windows test machine)

Build native DLL in Release mode:

```powershell
cmake -S native/audio-engine -B native/audio-engine/build -DCMAKE_BUILD_TYPE=Release
cmake --build native/audio-engine/build --config Release --target louderme_audio_c
dotnet build native/windows-bridge/LouderMeAudioBridge.csproj -c Release
```

Copy the compiled `louderme_audio_c.dll` beside the bridge executable in
`native/windows-bridge/bin/Release/net10.0-windows`.

Test the real C++ DLL interop, PCM format conversion and a synthetic
PCM16 44.1 kHz → native 150% DSP → float32 48 kHz end-to-end pipeline
**without any audio hardware**:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-native
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-formats
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-pipeline
```

When an independently **installed, signed and verified** LouderME virtual
endpoint becomes available, first list playback endpoints:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
```

Then opt in to the experimental physical route; substitute the actual ID
strings returned above. This prototype does not make the virtual device the
Windows default; doing so remains an explicit manual test-lab operation.

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --source-id "<VIRTUAL_RENDER_ID>" --output-id "<PHYSICAL_RENDER_ID>" --boost 150 --confirm-experimental
```

Ctrl+C stops the bridge. Optional experimental seven-band EQ gains are available at startup through
`--eq-db 0,0,0,0,8,0,0` (comma-separated dB for 60/170/310/600/1000/3000/12000 Hz; -10 to +10). They are applied before capture begins; live UI updates are not yet implemented.

The DSP is connected only to the chosen virtual
endpoint capture stream. **Listening to audio on an ordinary physical output
does not prove that the virtual bridge is processing it.**

## Remaining work before any production release

1. Complete **production-signable, installable and audited** SysVAD-derived
   LouderME virtual render driver (the repo has WDK-validated unsigned lab
   INF/CAT/SYS generation only; no production signed or installed driver).
2. Verify that its loopback stream exposes the PCM actually sent by Windows
   applications; Microsoft SysVAD sample alone does not prove this.
3. **PCM16/float32 conversion and basic streaming sample-rate adaptation are
   implemented but require hardware proof.** Add production-quality anti-alias
   resampling, channel-layout negotiation, measured latency and underrun
   handling, hotplug/default-device recovery, click-free EQ updates and a
   thread-safe control channel.
4. Verify no doubled output, echo feedback, loss of physical audio or
   failure to restore previous default routes on restart/uninstall/crash.
5. Add real Windows device tests (Bluetooth, HDMI, Realtek, headphones),
   measure latency/underruns, signing/install/update/uninstall and disabled
   path behavior; user post-release visual review is separate.
6. Only after genuine routing evidence should the UI offer “Native Global
   Boost” without the external Equalizer APO fallback.

See `docs/NATIVE_AUDIO_ENGINE_ARCHITECTURE.md` and Issues #46/#48.

## Lab virtual endpoint PCM proof (no physical output)

When a signed driver has been installed in a **dedicated Windows test VM**,
run `--list-devices` to obtain its ID, then:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-virtual --source-id "<VIRTUAL_ID>" --confirm-experimental
```

This opt-in probe sends a quiet 440 Hz tone to the specified *virtual* output
and uses WASAPI loopback of that same endpoint to calculate RMS/peak; it
fails if PCM is silent or missing. It does not set any system default or
send the tone to a physical speaker. A positive result alone is not a
system-wide processed-sound test. Driver signing, safe installation and
physical output measurements remain pending.
