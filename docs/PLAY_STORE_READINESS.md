# LouderMe — Google Play readiness

Last reviewed: 2026-10-05

## Identity
- App: LouderMe
- Developer/studio: Michel's Lab
- Package: `com.michelslab.louderme`
- versionName: `0.1.4`
- versionCode: `5`
- compileSdk / targetSdk: `36`
- minSdk: `29`

## Distribution split

### Play flavor
- update channel: Google Play In-App Updates;
- does not request `REQUEST_INSTALL_PACKAGES`;
- suitable foundation for Play policy review.

### Sideload flavor
- direct Michel's Lab public update feed;
- `REQUEST_INSTALL_PACKAGES` exists only here;
- uses Android PackageInstaller and requires system/user approval.

This split avoids shipping a sideload-install permission in the Play flavor of an audio utility.

## Foreground service
LouderMe uses `foregroundServiceType="specialUse"` to keep a user-started audio effect active while the user listens in another app.

## Privacy
Draft: `docs/PRIVACY_POLICY.md`

## Signing
Play and sideload signing identities are separate concerns.

v0.1.4 bootstraps a stable sideload key. Production Play upload/App Signing configuration remains pending.

## Validated technical base
- [x] Current CI builds/tests real HEAD.
- [x] Target Samsung boost test.
- [x] Session-0 boost audibly changes external-app output on target Samsung.
- [x] In-app Play updater infrastructure.
- [x] Direct updater security checks implemented.
- [x] Play/sideload permissions separated by product flavor.
- [x] Local equalizer configuration/presets implemented.

## Remaining production gates
- [ ] Validate session-0 EQ audibly on target Samsung.
- [ ] Transfer sideload bootstrap key into secure CI secrets.
- [ ] Generate signed Play AAB.
- [ ] Configure Play App Signing/upload key.
- [ ] Publish final privacy-policy URL.
- [ ] Complete Play foreground-service declaration.
- [ ] Internal testing install from Google Play.
- [ ] Confirm Play in-app update behavior on test track.
