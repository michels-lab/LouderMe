# Operación Granada — 2026-11-03: canonical PCM/float WaveFormat matching

## Root cause

The source-level SysVAD WaveRT render→loopback FIFO added in PR #136 rejected an output/input pair unless their **WAVEFORMATEX tag** matched exactly. That is unnecessarily strict for a standard PCM stream (`WAVE_FORMAT_PCM`) and its semantically identical `WAVE_FORMAT_EXTENSIBLE` + `KSDATAFORMAT_SUBTYPE_PCM` representation, or legacy IEEE float32 versus extensible IEEE float32. Windows software and endpoint processing can expose either wrapper while preserving the same actual samples. The old `SameFormat` logic would silently drop all queued PCM; a fully built driver could appear to have no loopback audio.

## Fix: byte-identical-only normalization

- `LouderMeLabPcmRelay.h` continues to check channels (1–2), sample rate, container sample width (16/24/32), byte/frame alignment, and `nAvgBytesPerSec`.
- For ext formats only, require canonical `cbSize >= 22`, recognized PCM/float32 subtype GUID and mono/stereo channel layout: mono front-center mask `0x4`, stereo front-left/right `0x3`, or an unspecified zero mask. Other masks are **not remapped**.
- Compare **canonical sample class** (integer PCM or 32-bit IEEE float) and actual **valid sample bits** rather than requiring identical wrapper tags. Legacy formats imply all bits in their container are significant; extensible formats can explicitly carry a smaller valid-bits count. Two 24-valid-bit PCM samples in a 32-bit container may match one another, but cannot be misinterpreted as a full-precision legacy 32-bit sample.
- Extensible IEEE float32 with a partial valid-bits count is rejected. PCM32 and IEEE float32 never match even when both use the same 4/8-byte stereo frame width.
- A mismatched capture continues to zero-fill and reset the FIFO. No automatic resampling, sample conversion, routing changes, microphone access, audio recordings, or relaxation of DRM/stream-identity enforcement.

## Tests

- The production relay C++ header is compiled with test-side kernel stubs. It now checks legacy→extended and extended→legacy PCM16, equivalent float32, incompatible PCM32↔float32, invalid surround masks, valid 24-in-32 PCM versus 32-in-32 PCM, impossible valid bits, partial-bit float32, and all previous FIFO ordering/concurrency tests.
- Python source-boundary tests require canonical sample class and valid-bit comparisons rather than raw `wFormatTag` comparison.
- PR validation must pass exact-head pinned Microsoft SysVAD derivation, host C++ test, x64 WDK compile + INF/CAT and Android installed-APK regressions.

## Evidence boundaries

This eliminates a clear source-level cause for missing virtual loopback PCM when equivalent wrapper formats are negotiated; it **does not** prove that a real installed signed lab driver transfers PCM, produces acoustic sound, or intercepts third-party applications globally. Run the existing consented snapshot-backed Windows VM lab to demonstrate independent source→virtual loopback→native DSP→physical WASAPI A/B with pre/post no-source controls. Keep issues #46/#48 open; do not ship the experimental kernel driver as a release.
