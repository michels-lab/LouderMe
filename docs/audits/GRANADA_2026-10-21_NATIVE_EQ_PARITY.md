# Operación Granada — nominal equalizer-band parity (Android, desktop, native DSP)

Date: 2026-10-21. Owning product: LouderMe (Michel's Lab). Scope: experimental native DSP + contract tests; no public distribution.

## Reproduced root cause

Before this change, the experimental portable C++ DSP declared centers **60/170/310/600/1000/3000/12000 Hz**, while LouderMe Android `EqualizerPresets.frequenciesHz` and the current stable Windows Equalizer APO UI `EqualizerPresets.FrequenciesHz` both declare **60/150/400/1000/2500/6000/12000 Hz**. This means the slider/list position applied a peaking filter at a different frequency, e.g. zero-based index 4 was 1 kHz in experimental native DSP but 2.5 kHz to customers. The previous C++ 1-kHz EQ test hardcoded index 4, silently legitimizing the mismatch.

## Linked corrections

1. Set the native C++ `AudioEngine::CenterHz` order equal to the unchanged stable Android and Desktop sources. Native processing still uses preallocated Q=1.2 peaking EQ, five-millisecond fade and bounded limiter. No extra thread, microphone, driver installation, OS default route or app binary modification.
2. Correct native C++ 1-kHz bypass, live-change and rapid-toggle fixture positions from 4 to 3; add seven distinct 48k-Hz, low-level sine-response experiments in C++ tests, one at each advertised center, with numerical gain acceptance. Update native C# 1-kHz interop/test controls and lab command documentation.
3. Add `tools/test_louderme_eq_band_contract.py` which parses **actual source arrays** from Android, stable Desktop and experimental C++, asserts exact seven-band order and verifies that the C# interop proof and C++ frequency-sweep fixture keep their contract. Wire source-guard into Linux native DSP CI; changes to public platform arrays also trigger this native contract job.

## Evidence levels and exclusions

- Static contract: three independent source array literals must match canonical 60/150/400/1000/2500/6000/12000 Hz.
- Synthetic DSP: all seven bands must amplify their own matching PCM tones at +8 dB (bounded numeric acceptance); existing limiter, mono/stereo, click-reduced transitions, live C# interop and channel isolation tests remain enforced.
- Non-equivalence explicitly accepted: Android session-0 device hardware may collapse several requested labels onto fewer physical bands; Equalizer APO's GraphicEQ interpolates; native C++ uses Q=1.2 peaking filters. Matching nominal centers is not matching acoustic frequency-response curves.
- **Not done:** authenticated native driver, installed Windows audio endpoint, live arbitrary-app PCM, calibrated SPL, physical listening, release signing, stable updater or public Windows v0.1.13 changes.

## QA delivery status

Run current-head Linux/Windows native C++ DSP jobs, Windows managed/native interop, Android nonregression and the new source-band mapping guard in CI. Only merge if all finish green at the same exact commit. No stable release/version increment is justified by experimental backend tests alone.
