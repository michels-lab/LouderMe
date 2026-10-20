# Android installed Home/About navigation reliability — Granada 2026-10-20

Issue #33 requires rendered evidence from actual final shipping APKs; this workstream improves the real UI capture routine, without fabricating screenshot success.

## Observed failure

Android CI 39390523676 successfully built all normal app variants but the installed debug emulator UI test reported Home labels after a single tap on About. The real \`About\` button existed in the accessibility tree, but \`Back\` was absent after the 24-second wait. That result is a **failed installed UI transition**, not proof of a broken release, and not a passing screenshot gate.

## Isolated root hardening

1. Use the actual accessibility hierarchy to resolve the *clickable parent* of an exact matching Compose text/semantics label, rather than assuming that the small Text child box is the button hit target.
2. Print exact control target and tap coordinates in CI for traceability.
3. Attempt a bounded maximum of three **real taps** on the actual installed UI. Re-read hierarchy between attempts; never synthesize success.
4. Require observed \`Back\` and \`About\` semantics in the installed app before taking a passing About screenshot. If all attempts fail, throw and retain an unqualified \`diagnostic-about-<viewport>.png\` artifact.
5. Pure standard-library Python tests cover clickable-parent selection, text-only fallbacks and absent/invalid bounds; actual emulator install and signed candidate gates remain separate.

This does not change LouderMe's audio engine, UI rendering, user data, signing, version numbers or public releases. It does not claim acoustic validation. The exact final signed APK still needs its own separately verified screenshot evidence.
