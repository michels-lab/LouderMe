# GRANADA — WASAPI queue-versus-render provenance (2026-10-27)

**Scope:** Windows disposable virtual-audio LAB only; no consumer release and no installed/test-signed driver.

## Root cause (confirmed source bug)

The external app route printed `physicalEmitted=` directly from `outputFrames`. That variable is incremented at `BufferedWaveProvider.AddSamples`, **before** any `WasapiOut` callback reads the queue. Therefore it measured *enqueued* PCM, not consumed render PCM. Because NAudio `ReadFully=true` zero-fills empty reads, counting `Read` return bytes without accounting for the queue would create a second phantom-delivery proof.

The previous positive and 100/150 validators checked that mislabeled queued counter was large, so a starving physical render path could pass the frame-provenance portion of acceptance (the separate 440 Hz physical loopback amplitude/coherence still had to pass). This is an evidence-integrity bug, not proof of a driver failure.

## Implementation: three linked functional deliverables

**A. Real render-consumption telemetry:** `MonitoredRenderBuffer` counts a conservative lower bound of real queued PCM **provided to WASAPI render callbacks**: `min(BufferedBytes before read, bytes reported by the read)`. Empty and padded reads contribute zero. Both `Read(byte[],...)` and `Read(Span<byte>)` are covered by device-free tests, including zero, full, partial, and repeated starvation. `PayloadFramesRead` is atomic and frame-aligned. This proves a callback consumed queued data, **not** that speakers emitted acoustic sound; a separate actual WASAPI physical loopback analyzer supplies the tonal measurement.

**B. Honest bridge proof and abort:** distinct `physicalQueued`, `renderConsumed`, and `starvedRenderReads` accompany real virtual source and physical monitor PCM. Bounded physical LAB acceptance requires at least one output-format second of render-consumed PCM and never permits consuming more than was enqueued. This prevents the old queue-only `physicalEmitted` counter from masquerading as physical delivery. A genuine coherent 440 Hz WASAPI physical loopback remains mandatory; no acoustic SPL claim.

**C. End-to-end provenance and contaminated-controls rejection:** fail-closed `PositiveSourceCheck.ps1`, `NoSourceControlCheck.ps1` and `IndependentGainCheck.ps1` now require the new fields with complete numeric evidence. Negative *no independent source* controls require **zero** virtual PCM, enqueued PCM, and render-consumed PCM. Both 100% and 150% files require numeric render-consumed frames and reject the legacy field. Session JSON stores queued, consumed and starvation independently. Additional adversarial fixtures reject swapped/missing/oversupplied callback data and background audio. Python static contract prevents reviving the bad label.

## Verification gates and honest limits

1. Exact-head Windows native bridge CI: `--check-recovery` must pass the buffered-render-counter scenarios; PowerShell proof fixtures for no-source, independent source and 100/150 must all pass. This is **device-free**.
2. Native C++ Linux/Windows tests and WDK/virtual-driver source checks must pass without touching a device.
3. Android installed UI/Developer nonregression must pass or any flaky emulator failure must be investigated/retried on unchanged SHA.
4. Merge only after all applicable CI has finished. No new stable Windows or Android release: no consumer binary/source version bump.
5. **Still blocked by physical hardware:** Microsoft-trusted signing/driver install in a disposable VM, real virtual PCM, physical loopback/capture, timing/per-device hotplug, measured gain and user inspection. A real listener callback is not an acoustic SPL measurement.

**Previous source accepted:** `physicalEmitted = queuedPhysicalFrames` (invalid interpretation).
**New claim:** `renderConsumed <= physicalQueued` plus independently measured physical WASAPI 440 Hz identity. This is stronger but not a universal third-party playback proof.

Issues: #46 native DSP/global integration; #48 signed virtual endpoint and actual Windows route. Release Windows 0.1.17 + Android 0.1.12 remains unchanged.
