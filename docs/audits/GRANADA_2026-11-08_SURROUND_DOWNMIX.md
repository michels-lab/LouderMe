# Granada 2026-11-08 — actual Windows surround to stereo rendering

## Functional block removed
Native virtual→physical bridge supported only equal channel counts or mono↔stereo. A Windows app selecting a 5.1/7.1 virtual render mix could not play through commonly available 2-channel physical speakers/headsets. The user-selected route failed preflight although Windows mix layouts were correctly declared.

## Implementation
- The real PcmFormatAdapter now precomputes a speaker-aware 5.1/7.1→stereo matrix once on route creation for WAVEFORMATEXTENSIBLE masks 0x3f (5.1 back), 0x60f (5.1 side) or 0x63f (7.1). It uses bit order of actual speaker positions instead of assuming the same array indices across formats.
- FL/FR go to their own sides; FC, back and side speakers fold into the appropriate stereo side at 0.7071 raw weighting. LFE is not mixed into full-range stereo. Stereo outputs are normalized by the sum of their weights to stay inside normalized float boundaries even when every surround speaker sends +1 simultaneously. This trades absolute level for conservative headroom.
- The existing native C++ DSP processes all source channels before rendering. The live callback uses preallocated coefficient arrays and streaming frame history without per-packet allocation. Resampler and anti-alias paths remain unchanged and can handle 96→48 kHz after downmix.
- Fail closed for nonstandard multichannel speaker masks and stereo endpoints whose explicit positions are not FL+FR. This is not arbitrary N→2 remixing.
- Route preflight is updated. Windows CI's real --check-formats/--check-scratch/--check-route-preflight exercise side/back 5.1, 7.1, 8→2 downsampling with packet boundaries, channel isolation, LFE exclusion, negative layout checks and correlated full-scale amplitude.

## Scope / limits
No signed SysVAD driver is installed by CI; no physical audio device or real third-party app is controlled. This advances the actual native route (#48), but physical VM proof and output/device tests are still required. Not a production loudness-calibrated downmixer; normal product settings have no behavior change. No RC / release.
