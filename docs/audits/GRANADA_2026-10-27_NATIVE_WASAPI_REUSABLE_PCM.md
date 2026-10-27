# GRANADA — Native Windows WASAPI bridge reusable PCM scratch

Date: 2026-10-27
Scope: issue #46 (native owned DSP) and #48 (virtual render-to-physical bridge). SOURCE-ONLY experiment; **no public release**.

## Baseline found

The core C++ DSP itself already does in-place interleaved float processing with no heap allocations in process(). However, the **actual experimental Windows bridge capture callback** allocated a managed byte[] on every Decode and Encode, and Encode sometimes resized/copied an over-provisioned array. That creates GC pressure under continuous WASAPI capture and could lead to callback jitter despite a fast C++ engine. The bridge already has bounded overflow, recovery and clock ppm feedback, but those controls don't prevent GC stalls.

## Implemented across actual callback

1. **Preallocated session buffers:** after checking the actual selected virtual and physical PCM formats, the native bridge allocates an input float scratch and output render scratch **once**, outside WASAPI callback. Capacity is derived from each device's sample rate, format/channel alignment, maximum 500 ms PCM packet and ±250 ppm clock adjustment with safe interpolation headroom. Real callback rejects unexpectedly longer-than-500-ms packets rather than allocating; this is experimental fail closed, not a promise to handle arbitrary OS callback sizes. No routing/default-device/recording behavior changed.
2. **Allocation-free PCM conversions:** `PcmFormatAdapter.DecodeInto` writes validated PCM16/float32 into the reusable decoder; `EncodeInto` writes resampled/channel-mapped/limiter-clamped PCM into the reusable physical render output without `new byte[]`, resize or copy. Streaming phase history and FIR coefficients persist across packets as before. Anti-alias FIR now has a `Span<byte>` overload and retains channel history. The existing allocating Decode/Encode wrappers are still available for standalone offline tests and other callers. Output scratch too small is rejected BEFORE mutating resampler state; no partial silent replacement of live output. All samples checked finite, all emitted samples frame-aligned.
3. **Tested equivalence and callback budget:** `--check-scratch` runs six format matrices (PCM16/float32, 44.1/48/96 kHz, mono/stereo, matching eight-channel), each with 60 uneven-size streaming packet comparisons against the original convenience output. Tests reject short decode/output scratch and measure managed GC allocation across 256 warmed 10-ms packets. A dedicated Windows native CI step fails on any divergence/excess allocation. The existing `--check-formats`, native C++ CI and real WinForms/Android stable workflows remain untouched.

## Safety / true claims

This reduces managed PCM *array churn* on the experimental WASAPI capture callback. It **does not** establish that every NAudio callback path is allocation-free (NAudio and event dispatch still run separately), that native kernels meet hard real-time timing, that a virtual render driver is signed/installed, that third-party Windows audio has ever flowed through a driver in a physical VM, or that speakers played any sound. Audio quality is still a linear streaming rate adapter with a windowed-sinc FIR, **not** a production-grade true-peak limiter or premium resampler. Windows Equalizer APO remains the shipped fallback; no stable user app has a native globally routed driver.

## Required gates

Merge only after exact-head Microsoft Windows .NET bridge + C++ DSP + scratch GC/parity + existing virtual-driver source QA all pass and no native Linux regressions. No kernel install, no microphone, no elevated permission, no Windows audio policy or device change. Owner review remains post-release and no release was authorized. Remaining #48 physical signed VM proof and #46 true-peak/routing are explicitly OPEN.
