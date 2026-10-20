# Windows Developer audio-readiness audit — Granada 2026-10-20

This candidate extends the already integrated Windows Developer Edition. It does **not** change gain processing, the public application, EQ presets, startup, installer/update channel, audio permissions or device settings.

## Read-only diagnostics

The real Developer inspector now compares **Developer profile expected settings** with the existing Equalizer APO file using `EqualizerApoEngine.GetStatus(DesktopSettingsStore.Load())`. It classifies precisely:

| Code | Severity | What was observed |
|---|---|---|
| `APO_NOT_INSTALLED` | P1 | Equalizer APO installation absent |
| `APO_NOT_LINKED` | P1 | Installation exists but LouderMe configuration is not usable/linked/readable |
| `APO_SETTINGS_MISMATCH` | P1 | LouderMe Boost/EQ settings do not match on-disk APO configuration |
| `AUDIO_AUDIBILITY_UNVERIFIED` | P2 | Settings match the linked file; no audible-path evidence |

The inspector also enumerates the current **default multimedia render endpoint**, read-only. If unavailable it reports `DEFAULT_RENDER_ENDPOINT_UNAVAILABLE` without exporting the device name. Enumeration does not establish actual APO attachment. No output capture, system audio changes, elevation or automated loudness test occurs.

`--check-developer-contract` now covers all four APO classification outcomes through pure .NET fixtures; the preexisting `--developer-smoke-test` exercises actual WinForms rendering and audit on the Windows GitHub runner. Evidence still cannot establish endpoint-specific processing, external player routing, Bluetooth/wired routing, virtual device driver functioning, true +dB gain or clipping. These require owner-approved physical PC measurements.

## Pending

- Exact head Windows CI passing is required before integration.
- Physical PC session: check actual default output, selected Equalizer APO Configurator endpoint, 100/150/200/250% music test and 7 EQ bands; report recording only if owner explicitly enables it.
- Issue #46/#48 native virtual audio driver remains separate and blocked; this read-only audit does not replace it.
- No new stable release, app version or public update.
