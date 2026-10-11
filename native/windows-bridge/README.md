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
- Experimental output queue feedback now uses a **bounded ±250 ppm** slowly smoothed correction to the PCM adapter's frame step to compensate modest capture/render clock mismatch without altering Windows device clocks. This is not hardware clock synchronization.
- The bridge reports **queued audio milliseconds (not audible end-to-end latency)**, packet-processing work relative to nominal audio time (not OS CPU utilization), worst callback and over-budget callback counts in 1-second summaries. Independent callback counters use atomic updates. The same-rate/no-correction output fast path avoids a redundant per-packet array resize/copy, reducing managed allocation overhead.
- Uses **stateful linear interpolation** across capture callbacks and
  an additional causal, stateful **windowed-sinc FIR anti-aliasing filter**
  for downsampling only. Filter length is selected at construction
  (47/63/127 taps depending on the rate ratio), with a conservative
  cutoff below output Nyquist, coefficients normalized to unity DC gain
  and independent channel history across callbacks. Coefficients/history
  allocate once; in-place filtering adds no separate packet allocation.
  This is a measured **synthetic anti-alias improvement**, not
  production-certified transparent resampling: the causal filter adds
  group delay, passband/stopband and CPU/latency need hardware evaluation.
  Equal source/physical channel counts (1–8) are supported;
  explicit **mono → stereo duplication** and **stereo → mono averaging**
  are also supported. All other channel-count changes still fail closed.
  Packet-boundary interpolation now waits for the next capture frame rather
  than throwing when a short packet ends between two interpolated samples.
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
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-loopback
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-route-tone
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --check-timing
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
   resampling beyond the newly added basic windowed-sinc anti-alias FIR,
   full multichannel speaker-layout negotiation beyond mono/stereo,
   measured end-to-end speaker latency/CPU on hardware, true underflow
   handling and jitter/clock drift beyond ±250 ppm,
   hotplug/default-device recovery, click-free EQ updates and a thread-safe
   control channel.
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
and uses WASAPI loopback of that same endpoint to calculate RMS, peak,
440 Hz amplitude and phase-independent signal coherence. It rejects
nonzero **unrelated audio**, 880 Hz, DC offset, silence, malformed PCM and
short captures instead of treating any noise as proof.

The evidence is limited to a controlled lab virtual render endpoint and
the specific generated tone. It does not set the default playback device,
does not send the tone to physical speakers, and is **not** a production
driver install/certification, app-origin sound capture proof or global
processed-sound test. Driver signing, safe installation, physical routing,
latency, crash/rollback and device testing remain pending.

## NEW: Isolated virtual → physical loopback route proof (not yet run on hardware)

The CI-only `--check-route-tone` builds and decodes a quiet, known 440 Hz
signal at PCM16/float32 mono/stereo 44.1, 48 and 96 kHz. Its PASS result
**does not mean a real speaker played anything**.

To test real Windows routing, use a **disposable Windows lab VM or dedicated
audio workstation that can be restored from a snapshot**, and only after the
virtual render driver is installed with a legally valid **lab/test** signing
path. Hosted CI cannot provide a physical speaker, trustworthy app-origin
PCM, kernel install/reboot test or an acoustic measurement.

First run `--probe-virtual` successfully on the installed device. Then list
actual endpoint IDs and verify the physical endpoint is separate, audio is
connected, Windows system playback volume is set low, and *all other media
is paused*:

```powershell
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-route --source-id "<LOUDEME_VIRTUAL_ID>" --output-id "<PHYSICAL_SPEAKER_ID>" --boost 150 --confirm-experimental
```

The probe **does not install drivers or switch Windows defaults**. It
explicitly selects two active render endpoints: the first **must** be a
LouderME-named virtual endpoint; the second **must not** be one. It starts
the existing native DSP bridge, emits a low-level 2-second stimulus *only
into the virtual device*, and measures 4 seconds of the independently chosen
physical endpoint's WASAPI loopback. It requires correctly aligned, finite
PCM, a matching 440 Hz peak/coherence and at least one second of actual
virtual-capture processing. A signal from a different frequency, silence
or insufficient virtual capture **fails closed** (exit code 4).

The test currently supports physical and virtual mono/stereo PCM16/float32
mix formats and **boost 100–150%** (higher output is intentionally disallowed
for this test). A PASS is stronger than synthetic DSP CI: it shows that
the 440 Hz virtual stimulus reached the *physical playback stream through
the bridge in this isolated environment*. It is **not** a microphone
recording, calibrated speaker SPL measurement, third-party application
audio proof, absence-of-echo proof or evidence of 150% output gain over
100%. Repeat a controlled 100 vs 150 comparison only after safety/volume
checks; dedicated output amplitude/latency analysis remains pending.

**If there is no real Windows audio endpoint, no successfully installed
virtual driver, or no non-silent loopback:** the probe fails. Do not
simulate a PASS or add a generic nonzero workaround. Capture console
metrics and real hardware details in the lab issue; restore the VM snapshot
after testing. Never distribute the unsigned lab driver, and leave
production Windows v0.1.11/Equalizer APO untouched.

## Disposable Windows VM automated test wrapper

See `native/virtual-driver/run_disposable_lab.ps1` for strict, opt-in
PowerShell orchestration of a *separately signed* lab-only SYS/INF/CAT
device installation, `--probe-virtual`, `--probe-route` and root-device
cleanup. It refuses non-VM hosts, unauthenticated catalogs, missing bridge
DLLs and unselected physical sinks. This is **not** run by hosted GitHub CI;
a dedicated disposable VM snapshot and real audio endpoints are prerequisites.
The helper never turns on test signing or disables Secure Boot, and it does
not restore Windows default output selection automatically. Recover from
the VM snapshot when necessary. CI only syntax-checks the script.
