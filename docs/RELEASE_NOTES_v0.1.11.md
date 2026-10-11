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
- **Owner-approved one-time signing migration:** Android v0.1.11 Direct uses a NEW permanent signing certificate. This is intentionally different from Android v0.1.10 and earlier; **in-place APK updates are NOT supported**. Back up any local data, uninstall the old LouderME APK, and install v0.1.11 fresh. Android data may be deleted by uninstalling.
- The new signing certificate is pinned by CI (SHA-256 starts `4a5bb9d9`). Future Direct releases MUST retain the new certificate and publish an APK SHA-256. Do not regenerate the keystore. Google Play delivery is separate.

## Validation
- CI must build both flavors, verify the installed Android UI, and verify the final signed Direct APK uses the owner-approved new certificate.
- The private signing keystore and all credential values live only in GitHub Actions Secrets, never in source or public release artifacts.
- Physical Samsung / Bluetooth audio behavior, app-coverage scope and Google Play delivery require separate real-device/provider tests.
