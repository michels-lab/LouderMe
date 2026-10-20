# Operación Granada — preflight for the NEXT LouderMe distribution

**Status (2026-10-20): source-only readiness campaign, NOT a version bump, public release or authorization to publish.**

## Integrated source milestone

- PR #78: real Windows Developer Edition; Start Menu shortcut, developer/public profile isolation and anchored WinForms audit notes.
- PR #80 and #81: installed Android Home/About QA and exact-only disposable Pixel Launcher ANR recovery.
- PR #82: Windows Developer read-only APO file/config-mismatch and default endpoint readiness findings.
- PR #79: separate Android `com.michelslab.louderme.developer` flavor with real rendered Compose section selection, durable notes, read-only audit, JSON and AI prompt.
- Android exact-head premerge CI `39402929535` and Windows CI `39402918588` passed. Integrated Android commit: `c3fdce0`. These are **emulator and CI proofs, not owner physical-audio acceptance**.

## What a release does NOT mean

The Windows API and Equalizer APO file-readiness probes do not prove the Windows playback device applies gain. The Windows native virtual endpoint #46/#48 is not integrated. The Android flavor is a **debug-only separately installable developer test artifact**; it is not a sustainably signed, officially published Developer APK. A public Sideload app release and an independent Developer APK publication are distinct deliverables.

## Safe release preparation gates (do not skip)

| Gate | Android stable Direct | Windows stable Desktop |
|---|---|---|
| Owner authorizes release + intended versions | Required | Required |
| Version identity | Increase `versionCode` beyond 13 and versionName beyond `0.1.12`; never recycle existing `v0.1.12` tag | Increase project/installer version beyond `0.1.13`; release note must match `desktop/releases/v*.md` |
| Compilation | Play/Direct/Developer debug tests; release unsigned build | .NET Release build + single-file EXE + Inno installer |
| Exact installed UI | Home/About on candidate; independent Developer notes/save/restart/JSON in emulator | Installed public Home/About and actual Developer WinForms controls/audit |
| Exact *shipping* binary | Canonical permanent signer verified by `apksigner`, checksum verified, final signed APK installed and Home/About captured at two viewport sizes | Full installer installed and uninstalled; installed `LouderMe.exe` starts, independent Developer shortcut points to it with `--developer`, installed Developer audit smoke passes |
| Publisher | Public GitHub/Direct feed changed only with owner release instruction and passing automated gates | Desktop release tag only with owner-approved versioned release notes and passing full installer checks |
| Postpublication | Verify public unified links/feed and updater compatibility; owner phone test still outstanding | Verify public installer/feed and updater; owner on-device Windows audio route/listening tests still outstanding |

## Current owner hardware validation (BLOCKED until owner test)

- **Windows:** using the actual default speakers, headphones and (if used) Bluetooth, confirm selected Equalizer APO Configurator endpoint, attach/enable the APO if needed, exercise optional -18 dB listening probe followed by automatic restoration, compare public Boost 100/150/200/250% and the seven EQ bands. Do not claim audible gain from file checks.
- **Android:** install a properly signed release on target Samsung and verify update from the currently installed signer, boost and EQ, navigation, consented updater. Install `.developer` separately only if approved for owner QA.
- **Both:** verify screenshots captured from exact final release artifacts; visually review published binaries after release according to master standard, and keep physical audio QA separate from emulator results.

## Distribution controls

The Android `release_guard` currently excludes already-existing tags, so merging **without a version bump** does not generate another Android stable APK. Changing version on `main` is release-active; do not do it as routine cleanup. The Windows release workflow triggers from new `desktop/releases/v*.md` on `main`, so do not create that file without authorization. The preflight upgrades the Windows release CI with actual Developer contract tests and live WinForms audit smoke. Windows normal-build CI **and** release CI invoke the same `desktop/tools/check_developer_installed.ps1` on the **installed** executable and its separate Start Menu shortcut. This detects missing/incorrect `--developer` entry and a failing installed live auditor before public distribution. It **does not** add any new release triggers. The Android `release_guard` also waits for the independently **installed** Developer Edition note/save/restart/audit/JSON workflow, not just ordinary Home/About visuals, before enabling new-version sideload signing. Android signing Secrets/continuity issue #40, final release evidence #33, native audio #46/#48 and Developer Edition owner acceptance #77 remain tracked separately.

**No versions were incremented, no new release notes added, no releases or public update feeds changed in this block.**
