# LouderMe — Infrastructure & External Services Audit

Last reviewed: **2026-10-06**

## Current architecture
- Android-native application.
- Native Windows Desktop application (WinForms/.NET 10) with self-contained EXE + Inno Setup packaging.
- Core audio processing is local/on-device.
- Production update path: Google Play In-App Updates.
- GitHub is used for source, CI and test/direct APK releases.
- No cloud database or account backend is required for the current product.

## Verified strengths
- Package namespace is `com.michelslab.louderme`.
- v0.1.1 build/test CI validates current HEAD before publishing.
- Checksum is published for direct APK test releases.
- Play updater dependencies are present.
- No reason exists to put audio-engine operation behind a cloud dependency.

## Gaps / required follow-up
1. Physical S26 Ultra A/B validation of 100% / 150% / 200% output remains the primary technical gate.
2. Complete production Play Console listing, signing and Play-delivered validation.
3. Validate In-App Updates from an actual Play-installed build.
4. Finish/validate mixer, EQ, preset persistence and product behavior after the audio engine path is proven.
5. Keep privacy/store disclosures aligned with any future permissions or data collection.

## Supabase / Google cloud decision boundary
Do not add Supabase for the current audio-booster feature set.

A backend only becomes justified if LouderMe later needs:
- account-based preset sync;
- cross-device user profiles;
- remote feature configuration;
- subscription/account entitlements;
- opt-in telemetry requiring a server.

If preset sync is introduced, store only the minimum preset/profile data and protect every user table with RLS.

## Secret rule
Play signing secrets, API credentials and any future backend secret keys must stay outside source. Client apps may contain only credentials explicitly designed to be public clients/publishable keys.


## 2026-10-06 — Desktop + startup architecture extension

### Android startup
- Adds opt-in `Start with phone`.
- Persists the preference locally.
- Uses `RECEIVE_BOOT_COMPLETED` + an explicit boot receiver.
- Restores the saved boost percentage through the existing foreground audio service.
- Does not force-open an Activity after reboot.
- Device reboot behavior remains a physical-device validation item.

### Windows Desktop
- Native WinForms surface; no browser/HTML shell.
- .NET 10 self-contained single-file EXE.
- Inno Setup installer, desktop/start-menu shortcuts and uninstall entry.
- Official LouderMe icon generated from the canonical SVG.
- Real Windows default-endpoint volume/mute control through NAudio.
- Opt-in `Start with Windows` through HKCU Run, launched with `--startup` and minimized to the system tray.
- CI builds and smoke-tests both the portable EXE and an installed copy.

### Explicit audio boundary
The first Desktop foundation does not claim >100% system-wide Windows gain. Native Windows endpoint volume alone cannot represent the 125–250% LouderMe targets. A real DSP/APO implementation is required before those targets can be applied rather than merely persisted/displayed.

### Pending
- Implement and validate the native Windows DSP/APO engine for true >100% boost.
- Implement system-wide Windows EQ on the same DSP path.
- Add the Desktop automatic-update feed before a production Desktop release.
- Add optional Authenticode signing when the Michel's Lab Windows certificate is available.
- Physical Windows audio/output-device validation.
