# Operación Granada — 2026-11-03: render DMA → WaveRT loopback PCM (experimental)

## Root cause and feature

The pinned Microsoft SysVAD example (commit `2dc3fd3a0cc84a2933f2194e7ec0871584979071`) uses `ToneGenerator::GenerateSine` in `CMiniportWaveRTStream::WriteBytes` on its capture pin. LouderMe had disabled that demo generator, producing silence. Neither source inspection nor build CI proves that the Windows audio engine will independently forward actual render DMA PCM to that custom capture stream. The existing user-consented VM test is therefore mandatory.

The new **LAB ONLY** code explicitly publishes data from the *consumed* system-render WaveRT DMA buffer and consumes those bytes on the matching loopback capture stream. It does not modify the consumer application, choose Windows default endpoints, inspect process names, record to disk, or claim kernel sound on an installed machine.

## Implementation

- `native/virtual-driver/LouderMeLabPcmRelay.h`: per-miniport **64 KiB nonpaged ring**, guarded by a kernel spin lock. No allocations, disk/network IO or waiting in the capture/render callback. No global buffer shared between audio devices.
- Formats must agree in sample rate, channel count (1–2), bit depth (16/24/32), block alignment, WaveFormat tag and extensible GUID subtype. Unsupported conversions **fail to silence**; conversion/resampling is handled in user-space DSP, not in this kernel prototype.
- Ring overflow discards the oldest unconsumed PCM; underruns zero-fill output (never fake 440 Hz); source/loopback closure resets state and the destructor securely wipes and frees nonpaged storage.
- Render callback publishes only the system render stream's **actually advanced DMA bytes**, with circular-buffer wrap and a strict bounded copy; the loopback's capture `WriteBytes` consumes only these copied bytes, replacing the example's synthetic test generator.
- The forwarding step respects WaveRT's copy-protection and loopback protection checks, refusing restricted content rather than bypassing DRM. No microphones/offload, no multi-render mixing, and no alternative non-LAB driver configuration are introduced.
- `prepare_sysvad.py` copies this header and exact-patches three pinned Microsoft source files, records all output SHA-256 values in the LAB manifest, and rejects changed upstream anchors. All MS-PL license handling remains intact.

## Acceptance

1. Python derivation tests and *real pinned Microsoft source derivation* must pass; WDK must compile the x64 kernel code **without signing or installing it** in hosted CI.
2. With owner's explicit consent, an isolated snapshot-backed Windows VM with a locally trusted lab-signed package and a real physical WASAPI endpoint must complete: pre-driver physical A/B, silent virtual listener before, independent process 440 Hz at virtual loopback, silent listener after, native C++ DSP to physical output at Boost 100/150, before/after physical no-source controls, and removal of **only the LAB PnP instance**.
3. A successful CI build is NOT proof of a working device, installed driver, actual third-party audio, acoustic SPL, system-wide Boost, or production-signature readiness. Do not close #46/#48 or publish a release without the real VM/hardware evidence.

## Engineering limitations still to test

- Shared-mode Windows endpoint format compatibility and WaveRT event timing need actual VM verification. The relay intentionally rejects mismatches instead of guessing a conversion.
- If render/capture clocks drift, the bounded FIFO will emit silence or drop old data rather than hang the kernel. A drop-free capture is therefore **not** assumed.
- Capture of protected streams is explicitly refused; no policy bypass.
- A 64 KiB lab buffer is not a production audio architecture. More advanced jitter/reclocking and provenance work is required before any user installer.

No release, no privileged driver install, no signed driver and no acoustic measurement were performed here.
