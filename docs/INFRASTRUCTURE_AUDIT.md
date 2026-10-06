# LouderMe — Infrastructure & External Services Audit

Last reviewed: **2026-10-06**

## Current architecture
- Android-native application.
- Native Windows desktop application (.NET 10 WinForms).
- Windows system-wide boost above 100% and desktop EQ use a local Windows APO effects backend through Equalizer APO 1.4.2.
- Core audio processing is local/on-device.
- Production update path: Google Play In-App Updates.
- GitHub is used for source, CI and test/direct APK releases.
- No cloud database or account backend is required for the current product.

## Verified strengths
- Package namespace is `com.michelslab.louderme`.
- v0.1.1 build/test CI validates current HEAD before publishing.
- Checksum is published for direct APK test releases.
- Play updater dependencies are present.
- No reason exists to put audio-engine operation behind a cloud dependency.

## Gaps / required follow-up
1. Physical S26 Ultra A/B validation of 100% / 150% / 200% output remains the primary technical gate.
2. Complete production Play Console listing, signing and Play-delivered validation.
3. Validate In-App Updates from an actual Play-installed build.
4. Finish/validate mixer, EQ, preset persistence and product behavior after the audio engine path is proven.
5. Keep privacy/store disclosures aligned with any future permissions or data collection.

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
