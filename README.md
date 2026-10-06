# LouderMe

**Make everything louder.**

LouderMe is a cross-platform audio utility by **Michel's Lab** for increasing quiet media output, shaping sound with a real equalizer, and exposing the state of the Android audio engine instead of hiding it behind fake controls.

## Current release line

**Android v0.1.6 — official LouderMe branding**

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
`realmichelduarte/michel-s-life-releases/louderme/latest.json`

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
- Current Android version: `0.1.6`
- Current Desktop foundation version: `0.1.0`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved


## Direct auto-updates

LouderMe now has two update channels:

- **Google Play** builds use Play In-App Updates.
- **Michel's Lab Direct** builds use a public manifest and stable signed APKs while keeping source private.

Direct updates verify SHA-256, package identity, version and signing identity before Android receives the package.

v0.1.4 is the stable-signing migration baseline. Older CI-debug APKs require one uninstall/reinstall because Android will not accept a new signing identity as an in-place update. After the stable v0.1.4 baseline is installed, later direct releases can update in place.

Android still shows the required system installation confirmation.


## Windows Desktop

LouderMe now includes a **native Windows Desktop foundation** under `desktop/`.

- .NET 10 WinForms; no WebView and no HTML UI.
- Self-contained portable `.exe`.
- Inno Setup installer, matching the Michel's Lab Windows installation pattern used by FoamLens and Michel's Life.
- Official LouderMe flowing-waveform identity for the Windows executable/installer.
- Real Windows default-output volume and mute control.
- Opt-in **Start with Windows** using the current-user startup entry; startup launches minimized to the system tray.
- Android also has an opt-in **Start with phone** control that restores the saved boost level after boot.

### Desktop boost boundary

Windows Core Audio endpoint volume stops at the endpoint's actual maximum. LouderMe Desktop therefore does not fake 125–250% by relabeling 100%.

The Desktop UI can retain a requested 100–250% target, but system-wide gain above the Windows endpoint maximum remains gated on a real native DSP/APO module and physical validation.
