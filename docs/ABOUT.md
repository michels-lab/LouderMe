# LouderMe About and product-identity specification

## Product
- App: **LouderMe**
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Windows product: **LouderMe Desktop**
- Official product identity: **Option 4 — flowing waveform**
- Official tagline: **SOUND THAT LIFTS YOU**

Canonical product assets come from:
`realmichelduarte/Michel-Software-Standards/shared-assets/product-logos/louderme/`

Canonical developer/studio assets come from:
- `shared-assets/michel_duarte_avatar.jpg`
- `shared-assets/michels-lab/official-mark.png`
- `shared-assets/michels-lab/official-lockup.png`

## Mandatory identity hierarchy

Both Android and Windows native About surfaces follow:

1. **LouderMe product identity**
   - official waveform geometry;
   - LouderMe name;
   - real current build version;
   - product description/tagline.

2. **About the author**
   - canonical Michel Duarte portrait;
   - **Michel Duarte**;
   - developer role.

3. **Michel's Lab**
   - canonical parent-brand lockup/mark;
   - clear studio relationship subordinate to LouderMe.

4. **Social/contact**
   - Instagram;
   - Facebook;
   - LinkedIn;
   - GitHub;
   - Email.

Every visible social row must render a recognizable icon/glyph **and** the visible network name.

Canonical URLs are copied from:
`realmichelduarte/Michel-Software-Standards/brand/developer-profile.json`.

## Android implementation

Android About is a native Compose surface.

It includes:
- official LouderMe waveform/lockup expression;
- build-derived version and versionCode;
- canonical Michel Duarte portrait;
- canonical Michel's Lab parent-brand lockup;
- social rows in canonical order;
- update state and update action;
- Start with phone setting;
- local-processing privacy copy;
- copyright/license.

Android Home also uses the waveform as a design language:
- official mark in the command bar;
- waveform-derived Global Boost visual feedback;
- waveform-derived EQ curve;
- branded splash/launcher identity.

### Android update behavior
- Play builds use Google Play update delivery.
- Michel's Lab Direct builds use the public release feed, verify SHA/package/version/signing continuity and hand final installation to Android for user confirmation.

## Windows Desktop implementation

Desktop About is a dedicated native WinForms surface rather than a MessageBox.

It includes:
- canonical LouderMe waveform geometry;
- real assembly/product version;
- Windows stable-channel identity;
- canonical Michel Duarte portrait;
- canonical Michel's Lab lockup;
- canonical social/contact ordering;
- stable update check/install entry point;
- privacy/legal information;
- Equalizer APO third-party notice;
- explicit unsigned/AuthentiCode/SmartScreen state.

Desktop branding also includes:
- executable/shortcut/taskbar icon derived from the official LouderMe app icon;
- branded native splash/startup surface;
- LouderMe waveform header;
- waveform-derived boost activity;
- EQ curve visualization derived from the audio/waveform identity;
- Michel's Lab publisher metadata in the installer.

## Windows updates

Desktop checks the public stable feed:

`louderme-desktop/latest.json`

When a newer version exists:
1. LouderMe downloads the published installer;
2. verifies the published installer SHA-256;
3. only then asks Windows to launch the installer.

No installer is silently executed without an explicit user action.

## Privacy summary

LouderMe does not record, capture, store or upload audio content.

Android boost/EQ settings remain local.

Desktop settings remain local and system-wide boost/EQ are written to the local Windows APO configuration.

## Third-party dependency

LouderMe Desktop uses user-installed **Equalizer APO 1.4.2** as the local Windows system-effects backend for gain above the native endpoint maximum.

Equalizer APO is not bundled or redistributed by LouderMe.

## Rules
- Never hard-code a production display version separately from build metadata.
- Never claim universal compatibility solely because a session-0 effect or APO configuration exists.
- Never describe target digital gain as measured acoustic dB SPL.
- Never claim Windows Authenticode signing unless the release artifacts were actually signed with a publisher certificate.
- Product identity is LouderMe first; Michel's Lab appears as the parent studio/author brand rather than replacing the product.
