# PDR — Native Windows global audio: evidence before distribution

- **Date:** 2026-10-21
- **Review method:** ONE ChatGPT analysis using Michel's Lab's portable Contrarian, User Advocate and Intelligence Architect lenses, plus domain/trust synthesis. These are **not three autonomous agents or completed physical tests**.
- **Product:** LouderMe, Android + Windows; Windows native path only. Inspected product `main` at `7ea4f26`, PR #88, `native/windows-bridge/Program.cs`, `native/virtual-driver/run_disposable_lab.ps1`, `native/independent-tone/Program.cs`, issues [#46](https://github.com/michels-lab/LouderMe/issues/46) and [#48](https://github.com/michels-lab/LouderMe/issues/48).
- **Status:** PROPOSED review and controlled test work. PR #88 source integration/hosted tests passed; **no actual installed LAB driver, third-party PCM, acoustic SPL or native public Windows shipping demonstrated**.
- **Owner goal:** LouderMe's OWN differentiated Boost/EQ must audibly process Windows application sound, not merely change endpoint volume, require Equalizer APO forever, or present a fake active indicator.

## Lens 1 — Contrarian: do not confuse more tests with the missing signal path

1. Strongest objection: SysVAD-derived virtual render may still return **silence** on loopback. A named Windows endpoint, compiled SYS and valid INF/CAT do not mean user PCM leaves a real application. The current simulator cannot establish this without signed driver installation in a disposable VM.
2. The independent 440 Hz source app is an intentional progress gate, but it is still a controlled synthetic app, not Spotify, Chrome, system sounds, USB DAC, exclusive-mode or DRM. Never extrapolate generic application coverage.
3. A 150% linear DSP gain claim can be masked by Windows effects, clip or sound pressure limits. A numerical matched amplitude ratio in actual physical endpoint loopback is necessary but not sufficient for headphone safety or sound quality.
4. Alternative to building a new virtual driver immediately: evaluate an owned **custom APO** prototype against SysVAD, with the same third-party/latency/signing standard. Do not choose merely because an existing Equalizer APO fallback happens to work.

## Lens 2 — User Advocate: first-run and rollback matter more than a 250% label

1. A user needs obvious **native active / bypassed / unavailable / legacy fallback** states, not a green “boost active” badge when there is no virtual endpoint or PCM route.
2. A driver failure, app crash or hardware disconnect must restore the original output where possible and explain the next safe action. Never silently switch Windows defaults, route to a dead virtual endpoint or trap users without sound.
3. Keep initial loudness conservative and never override per-device hardware safety. Show which actual device is processed, and why exclusive-mode/DRM streams may bypass a shared-mode route.
4. Preserve separate user-facing Settings/Audio and Developer-only diagnostics. A lab console or raw driver installer is not an accessible consumer UX.

## Lens 3 — Intelligence Architect: deterministic evidence before AI

1. Retain `sourceFrames`, `physicalEmitted`, frame drops, queue resets, callback workload and command coalescing counters, with no user-audio recordings or cloud transfer.
2. Upgrade physical proof from single “PASS” strings to structured **source-process provenance**, independent physical 440 Hz amplitude at 100%/150%, coherence, format and exact SHA-bound artifacts, with negative/invalid evidence tests.
3. Split logic into pure verifiers exercised by hosted CI and a small privileged actual-device lab runner. Avoid coupling driver installation, DSP controls and numerical acceptance inside one untestable script.
4. Do not add “AI sound optimization” yet: there is no trustworthy end-user real audio dataset, consent/evaluation pipeline or established native route to optimize.

## Domain/trust and product-differentiation check

- **Observed:** prior PR #88 exact-head C++ DSP, .NET bridge, pinned SysVAD WDK packaging and Android regressions all passed on GitHub Actions. Current Windows `main` contains a separate source app and a gated manual VM test, **not a signed installed driver**.
- **Unknown:** whether the SysVAD render miniport provides independently observable non-silent virtual-loopback PCM, whether the bridge can safely process arbitrary third-party shared-mode streams, true device latency, peak safety and stability through hot-plug.
- **Safety:** retain local-only testing, explicit consent and separate physical endpoint IDs; require signed and trusted driver only in disposable snapshot-backed VM, never a primary PC; no raw user PCM stored.
- **Differentiation:** owning a robust real-time C++ DSP is valuable, but the differentiator for users only materializes after reliable OS integration, supported formats and safe rollback.

