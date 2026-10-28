# GRANADA — Native Windows DSP unity bypass must preserve full-scale PCM

Date: 2026-10-27. LouderMe issues #46/#48. Source-only change, no stable release.

## Reproduced source-level defect

The C++ `AudioEngine::process` applied its soft-knee limiter **unconditionally** to every PCM sample, even when Boost and equalizer were disabled. This compressed a fully valid source sine/waveform above the 0.88 amplitude knee near 0 dBFS; e.g., an otherwise transparent 0.99 sample was reduced on the unprocessed path. It also applied the same unnecessary knee when Boost was explicitly enabled at 100% or an EQ had all seven gains at 0 dB. A native Windows audio route should be transparent at unity when not altering sound.

## Fix — real C++ DSP

- Added a **separate limiter wet-mix state** with the existing 5-ms one-pole parameter smoothing, updated from real Boost-enabled >100% or EQ-enabled/nonflat band states, preserving setter thread constraints. Unchanged PCM at a settled 100% boost/flat EQ bypass passes through unchanged throughout normalized [-1, 1].
- Soft-knee limitation is engaged when meaningful Boost or nonflat EQ processing is requested; its mix ramps rather than jumping instantly when toggling a user control at a waveform crest. Processed values are always bounded to [-1, 1] and nonfinite output is sanitized. For the first few samples of an onset, final hard bounding is still necessary when the intermediate boost has moved faster than limiter fade; this is **not** a true-peak certified limiter.
- EQ filters, crossfade banking, seven product bands, channel separation, 100–250% gain, C ABI and thread-only setter semantics remain unchanged. No per-sample allocation, new thread, persistent audio capture or OS default-device manipulation.
- New C++ DSP regression cases: **bit-exact** full-scale bypass at 44.1/48/96 kHz for stereo peaks including -1/+1, Boost 250% disabled, flat EQ enabled; first-frame limiter engagement when turning on Boost; 250% high-crest safety; true soft-knee reduction on a calibrated 0.4 input; complete settling back to bit-exact 0.99 after Boost/EQ off; nonfinite/out-of-range input bounded; two-channel fragmented vs contiguous frame-for-frame limiter parity. The existing C ABI, EQ frequency response, EQ transition and Windows bridge tests continue as mandatory regressions.

## Limits

This removes unnecessary level compression **only in the experimental native C++ DSP**. It is not a completed consumer Windows app route and does not alter the public Equalizer APO implementation. Audio quality still needs in-ear/physical transducer testing, intersample true-peak limiting, controlled timing and clipping measurements. A newly developed Windows kernel route must be correctly signed, installed and tested inside an owner-authorized isolated VM, and actual independent Windows app sound must reach a real physical speaker before product release.

## Merge gate

Exact-head C++ Windows/Linux tests, bridge ABI/PCM tests, Android JVM/installed Home/About/Developer Edition, and if triggered the WDK lab source qualification all must pass before merge. No public version change, no prerelease, keep #46/#48 open.