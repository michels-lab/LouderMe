# LouderMe Android v0.1.12 — maintenance release

Michel's Lab stable maintenance update. This release does not claim universal Android third-party audio processing.

## Included
- Dedicated LouderME-Releases update feed only; no fallback to Michel's Life or another application's release channel.
- Validate update manifest schema, package ID, sideload channel, HTTPS host/repository path, checksum, and version before download or cached installation.
- Preserve Android package, version and same-signing-identity checks before asking the OS to install.
- Keep independent Android media volume (0–100%), supported Global Boost (100–250%) and seven-band EQ, with existing platform limitations.

## Release assurance
- Play and sideload builds and unit tests run in CI.
- Both debug build UI capture and **the exact FINAL signed release APK** must pass installed-emulator Home/About rendered UI checks. Publication is blocked if the signed APK cannot be installed, its UI is not reachable, or the visual manifest does not match its SHA-256.
- SHA-256 and owner-approved permanent sideload signing certificate are verified before source publishing.

## Android signer and compatibility
- Package: `com.michelslab.louderme`.
- versionName: `0.1.12`.
- versionCode: `13`.
- **Same signing identity as v0.1.11** (certificate SHA-256 `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3`); v0.1.11 users can update in place. Do not regenerate/replace this key.
- v0.1.10 and earlier used a different signature. Direct APK updates across that earlier signer transition are not supported; uninstalling an older APK may erase local data.
- Google Play availability is separate from the GitHub sideload release.

## Not included
- No native interception of all other Android applications' media streams.
- The experimental Michel's Lab C++ Windows audio engine and unsigned virtual-driver lab are NOT built into this Android APK.
- Physical Samsung/BT/Realtek playback and hearing safety are not certified by synthetic or emulated tests.
