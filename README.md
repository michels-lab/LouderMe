# LouderMe

**Make everything louder.**

LouderMe is an Android audio utility by **Michel's Lab** for increasing quiet media output, shaping sound with a real equalizer, and exposing the state of the Android audio engine instead of hiding it behind fake controls.

## Current public release bundle

**Android v0.1.10 + Windows Desktop v0.1.5**

Michel's Lab publishes the current stable Android and Windows binaries together on one public GitHub release page. Platform versions remain explicit when their release lines differ.

### Global Boost
- 100%, 125%, 150%, 175%, 200%, 225%, 250%.
- Fine 100–250% slider.
- Target signal-gain readout.
- Output-route and engine diagnostics.

### Equalizer
Seven LouderMe target bands:
60 Hz, 150 Hz, 400 Hz, 1 kHz, 2.5 kHz, 6 kHz, 12 kHz.

Presets:
Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones, plus Custom.

## Updates

LouderMe now has two explicit distribution flavors.

### Google Play
The `play` flavor:
- uses Google Play In-App Updates;
- checks automatically;
- exposes Check/Install actions;
- does **not** request `REQUEST_INSTALL_PACKAGES`.

### Michel's Lab direct / sideload
The `sideload` flavor:
- checks a public Michel's Lab update manifest automatically;
- downloads a newer APK automatically;
- validates SHA-256;
- validates package ID and versionCode;
- validates that the APK is signed by the same LouderMe sideload release key;
- then hands installation to Android.

Android still requires system/user confirmation for APK installation.

Public feed:
`michels-lab/michel-s-life-releases/louderme/latest.json`

The LouderMe source repository remains private.

## One-time signing transition

Versions through v0.1.3 were GitHub CI debug APKs. Hosted CI debug certificates are not a durable update identity.

v0.1.4 establishes the first **stable Michel's Lab sideload signing key**. Because Android only allows in-place updates signed by an accepted signing identity, an existing v0.1.3 debug installation cannot be silently converted to the new stable key.

The one-time migration is:
1. install/save any desired settings manually;
2. uninstall the old debug-signed LouderMe;
3. install the stable-signed v0.1.4 sideload APK;
4. from then on, the direct updater can perform future same-key updates.

## Visual system

LouderMe follows the current IG Cleaner Pro family language:
near-black workspace, layered blue-black surfaces, cyan/blue operational accents, gold highlights, compact status badges and dashboard cards.

## Identity
- Product: `LouderMe`
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Current Android version: `0.1.10`
- Current Windows Desktop version: `0.1.5`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved


## Direct auto-updates

LouderMe now has two update channels:

- **Google Play** builds use Play In-App Updates.
- **Michel's Lab Direct** builds use a public manifest and stable signed APKs while keeping source private.

Direct updates verify SHA-256, package identity, version and signing identity before Android receives the package.

v0.1.4 is the stable-signing migration baseline. Older CI-debug APKs require one uninstall/reinstall because Android will not accept a new signing identity as an in-place update. After the stable v0.1.4 baseline is installed, later direct releases can update in place.

Android still shows the required system installation confirmation.


## Unified public release

The customer-facing public binary channel groups the current stable Android and Windows builds in one GitHub release. Android and Desktop keep separate updater manifests and real platform version numbers, but both manifests point to the same release page.

When one platform is updated, the public publisher carries forward the validated current binaries of the other platform so the release remains complete.
