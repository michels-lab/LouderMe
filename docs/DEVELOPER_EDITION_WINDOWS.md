# LouderMe — Developer Edition Windows, Granada 2026-10-20

**Status:** Implemented on an isolated feature branch; not yet merged, released or verified on Michel's Windows PC. Android Developer Edition is a separate unresolved platform requirement (#77).

## Launch / isolation

The Windows installer has a dedicated **LouderMe Developer Edition** Start Menu shortcut. It starts the exact same LouderMe.exe, real WinForms controls and real Windows audio implementation with \`--developer\`; the public shortcut does not enable it. The Developer Edition uses a different single-instance mutex and \`%LOCALAPPDATA%/Michel's Lab/LouderMe.Developer/\` for settings, logs and owner notes. It does not import user settings, tokens or sessions. LouderMe currently has **no account login**; no sign-in integration is invented or claimed.

This is a developer-only audit tool, **not a replacement audio engine**. Manually operating Windows volume or processing sliders still affects the actual output / Equalizer APO and requires owner awareness. The Developer Edition intentionally disables public auto-start registry changes and public installer/updater operations, and runs no automatic update network check.

## Inspect / comment workflow

1. Start the Developer Edition shortcut. The normal Home opens with its separate diagnostics window.
2. Press **Select section / Add note**, then click a *real live control* in the Home or About window. Inspection consumes the mouse click without triggering the underlying control.
3. Enter the freeform owner observation, severity and status. Each entry captures real screen, stable full control-path locator, label, exact owner text, creation/update UTC timestamps and a unique ID.
4. Edit or delete an entry later; annotations persist across restarts in the isolated developer profile.
5. **Run audit** performs live WinForms control checks, fixed About visibility checks, read-only APO attachment/config checks and reports unresolvable annotations. A note whose anchored control is missing stays unresolved: never silently reattach it to a different button.
6. **Export JSON / Export AI prompt** opens a local, editable preview. Review/redact notes before selecting the output file. Automatic findings omit credentials, raw input, stack traces, local file paths and personally identifying data. No external upload or destructive audio action is used by the audit.

## Validation

The Desktop Windows CI for this branch is configured to:
- compile the real WinForms executable;
- run \`--check-developer-contract\` to check profile separation, exact owner comment in both exports, durable save/load, and preservation of corrupt notes;
- run \`--developer-smoke-test\` to open the actual MainForm and DeveloperDiagnosticsForm, inspect runtime controls, perform a read-only audit and exit nonzero on failure;
- retain the already-existing compact About, Windows Setup launch/uninstall, portable packaging, SHA-256 and installed UI render tests.

**Not yet validated:** installed Developer shortcut usability with owner, precise pointer selection on mouse/trackpad and different resolutions, About form annotations after navigation, real Windows audio observations, independent Android Developer package, real-provider sign-in (not applicable: public LouderMe has no account provider), official release artifact. A passing CI is not physical audio proof.

## Granada workstreams and reentry

1. **Windows developer inspector (this branch):** implementation + automated gates pending CI/PR. Owner: ChatGPT Granada.
2. **Android developer twin (#77):** separate Gradle flavor/package ID/storage, same real app UI, exact Compose section anchors, annotation review/export; requires separate mobile implementation and installed emulator/physical validation.
3. **Native Windows system-audio chain (#46/#48):** work in isolated virtual-driver/bridge branch. Do not change stable APO path; signed virtual endpoint, real third-party audio, fail-safe routing, measured latency and user-device approval remain open.
4. **Desktop physical device regression (#46):** validate owner-observed APO functionality across audio devices/Windows restart, distinguish config match from audible gain. Do not mark system-wide DSP proven.
5. **Exact final-shipping-binary UI evidence (#33):** align final signed/published binary with real installed UI capture; physical audition and signer continuity are separate.

**Never** publish a release from GRANADA alone. Keep the public LouderME-Releases channel separate from Michel's Life.
