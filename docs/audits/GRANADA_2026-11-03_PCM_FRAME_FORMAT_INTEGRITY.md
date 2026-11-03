# Granada 2026-11-03 — frame-safe virtual PCM relay and strict mix-format identity

## Defects identified in the kernel relay introduced by PR #136

The first working WDK-compiled LouderMe WaveRT render→loopback FIFO had a fixed `65536`-byte ring. **65536 is not divisible by six** (stereo 24-bit PCM), so a wrapped FIFO could begin replay on a different byte within a frame. That risks channel/sample corruption despite otherwise successful loopback measurements. The relay also accepted every `WAVE_FORMAT_EXTENSIBLE` subtype, including arbitrary non-audio formats, and allowed source/capture byte counts not aligned to the advertised block size.

A capture request with an incompatible format previously zero-filled only that read, leaving old PCM queued to be replayed if a later capture used the original format. This was a possible false positive for the isolated virtual 440-Hz probe.

## Corrective implementation

- The per-miniport ring now uses **65520 bytes**, divisible by all supported 16/24/32-bit mono/stereo block-alignment sizes (2,3,4,6,8).
- The relay checks every **publish and consume byte count modulo `nBlockAlign`**. Non-frame-length packets clear the FIFO and emit silence rather than shifting channels.
- WaveRT derivation publishes only when `m_ulDmaBufferSize` and measured `ByteDisplacement` are whole sample frames. It otherwise keeps normal driver position updates but refuses to invent relay PCM.
- The relay accepts only PCM with supported 16/24/32-bit formats and IEEE float **32-bit only**, whether using a standard WaveFormat tag or a matching extended GUID subtype. Unrecognized extended GUIDs, float16, malformed `cbSize` and impossible `nAvgBytesPerSec` fail closed.
- Capture format mismatch now discards stale queued samples. A later compatible capture cannot resurrect the earlier data. No implicit resampling/conversion, microphone, disk writes or generic Windows default-device changes.
- The host test executes the *same production C++ header*, testing wrap and overflow with 6-byte frames, float32 and PCM extended GUIDs, unknown tags, unaligned packets, finite zero-filled idle source, stale-format flushing and bounded allocation. The pinned SysVAD derivation tests enforce the new DMA conditions.

## Scope / still blocked

This protects a **source-level, experimental kernel driver**. Even if WDK/x64, INF/CAT and Android CI all pass, it does not prove a trusted installed Windows virtual device receives actual independent-app PCM, that the native C++ DSP sends it to the selected physical sink, or that gain is audible in speakers. Complete the disposable snapshot-backed signed-lab-driver VM A/B chain before any release or closing #46/#48.

No production driver signing, physical installation, acoustic SPL or stable release is performed in this change.
