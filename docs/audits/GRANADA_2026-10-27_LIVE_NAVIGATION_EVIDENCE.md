# Operación Granada — live navigation proof and clipping diagnostics

Date: 2026-10-27. Scope: Developer Edition in LouderMe Windows and Android (#77). No stable consumer release or audio-driver work.

## Verified gap

Android Developer "Locate" previously set a stored note's `focused` locator and switched Home/About. This was silently treated as locating the section even when the target's layout never appeared; it also kept stale `visibleBounds` entries when a conditional Compose control unmounted. A note with an obsolete locator could navigate and highlight nothing, and a scrolled-off target could be called "rendered" though invisible on-screen.

Windows Developer `RunAudit` previously inspected whether an About button existed, was enabled and had positive width, but not whether its real clickable rectangle was inside the rendered host window. In a narrow/clipped viewport a real but unreachable About button could pass. It did not record exceptions/non-shown dialogs raised by a genuine user About navigation action.

## Three concrete capabilities

1. **Android source-verified Locate**: each selection creates a schema-limited in-memory request with exact Home/About anchor and UTC request timestamp. A successful navigation is recorded **only** when the requested exact target actually has measured positive, finite bounds intersecting the current Android window viewport. Unsupported/obsolete note target, wrong screen, unloaded screen, outside viewport and invalid geometry cannot give a false verified status. The inspector shows a "waiting"/"not yet observed"/"verified" state, and the structured Developer JSON includes precise request/verification metadata without user comments. A pending state is **not** labeled a confirmed navigation failure.
2. **Android registration cleanup**: `DisposableEffect` unregisters each control from the real inspector when the Compose element leaves composition; `activateScreen` still clears old screen mappings on navigation. Invalid live coordinates cannot be selected. Pure JVM geometry/target tests exercise valid, unmounted, wrong-screen, stale, degenerate, NaN/Infinity, old clock and offscreen paths.
3. **Windows actual navigation auditing**: the read-only Developer `RunAudit` now checks the *actual* About button's on-screen bounds in the host client viewport rather than only its existence, and checks an already open About form actually contains mounted visible UI. Source-only `OpenAbout` tracks a real WinForms Shown event and records a closed-code **actual** error when the modal was never shown or threw; raw exception details are never written to the Developer event history. A dedicated pure rectangle contract has seven clipped/unreachable adversarial cases in the existing executable Developer contract test.

## Boundaries

These are in-app runtime/geometry observations, **not** proof of audio gain, actual speakers, successful page navigation for every route, or screen reader accessibility. The Android pending report may remain pending for scrolled-off targets until the owner brings them into view. No simulated sign-in (public LouderMe has no auth), no personal comments collected automatically, no new permissions or default-device changes.

**Acceptance**: exact PR SHA Windows CI: build + real installed Windows launcher/Developer smoke + rendered Home/About at compact/full widths; Android CI: unit + actual emulator-installed Home/About + Developer Edition notes/audit persistence, all successful. No release; official public stable Windows 0.1.17 + Android 0.1.12 remain unchanged. #77 still open for broader runtime/render errors and user QA; #46/#48 still pending physical signed virtual audio route validation.
