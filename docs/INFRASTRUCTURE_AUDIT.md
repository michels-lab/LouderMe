# LouderMe — Infrastructure & External Services Audit

Last reviewed: **2026-10-07**

## Current architecture
- Android-native application.
- Native Windows desktop application (.NET 10 WinForms).
- Windows system-wide boost above 100% and desktop EQ use a local Windows APO effects backend through Equalizer APO 1.4.2.
- Core audio processing is local/on-device.
- Android update paths are split by distribution flavor: Google Play In-App Updates for `play`, and the Michel's Lab Direct updater for `sideload`.
- Windows Desktop uses the public stable installer feed with SHA-256 verification before launch.
- GitHub is used for source, CI and the unified public Android + Windows binary release surface.
- No cloud database or account backend is required for the current product.

## Verified strengths
- Package namespace is `com.michelslab.louderme`.
- Current CI validates exact HEAD before publication for both Android distribution flavors and Windows Desktop.
- Windows CI separately validates Portable launch and installed-Setup launch, not only build/install existence.
- SHA-256 is published for Android Direct APK, Windows Setup and Windows Portable binaries.
- Android Direct validates checksum, package/version and signing continuity before installation.
- Desktop updater validates feed schema/product/platform/channel and installer checksum before launch.
- Play updater dependencies are present.
- No reason exists to put audio-engine operation behind a cloud dependency.

## Gaps / required follow-up
1. Physical S26 Ultra A/B validation of Android Global Boost/EQ behavior remains the primary acoustic technical gate; repository CI cannot prove audible system-wide behavior.
2. Validate Android v0.2.0 Device Volume on the target phone against hardware volume buttons/system UI after release.
3. Complete production Play Console listing/signing and validate In-App Updates from an actual Play-installed build.
4. Validate Windows endpoint switching, real Device Volume sync and Equalizer APO audible 100–250% behavior on a physical Windows machine.
5. Windows Authenticode is still unconfigured, so Unknown publisher/SmartScreen remains an external trust-state limitation.
6. Keep privacy/store disclosures aligned with any future permissions or data collection.

## Supabase / Google cloud decision boundary
Do not add Supabase for the current audio-booster feature set.

A backend only becomes justified if LouderMe later needs:
- account-based preset sync;
- cross-device user profiles;
- remote feature configuration;
- subscription/account entitlements;
- opt-in telemetry requiring a server.

If preset sync is introduced, store only the minimum preset/profile data and protect every user table with RLS.

## Secret rule
Play signing secrets, API credentials and any future backend secret keys must stay outside source. Client apps may contain only credentials explicitly designed to be public clients/publishable keys.


## Windows desktop APO integration — 2026-10-06

LouderMe Desktop keeps Windows endpoint volume (0–100%) separate from digital Global Boost (100–250%).

Actual boost above 100% is implemented through the Windows system-effects/APO path using Equalizer APO 1.4.2. LouderMe does not redistribute the third-party installer; it links users to the official project and manages only its own `LouderMe.txt` include after installation.

Audio processing remains local. No account, telemetry, cloud database, audio capture upload, or new application secret is introduced.

Compatibility note: applications using ASIO or WASAPI exclusive mode can bypass Windows system effects. Endpoint attachment and audible behavior therefore remain physical-Windows validation items.


## Device Volume — v0.2.0 candidate

Device Volume is intentionally separate from Global Boost.

### Android
- real media-stream control through `AudioManager.STREAM_MUSIC`;
- 0–100% product scale mapped to the device's discrete system volume steps;
- foreground-only live resync while the activity is active;
- hardware/system volume changes are reflected back into LouderMe;
- permission/boot failures are contained so UI state does not falsely claim the boost service is running.

### Windows
- real master volume through `AudioEndpointVolume.MasterVolumeLevelScalar`;
- 0–100% endpoint volume remains distinct from APO-backed 100–250% Global Boost;
- live resync detects external Windows volume changes and default playback-device changes;
- missing endpoint or Equalizer APO access failures are isolated from unrelated UI/settings state.

No cloud/backend dependency is introduced by Device Volume.
