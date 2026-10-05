# LouderMe Architecture

## Core success criterion
LouderMe increases general phone audio output from apps such as Spotify, YouTube, Instagram and browsers, then lets the user shape that signal transparently.

## Distribution flavors

### `play`
BuildConfig update channel: `play`.

Uses Google Play In-App Updates. The manifest intentionally does not request `REQUEST_INSTALL_PACKAGES`.

### `sideload`
BuildConfig update channel: `direct`.

Uses the Michel's Lab public update manifest while keeping source code private.

Manifest:
`https://raw.githubusercontent.com/realmichelduarte/michel-s-life-releases/main/louderme/latest.json`

Only the sideload manifest adds `REQUEST_INSTALL_PACKAGES`.

## Direct updater pipeline

1. App checks `latest.json` over HTTPS.
2. If `versionCode` is newer, it downloads the APK.
3. It verifies the manifest SHA-256.
4. It inspects the downloaded APK with Android PackageManager.
5. Package name must equal `com.michelslab.louderme`.
6. APK versionCode must match the manifest and be newer than the running build.
7. Candidate signing-certificate SHA-256 must intersect the installed app's signing history/current signers.
8. If the app is not yet allowed to install packages, Android's per-source settings screen is opened.
9. PackageInstaller streams the already-verified APK into an install session.
10. Android presents any required user confirmation.

The updater does not contain a GitHub token.

## Signing identity

A stable private sideload key is required for in-place APK updates.

GitHub-hosted debug keystores are unsuitable because a fresh hosted runner can generate a different debug key.

v0.1.4 therefore bootstraps one stable sideload signing key in a private CI artifact. The private key must then be transferred into GitHub Actions secrets or another proper secret manager and never committed to Git.

## Audio foreground service
`AudioBoostService` is a user-started foreground service using Android's `specialUse` foreground-service type.

## Boost engine
1. `LoudnessEnhancer(0)`
2. fallback `DynamicsProcessing(0)`

Target Samsung external-app boost is audibly validated.

## Equalizer engine
`SessionZeroEqualizer` uses `Equalizer(0, 0)`.

Product target bands:
60, 150, 400, 1000, 2500, 6000, 12000 Hz.

Target bands are mapped to Android-reported native EQ bands and clamped to the device range.

## Remaining audio milestones
- validate EQ audibly on target Samsung;
- limiter/compressor for high boost;
- output-device profiles;
- Smart Boost Beta;
- signed Play AAB/internal testing.
