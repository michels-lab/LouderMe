# LouderMe — Google Play readiness

Last reviewed: 2026-10-05

## Identity
- App: LouderMe
- Developer/studio: Michel's Lab
- Package: `com.michelslab.louderme`
- versionName: `0.1.1`
- versionCode: `2`
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

## Remaining production gates
- [ ] Current CI build green with real compile/tests.
- [ ] Physical Samsung device test.
- [ ] Confirm session-0 effect actually changes external-app output.
- [ ] Decide behavior if Samsung rejects/ignores session 0.
- [ ] Generate signed AAB.
- [ ] Configure Play App Signing/upload key.
- [ ] Publish final privacy-policy URL.
- [ ] Complete Play foreground-service declaration.
- [ ] Internal testing install from Google Play.
- [ ] Confirm in-app update behavior using Play test track.
