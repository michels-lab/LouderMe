# LouderMe v0.1.4 — Automatic Direct Updates

v0.1.4 fixes the gap between the Google Play updater and the APK you actually install directly.

## Direct / sideload updates
The sideload build now:
- checks for a newer Michel's Lab release automatically;
- downloads it automatically;
- validates SHA-256;
- checks package ID and versionCode;
- checks the Android signing certificate;
- asks Android for per-source install permission when needed;
- opens Android's required installation confirmation.

No private GitHub token is embedded in LouderMe.

## Play stays clean
The Google Play flavor continues to use Play In-App Updates and does not request the sideload package-install permission.

## One-time migration
v0.1.3 and earlier APKs were CI debug-signed builds. v0.1.4 establishes a stable sideload signing identity.

That means the existing debug installation must be uninstalled once before installing the stable-signed v0.1.4 baseline. From that baseline onward, same-key direct updates can replace the installed app normally.

## Existing features preserved
- 100–250% boost;
- 7-band equalizer;
- presets and Custom EQ;
- IG Cleaner-family Audio Workspace;
- Home/About update status;
- engine/EQ diagnostics.
