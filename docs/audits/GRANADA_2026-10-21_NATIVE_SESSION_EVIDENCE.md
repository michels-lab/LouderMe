# Operación Granada — LouderMe native Windows VM evidence correlation

Date: 2026-10-21
Owner: LouderMe (Michel's Lab)
Status: SOURCE CANDIDATE — exact-head CI and installed driver proof required.

## Observed defect
The physical-gain verifier formerly accepted two valid-looking positive 100% and 150% JSONs without itself requiring the before/after no-stimulus controls. Those controls existed in the VM orchestration but were not bound to the numeric verifier. Records also carried no cross-phase identifier for actual virtual/physical devices or the two managed DLLs. A stale/mixed pair of numeric result files could therefore be mistaken for one complete session by a caller invoking only the validator.

## Implementation
- Generate a fresh GUID in disposable driver lab; thread it through before, 100%, 150%, after and final acceptance.
- Fingerprint endpoint identifiers, independent tone DLL and bridge DLL via SHA-256; compare redacted fingerprints across all four phases. No raw endpoint IDs or PCM are placed in JSON.
- Reject missing controls, unexpected control phase, source launched during a silence control, missing-440 failure absent, contamination above amplitude 0.003, missing/invalid listener frames, cross-session/device/binary mismatches.
- Preserve exact physical matched-440 + coherence and 1.30–1.70 measured amplitude ratio acceptance of the existing numeric validator.
- Expand device-free adverse fixtures. This does not set endpoint defaults, install drivers, touch personal machines, claim acoustic SPL or change public app binaries.

## Validation plan
1. Native Windows bridge CI PowerShell parser checks for lab/probe/verifier/tests.
2. Device-free test_IndependentGainCheck.ps1 positive fixture and 24+ rejection cases.
3. Existing NoSourceControlCheck regression, native C++ DSP, managed bridge and Android nonregression jobs.
4. Separate owner-approved signed driver inside snapshot-backed VM to obtain actual independent-source and physical WASAPI evidence. NOT EXECUTED by CI.

## Release gate / remaining risks
Do not claim OS-wide third-party audio processing, physical acoustic loudness, driver trust or production release from source CI. Fingerprints prove within-record consistency, not that the installed kernel driver or physical hardware has been cryptographically attested. #46/#48 remain OPEN. No release requested, no public version increment. This report is a product-specific implementation note; promote a reusable cross-product rule only after exact-head integration.