## Opportunity matrix

| ID | Proposal / simpler alternative | Evidence vs hypothesis | Impact | Confidence | Effort | Risk | Reversibility | Priority | Owner / exact acceptance |
|---|---|---|---:|---:|---:|---:|---:|---|---|
| W1 | Run existing isolated VM **external process** signal proof; if no VM, do not claim PCM route | Source gate observed; actual device unknown | 5 | 4 | 4 | 4 | 4 | P0 | #48 / separate signed VM, 440 Hz both virtual/physical, bounded frame drops and numeric 100→150 amplitude |
| W2 | Pure evidence validator with adversarial nonfinite, swapped and false-scope test vectors | CI can validate now; physical gains unknown | 4 | 5 | 2 | 1 | 5 | P1 | Native Windows CI / invalid JSON must fail, accepted 1.5 numeric ratio must pass |
| W3 | Compare native owned APO and SysVAD route under same external-app proof | Both candidate architectures; performance unknown | 4 | 3 | 4 | 4 | 4 | P1 | #46 architecture decision / signed install, A/B latency & recovery |
| W4 | Draft conservative end-user route onboarding and visible native-available state | User experience inference, no installed UI proof yet | 4 | 3 | 3 | 3 | 5 | P2 | Desktop owner / actual installed UI, without claiming active if DSP silent |
| W5 | AI autotune on captured streaming system audio | No consent/permitted source/evaluation; speculative | 2 | 1 | 5 | 5 | 3 | P3 DEFER | **Do not implement** until real route evidence and user study |

*Scores are ordinal decision aids, not measured probabilities; 5 means high impact/evidence/effort/risk/reversibility as labeled.*

## Disagreements and simplification

- Contrarian favors evaluating a custom APO before continuing a costly virtual driver. Systems architect favors measuring the prototype already present before changing architecture. **Resolution:** keep the existing lab gate, document failure modes, run isolated VM or mark blocked. Only then compare an owned APO if the render stream cannot be captured safely and cheaply.
- User Advocate would rather remove the **native-active** indicator entirely until physical route exists. Engineering's smaller alternative is preserving the documented Equalizer APO fallback with honest “legacy” labeling, which is safer than adding unsupported capability labels.
- Intentionally **rejected**: automatically changing Windows default output, enabling test-signing on a personal machine, or shipping a demo SysVAD driver based solely on green hosted CI.

## Questions that might change the priority (not blockers for safe source work)

1. Is a **snapshot-backed Windows VM with a locally trusted lab-signing setup and working physical audio passthrough** actually available to run the manual proof?
2. Which Windows versions and output devices are minimum initial supported scope: built-in Realtek only, USB DAC, Bluetooth, HDMI?
3. Should native global audio override the documented Equalizer APO fallback only after real installed end-to-end acceptance, or remain an opt-in experimental feature first?
4. What peak/true-peak and limiter policy is required before an owner-facing 150–250% gain control can be enabled safely?

## Next bounded action and ownership

**Decision:** W2 implemented as a source-only PR candidate by LouderMe native maintainer; rest PROPOSED/DEFERRED pending actual VM or owner approval. Native validation should accept 100/150 physical matched 440 Hz evidence only after confirming exact provenance, finite numbers, coherence, independent-app scope and expected ratio, with negative fixtures in CI. No code or documentation can substitute for W1 physical driver, and the portable council review cannot sign driver binaries, install software, or release an app.

**Follow-up:** LouderMe `native/virtual-driver/IndependentGainCheck.ps1` and `test_IndependentGainCheck.ps1`; issues #46/#48; master `standards/NATIVE_DSP_REALTIME_AND_ROUTE_QA.md`. Mark physical route verified only with a link to signed driver/VM evidence and the real independent-source PASS manifest, never the present PDR.
