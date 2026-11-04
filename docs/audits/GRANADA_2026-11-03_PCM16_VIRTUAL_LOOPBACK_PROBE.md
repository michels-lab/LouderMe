# Operación Granada — 2026-11-03: virtual-only PCM16/IEEE-float32 loopback evidence

## Functional gap

The virtual-only Windows bridge helper `--probe-virtual-external` (and its same-process `--probe-virtual` counterpart) previously checked `IsFloat32(capture.WaveFormat)` and threw before opening capture if the exact LouderMe virtual render endpoint exposed a legitimate **PCM16** shared-mode format. Its owned-tone generator also unconditionally wrote 32-bit IEEE float sample bytes into whatever `WaveFormat` it received.

This did not affect the native C++ DSP route's decoder, which already accepted PCM16 and float32 via `PcmFormatAdapter`, but it created a **false negative for the separate Windows source → virtual endpoint PCM acceptance test**. A correctly installed lab device using PCM16 could never clear the virtual-only proof gate and therefore was prohibited from reaching subsequent physical-route checks.

## Changes

- Add `VirtualLoopbackProbePcm.cs`: receives the exact selected WASAPI mix and validates only supported mono-to-eight-channel PCM16 or IEEE float32. A `PcmFormatAdapter` decodes callback bytes into a **preallocated** float32 buffer for the existing, phase-insensitive 440-Hz `LoopbackToneVerifier`. Oversized/misaligned/nonfinite packets fail closed, with no ad hoc audio allocations in the callback.
- `Program.ProbeVirtualEndpoint` now uses the helper. External-source mode still creates **no tone, no WasapiOut, no native DSP, no physical sink and no default-device change**; it only decodes real loopback packets from the explicitly selected virtual endpoint and prints unchanged numeric evidence markers.
- The owned-tone virtual-only mode writes sample data matching its actual mix: little-endian PCM16 or IEEE float32 instead of unconditional 32-bit writes.
- `--check-virtual-formats` is a hardware-free selftest: 440-Hz owned input vectors across 44.1/48/96 kHz, one/two channels, PCM16/float32; irregular packet sizes; no-source silence; rejection of NaN float, malformed PCM frame, overlarge callback and unsupported 24-bit PCM (until the managed adapter implements it). The existing verifier still rejects unrelated frequencies.
- Windows native managed/interop CI builds the new helper and executes `--check-virtual-formats`. Android regression unchanged.

## Acceptance boundary

A green Windows CI demonstrates that source encoding and capture decoding work **in memory**, not that a signed virtual SysVAD driver is installed or that a separate Windows application reaches a physical output. The existing owner-consented snapshot-backed Windows VM lab still requires before/after virtual silence, positive independent source PCM, native DSP physical A/B, endpoint identity consistency and PnP cleanup. No consumer release; #46/#48 stay open.
