# Michel's Lab Audio Engine — native DSP and platform routing

Status: **Milestone 1, portable DSP only. Windows global routing not implemented.**
Decision date: 2026-10-08. Owner: LouderME / Michel's Lab.

## Product decision

LouderME's distinctive product capability must be a **Michel's Lab-owned audio processing engine**. External Equalizer APO installation must not be necessary for the **future native engine**. Existing production Windows releases still rely on Equalizer APO and must say so; do not silently suggest the new experimental DSP already affects Windows system audio.

## Separated responsibilities

1. **DSP kernel (native/audio-engine)**: portable C++17 normalized float PCM, seven peaking biquads, smoothed gain 100–250%, and full-scale bounded soft limiting. Pure processing; no audio input/output, device selection, driver or permissions.
2. **Platform integration**: obtains the lawful source stream, handles formats and clock/latency, routes samples through the DSP, delivers result exactly once, handles device changes, recoverability and bypass. *Required for any audible product effect.*
3. **Control plane**: UI process + settings service communicates with the audio backend safely; versioned parameters and a thread-safe message queue will be needed before use in a low-latency callback. User configuration remains local.
4. **Installer/update**: platform-specific privileges, driver components and rollback. Never secretly replace Windows default device, register kernel drivers, or enable capture/recording.
5. **Measurements**: latency, CPU, buffer underruns, continuity across device changes, full-scale peaks, EQ response and bit-exact disabled/bypass path where feasible.

## Windows native system-wide integration decision

Microsoft supports OEM/third-party **custom user-mode Audio Processing Objects (APO)** integrated with the Windows audio engine. For Windows 11 21H2+, componentized APO packages require `Class=AudioProcessingObject`; Windows 10 uses `Class=SoftwareComponent`. Per Microsoft implementation docs, componentized APO registration must be tied to a PnP audio-device component; it **cannot simply be globally registered for arbitrary unrelated audio drivers**. Installation/signing and endpoint compatibility therefore need a real feasibility prototype on a Windows test system.

Candidate routes (not interchangeable):
- **A. Custom Windows APO extension** attached to supported endpoint/driver combinations. Lowest conceptual latency and native Windows effect route when supported, but device-bound registration/packaging means universal coverage is not automatic.
- **B. Michel's Lab signed virtual audio endpoint** (research Microsoft's SysVAD sample): direct render stream to our endpoint, apply the same DSP, output to the user's chosen physical endpoint through a reliable bridge. More explicit cross-app routing, but the app must manage default output selection, driver packaging/signing, unplug/device changes, exclusive-mode streams, format/latency and feedback/loop prevention. Administrative install and Windows hardware signing may be required.
- **Do not call WASAPI loopback capture a global processing solution by itself**: capturing the post-mix output does not insert processed samples back into other apps' original output path; naïve capture-and-replay introduces double audio/feedback and delay.

**Next decision gate:** prototype a custom APO with real PnP deployment on a supported device, compare with SysVAD/virtual endpoint feasibility, measure tested vs untested coverage, and choose based on observable installed behavior. The user-visible label “Native Global Boost” may only show Active when processed OS output is confirmed. Keep fallback explicit, not disguised.

Windows audio in WASAPI exclusive, DRM-protected, offload and driver-specific paths may bypass third-party system effects; do not promise universal effects.

Official references:
- https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/implementing-audio-processing-objects
- https://learn.microsoft.com/en-us/windows-hardware/drivers/dashboard/deploying-audio-processing-objects
- https://learn.microsoft.com/en-us/samples/microsoft/windows-driver-samples/sysvad-virtual-audio-device-driver-sample/

## Android native integration and platform limit

The current Android app can control **actual media device volume** with AudioManager; that is not custom DSP on all other apps' PCM.

Android `AudioEffect` targets the specific `AudioTrack`/`MediaPlayer` audio session. Session-0 global-mix effect attachment is deprecated, and a regular application cannot unconditionally intercept and modify every third-party app's audio. `AudioPlaybackCapture` requires permission through `MediaProjection` and may only capture audio that the source app allows. Treat user capture consent, DRM/content policy and app opt-out as hard constraints; do not market “works with every app” or silently record audio.

Native DSP can serve streams LouderME itself legitimately owns, or opt-in permitted sources with a clearly stated limited scope. A universal Android system-effect route would require privileged/OEM integration outside normal Play-app capabilities.

Official references:
- https://developer.android.com/reference/android/media/audiofx/AudioEffect
- https://developer.android.com/media/platform/av-capture

## DSP core v1: exact scope

Implementation: `native/audio-engine/include/louderme/AudioEngine.hpp` and `src/AudioEngine.cpp`.

- Mono to eight-channel interleaved float32 PCM; 8–384 kHz; preallocated filter state.
- Gain 100–250%, one scalar across channels with 5 ms exponential smoothing; gain disabled = unity.
- Seven equalizer peaking bands (60/170/310/600/1000/3000/12000 Hz; Q=1.2; gains clamped to ±10 dB). When disabled, bypass. Band changes currently clear state (not pop-free at runtime).
- Soft limiting begins at magnitude 0.88 and stays within ±1.0 PCM to mitigate hard digital clipping. This is **not** true-peak / lookahead limiting and can introduce distortion on high-level input; it does not guarantee hearing safety or prevent clipping after hardware gain.
- `process()` does not allocate memory and accepts frames from a host; setters **are not safe for concurrent use** with the audio thread. Add lock-free state transfer before connecting a real device backend.
- All PCM remains local; core does not open devices, access networking, record audio or modify OS volume.

Tests: portable synthetic PCM checks for unity, boost, EQ frequency response, channel isolation, invalid inputs and output range. GitHub Actions builds/tests on Windows and Linux.

## Milestones remaining

1. M1: pass native DSP CI (Windows/Linux) + numeric/audio test vectors and real-time CPU profiling. **DSP implementation, not a system-wide replacement.**
2. M2: implement/control native Windows driver/endpoint adapter with verified real audio and audible 100–250% boost + EQ; device-switch, uninstaller, rollback, 48/44.1/96k, Bluetooth/headphones and non-admin runtime tests.
3. M3: integrate with Windows UI and updater, retire required user-installed Equalizer APO only when the verified native adapter works; preserve legacy fallback until then. Ship with required signing and explicit user consent.
4. M4: Android session-owned native processing and truthful availability state; evaluate opt-in restricted capture where allowed without falsely promising global.
5. M5: end-to-end latency/quality evidence, packaged user testing and explicit versioned release. Michel's visual review occurs after publication, not as a pre-release manual approval gate.

No new version is authorized by this architecture document.
