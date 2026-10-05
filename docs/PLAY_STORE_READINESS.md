# LouderMe — Google Play readiness

Last reviewed: 2026-10-05

## Identity
- App: LouderMe
- Developer/studio: Michel's Lab
- Package: `com.michelslab.louderme`
- versionName: `0.1.3`
- versionCode: `4`
- compileSdk / targetSdk: `36`
- minSdk: `29`

## Update channel
Production Play installs use Google Play In-App Updates.

v0.1.3 exposes update state on Home and About and supports automatic availability checks plus explicit Check/Install actions.

Direct GitHub debug APKs are explicitly a test channel.

## Foreground service
LouderMe uses `foregroundServiceType="specialUse"` to keep a user-started audio effect active while the user listens in another app.

Before Play submission:
- explain this exact user-visible audio-processing use case in the foreground-service declaration;
- confirm Play accepts the special-use case;
- keep the persistent notification and user-controlled off switch.

## Privacy
Draft: `docs/PRIVACY_POLICY.md`

Current design:
- no microphone permission;
- no audio capture/recording/upload;
- no analytics SDK;
- no advertising SDK;
- local boost and equalizer settings;
- Google Play update service for update management.

## Signing
Production upload keystore and passwords are not yet configured in this repository and must never be committed.

## Validated technical base
- [x] Current CI builds/tests real HEAD.
- [x] Target Samsung boost test.
- [x] Session-0 boost audibly changes external-app output on target Samsung.
- [x] Unsupported/degraded handling retained for other devices.
- [x] In-app Play updater infrastructure.
- [x] Home-visible update state.
- [x] Local equalizer configuration/presets implemented.

## Remaining production gates
- [ ] Validate session-0 EQ audibly on target Samsung hardware.
- [ ] Generate signed AAB.
- [ ] Configure Play App Signing/upload key.
- [ ] Publish final privacy-policy URL.
- [ ] Complete Play foreground-service declaration.
- [ ] Internal testing install from Google Play.
- [ ] Confirm in-app update behavior using Play test track.

## Audio terminology
Store/listing copy must describe 150–250% values as **digital signal gain / boost levels**. Do not claim fixed dB SPL or literal multiplication of perceived loudness.

EQ values are target band gain and may be mapped/clamped to the device's native Equalizer implementation.
