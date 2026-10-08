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
- Currently **float32 only**, identical sample rate/channel count for both
  endpoints, 1–8 channels. No resampling or rate adaptation; mismatch fails
  before playback starts.
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

Test real DLL interop **without any audio hardware**:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-native
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

Ctrl+C stops the bridge. The DSP is connected only to the chosen virtual
endpoint capture stream. **Listening to audio on an ordinary physical output
does not prove that the virtual bridge is processing it.**

## Remaining work before any production release

1. Implement/build/test a real SysVAD-derived, licensed, installable and
   appropriately signed LouderME virtual render device (current repo has
   neither binary nor INF).
2. Verify that its loopback stream exposes the PCM actually sent by Windows
   applications; Microsoft SysVAD sample alone does not prove this.
3. Add float32/PCM16 conversion, safe sample-rate adaptation, backpressure,
   stable performance, hotplug/default-device recovery, click-free EQ updates,
   and a thread-safe control channel.
4. Verify no doubled output, echo feedback, loss of physical audio or
   failure to restore previous default routes on restart/uninstall/crash.
5. Add real Windows device tests (Bluetooth, HDMI, Realtek, headphones),
   measure latency/underruns, signing/install/update/uninstall and disabled
   path behavior; user post-release visual review is separate.
6. Only after genuine routing evidence should the UI offer “Native Global
   Boost” without the external Equalizer APO fallback.

See `docs/NATIVE_AUDIO_ENGINE_ARCHITECTURE.md` and Issues #46/#48.
