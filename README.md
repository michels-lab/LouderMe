# LouderMe

**Make everything louder.**

LouderMe is an Android audio utility by **Michel's Lab** for increasing quiet media output, shaping sound with a real equalizer, and exposing the state of the Android audio engine instead of hiding it behind fake controls.

## Current public release bundle

**Android v0.1.11 + Windows Desktop v0.1.11** (last verified public bundle). Maintenance v0.1.12 is a new candidate until GitHub source releases, signatures, installed UI gates and unified public publisher all succeed.

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
`michels-lab/LouderME-Releases/louderme/latest.json` — current LouderMe Direct source accepts **only** the dedicated product feed. Previously installed old versions may still contain earlier fallback logic.

The LouderMe source is maintained separately from the **public binary-release repository** `michels-lab/LouderME-Releases`. Do not confuse the source repo's GitHub releases with the customer-facing unified release.

## One-time signing transition

Versions through v0.1.3 were GitHub CI debug APKs. Hosted CI debug certificates are not a durable update identity.

v0.1.11 established the current permanent Michel's Lab Direct signer (SHA-256 certificate starts `4a5bb9d9`), different from prior v0.1.10 and earlier. Versions v0.1.11 and later must retain this signer for in-place Direct updates. Earlier signers cannot update into this line in place.

For Direct installations at v0.1.10 or earlier, back up settings, uninstall the old package (this may remove app-local data), then install v0.1.11 or later. Users already on v0.1.11 should be able to update to v0.1.12 with the SAME certificate if the signed release passes CI.

## Visual system

LouderMe follows the current IG Cleaner Pro family language:
near-black workspace, layered blue-black surfaces, cyan/blue operational accents, gold highlights, compact status badges and dashboard cards.

## Identity
- Product: `LouderMe`
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Current verified public Android version: `0.1.11`; candidate source version: `0.1.12`
- Current verified public Windows Desktop version: `0.1.11`; candidate source version: `0.1.12`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved


## Direct auto-updates

LouderMe now has two update channels:

- **Google Play** builds use Play In-App Updates.
- **Michel's Lab Direct** builds use a public manifest and stable signed APKs while keeping source private.

Direct updates verify SHA-256, package identity, version and signing identity before Android receives the package.

v0.1.11 is the current permanent-signing baseline for Direct updates. Versions signed with an older key cannot upgrade in place to this certificate. Future releases retain the v0.1.11 signing identity.

Android still shows the required system installation confirmation.


## Unified public release

The customer-facing public binary channel groups the current stable Android and Windows builds in one GitHub release. Android and Desktop keep separate updater manifests and real platform version numbers, but both manifests point to the same release page.

When one platform is updated, the public publisher carries forward the validated current binaries of the other platform so the release remains complete.
