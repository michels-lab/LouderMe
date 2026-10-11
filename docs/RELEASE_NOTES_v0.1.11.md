# LouderME Android v0.1.11

Michel's Lab coordinated stable Android release.

## Included
- Current 0–100% device/media volume control, independent 100–250% boost on supported Android audio-effect routes, seven-band equalizer and saved presets.
- Official LouderME/Michel's Lab branding, About navigation, and start-with-phone option.
- Current Play and Michel's Lab Direct builds and verified direct updater behavior.

## Reliability
- Installed-emulator Home/About screenshot coverage now tolerates delayed accessibility hierarchy initialization and malformed transient uiautomator XML, without permitting a missing About control.
- Existing versioned safety, release integrity and permission separation remain in place.

## Not included
- No claim of guaranteed third-party audio processing across all Android apps.
- The experimental Windows C++ DSP/virtual driver is not an Android system-wide audio implementation.

## Distribution
- Package: `com.michelslab.louderme`
- versionName: `0.1.11`
- versionCode: `12`
- Michel's Lab Direct APK MUST retain the original signing certificate and publish its SHA-256. Google Play delivery is separate.

## Validation
- CI must build both flavors and verify the installed Android UI.
- Physical Samsung / Bluetooth audio behavior, app-coverage scope and Google Play delivery require separate real-device/provider tests.
