# Granada 2026-10-26 — independent Windows app positive audio evidence

## Product and starting point
LouderMe only. Main baseline `1bcb533c661accc9b04823e00b5328963ad47b61`; separate UI PR #109 owns Windows About/header assets and must not be touched. Native routing blockers #46/#48 remain open.

## Root cause
`native/virtual-driver/probe_independent_app_route.ps1` previously accepted a successful bridge exit, a generic PASS phrase, and a loosely matched amplitude/coherence substring. It did not enforce that the independent source itself completed its full PCM burst; that exactly one post-readiness real physical monitor record existed; or that the record contained sufficient virtual/physical frames and zero dropped samples. The final 100-to-150% ratio validator likewise did not inspect the PCM frame provenance. An incomplete or copied success message could therefore look convincing even without a trustworthy real signal path.

## Delivered, source-only
1. **PositiveSourceCheck.ps1**: new pure, strict stdout/source-process parser. Requires exactly one bridge readiness, one independent 440 Hz source completion, one scoped bridge PASS, and one full physical WASAPI loopback record. Validates real numeric RMS/peak/440 Hz/coherence, minimum frame counts, source PCM frames/channels and zero dropped frames. Rejects explicit failures even alongside PASS.
2. **Probe integration**: run validator before generating a positive JSON artifact; persist validated physical/source PCM counters in session-bound evidence. No driver install behavior, default endpoint, credentials, sounds, UI or stable Android/Windows binaries changed.
3. **End-of-run ratio gate**: `IndependentGainCheck.ps1` now rejects each 100% or 150% manifest without valid physical monitor frames, virtual/physical PCM, independent source frames/channels, and no dropped frames — not only a plausible amplitude ratio.
4. **Regression CI**: adversarial unit fixtures for malformed and duplicate success banners, no source completion, missing/incoherent/zero frame measurements, dropped PCM, clipping and inflated claims. Windows native CI watches both new files and executes `test_PositiveSourceCheck.ps1`.

## Acceptance and limitations
- Required: exact-head Windows native CI, including the new source+physical positive validator, existing source-gain JSON adversarial suite, no-source negative validator and bridge build. Never label authored tests as *executed* until a real CI run succeeds.
- **This is not a physical audio PASS**: no disposable VM was provisioned, no signed driver installed, no third-party commercial application tested, and no sound pressure or owner hardware measurement performed by this source-only branch.
- The experimentally owned C++ DSP still does not prove system-wide native integration. Stable Windows currently uses user-installed/attached Equalizer APO; unmodified stable Android/Windows bundles remain Windows 0.1.16 + Android 0.1.12 as of initial review.
- Next physical gate: snapshot-backed VM, locally trusted lab-signed driver, actual independent source into virtual render with the same physical WASAPI endpoint, before/after negative controls, 100/150 matched 440 Hz gain, exact identity and undo/cleanup evidence. Only then consider true native route certification.
- Keep UI work #109 isolated, do not make a release or update the public feed from this source-only validation change.
