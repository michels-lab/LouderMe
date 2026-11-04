# Operación Granada — 2026-11-03: WaveRT frame-accurate source → loopback clock

## Identified functional issue

In pinned Microsoft SysVAD `EndpointsCommon/minwavertstream.cpp`, `CMiniportWaveRTStream::UpdatePosition` computes `ByteDisplacement` from **whole elapsed milliseconds × nAvgBytesPerSec**, carrying the remainder in thousandths **of a byte**. For example, a 44.1-kHz stereo 24-bit stream is **264600 bytes/sec**. Two successive 1-ms intervals produce byte movements **264 and 265**: the second is NOT divisible by the six-byte sample-frame size.

LouderMe's new render→loopback DMA relay (PRs #136–#138) correctly rejects fractional-frame writes, so the upstream clock could cause recurring lost packets or a displaced stream position even while the miniport builds. This is especially likely at 44.1 kHz; dropping those packets can cause a real virtual PCM proof to fail on an installed VM.

## Fix

- New `LouderMeLabFrameClock.h` computes whole frames from `(bytes/sec / nBlockAlign) * elapsedMilliseconds + fractionalFrameMillis`, dividing by 1000 and retaining a remainder in **1/1000 frame** units. No floating-point, allocation, sleeping or kernel I/O. It uses 64-bit intermediates and limits each callback to **at most 250 milliseconds of whole frames**, so a paused VM cannot demand a multi-second DPC loop after resuming. The cap discards obsolete catch-up time and clears the fractional carry.
- The exact pinned-source patch applies this helper only to SysVAD's **system-render** and **render-loopback capture** pins; unrelated sample capture/offload pins retain their original clock. The same `m_byteDisplacementCarryForward` field stores a fractional frame for those two lab streams only.
- The STOP transition clears the fractional carry along with `m_ullLinearPosition`, preventing a previous audio session from biasing the next one.
- The clock header ships into the throwaway Microsoft WDK checkout as a standalone header, with its SHA256 recorded in the lab manifest. No installer or signed device is emitted by CI.
- Host test `test_pcm_frame_clock_host.cpp` runs the actual helper over 5,000 × 1-ms updates at 8 / 44.1 / 48 / 96 / 192 kHz and supported 2,3,4,6,8-byte frames, and many nonuniform time intervals, verifying every output remains frame aligned and the accumulated frame count exactly tracks elapsed time. Also covers STOP/reset, invalid metadata and 32-bit saturation.
- Python derivation/source-drift tests require header injection, original ByteDisplacement replacement and STOP-reset in the Microsoft source. CI runs both the preexisting PCM FIFO tests and this clock test before attempting the WDK build.

## Acceptance

This is a substantive **audio transport correctness change**, not proof that a device is installed or output sound is audible. Exact-HEAD Python derivation, real helper C++ execution, WDK miniport x64 and INF/CAT validation, and Android installed-APK regression must all pass before merge. An owner-authorized disposable Windows VM with a trusted lab-signed driver and actual independent Windows process source is still needed to demonstrate the virtual loopback, DSP and physical playback. Keep issues #46/#48 open, no release.
