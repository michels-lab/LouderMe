# LouderMe — Google Play readiness

Last reviewed: 2026-10-05

## Identity
- App: LouderMe
- Developer/studio: Michel's Lab
- Package: `com.michelslab.louderme`
- versionName: `0.1.2`
- versionCode: `3`
- compileSdk / targetSdk: `36`
- minSdk: `29`

## Update channel
Production Play installs use Google Play In-App Updates. Direct GitHub debug APKs are explicitly a test channel.

## Foreground service
LouderMe uses `foregroundServiceType="specialUse"` to keep a user-started audio effect active while the user listens in another app.

Before Play submission:
- explain this exact user-visible use case in the foreground-service declaration;
- confirm Play accepts the special-use case;
- keep the persistent notification and user-controlled off switch.

## Privacy
Draft: `docs/PRIVACY_POLICY.md`

Current design:
- no microphone permission;
- no audio capture/recording/upload;
- no analytics SDK;
- no advertising SDK;
- local boost settings only;
- Google Play update service for update management.

## Signing
Production upload keystore and passwords are not yet configured in this repository and must never be committed.

## Validated test release\n- GitHub test release: `v0.1.1`.\n- Main CI run: `37299949758` — success.\n- APK package verified as `com.michelslab.louderme`.\n\n## Remaining production gates
- [x] Current CI build green with real compile/tests.
- [x] Physical Samsung device test.
- [x] Confirm session-0 effect actually changes external-app output (user-reported audible validation).
- [x] Target Samsung accepts the current session-0 path; unsupported/degraded handling remains for other devices.
- [ ] Generate signed AAB.
- [ ] Configure Play App Signing/upload key.
- [ ] Publish final privacy-policy URL.
- [ ] Complete Play foreground-service declaration.
- [ ] Internal testing install from Google Play.
- [ ] Confirm in-app update behavior using Play test track.


## Audio terminology

Store/listing copy must describe 150–250% values as **digital signal gain / boost levels**. Do not claim a fixed dB SPL increase or literal multiplication of perceived loudness.
