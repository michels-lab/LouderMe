# LouderMe — Project Audit Log

Permanent chronological log for decisions, changes, tests, findings, regressions, pending work, and current status.

## 2026-10-05 — Project initialization

### Change / finding
The project was defined as an Android-first general audio booster, not merely a system volume control.

### Product decision
The primary goal is to increase the phone's general audio output so low-volume media can be heard more loudly. User-facing quick controls include 100%, 125%, 150%, 175%, and 200%. Mixer/EQ and predefined mixes are core secondary features. Smart Boost remains experimental and disabled by default.

### Action completed
- Product name selected: **LouderMe**.
- GitHub repository created as `realmichelduarte/LouderMe`.
- Android package fixed as `com.realmichelduarte.louderme`.
- README, proprietary LICENSE, permanent project audit log, architecture notes, project protocol, and About specification prepared.
- Initial Jetpack Compose shell created with boost-level controls, Mixer/EQ entry point, Smart Boost Beta entry point, and About screen.
- Initial version set to `0.1.0`.

### Validation
- Repository naming and package identity reviewed for consistency.
- About reads the version from `BuildConfig.VERSION_NAME` rather than a hard-coded production version.
- No claim of working global amplification has been made.

### Pending
- Prove reliable global audio processing on the target Android/Samsung device.
- Determine the supported system-wide audio-effect path on the target Android version.
- Implement boost engine with limiter/protection.
- Connect 100–200% quick controls to the real audio engine.
- Implement fine boost control, Mixer/EQ, preset persistence, and preset library.
- Implement Smart Boost Beta only after the manual/global path is stable.
- Add automated tests and device validation checklist.
- Produce an installable APK after the audio engine reaches a testable milestone.

### Current status
Foundation release `v0.1.0` prepared. UI shell and project protocol are established; the core global audio engine remains unimplemented and unvalidated.

## 2026-10-05 — Initial release CI setup

### Change / finding
The first two automated release attempts failed during Android SDK environment setup before the app build started.

### Evidence
- Run 1 failed because `android-actions/setup-android@v3` attempted to install the obsolete SDK package `tools`.
- Run 2 failed because the runner's preinstalled `sdkmanager` executable was not exposed on `PATH`.

### Action completed
- Removed the failing `setup-android` dependency.
- Updated the workflow to call the runner's Android SDK manager through its explicit SDK path.
- Updated checkout and Java setup actions to current major versions where applicable.
- Kept release publication gated behind a successful APK build.

### Current status
CI workflow corrected and queued for another validation run. No release will be published unless the APK build succeeds.


## 2026-10-05 — AndroidX build fix

### Change / finding
The release workflow reached the actual Android build successfully, but Gradle stopped at `:app:checkDebugAarMetadata` because the project uses AndroidX dependencies while AndroidX support was not enabled in Gradle properties.

### Evidence
GitHub Actions reported: `android.useAndroidX` was not enabled while the runtime classpath contained AndroidX Compose, Activity, Lifecycle, and related dependencies.

### Action completed
- Added root `gradle.properties`.
- Enabled `android.useAndroidX=true`.
- Triggered a fresh `v0.1.0` validation build.

### Current status
Awaiting the new CI build. Release publication remains gated behind a successful APK build.


## 2026-10-05 — Foundation release v0.1.0 published

### Change / finding
After enabling AndroidX, the next validation build reached Kotlin compilation and reported inconsistent JVM targets: Java was targeting 1.8 while Kotlin was targeting 17.

### Action completed
- Added Java 17 source/target compatibility to the Android module.
- Added Kotlin JVM toolchain 17.
- Re-ran the complete GitHub Actions build.
- Android debug APK compiled successfully.
- Release asset was prepared and uploaded.
- GitHub release `v0.1.0` was published successfully.

### Validation / evidence
- GitHub Actions run: `37291824472`.
- `:app:assembleDebug`: successful.
- Release tag: `v0.1.0`.
- Release title: **LouderMe v0.1.0 — Foundation**.
- APK asset: `LouderMe-v0.1.0-debug.apk`.
- APK size: 28,042,227 bytes.
- APK SHA-256: `42b5e15ca3e7c5e4b0b7b7e7ed7b6ac13eb166b6bc6b224564269dae1ae1600a`.
- Release page: https://github.com/realmichelduarte/LouderMe/releases/tag/v0.1.0

### Current status
The LouderMe foundation now builds successfully and has an installable debug APK release. The global/system-wide amplification engine remains intentionally unimplemented and unvalidated; implementing and testing that engine is the next product milestone.


## 2026-10-05 — v0.1.1 infrastructure and real audio-engine candidate

### Audit finding
A cross-app audit found that updater work had left LouderMe internally inconsistent: build metadata was already 0.1.1, MainActivity expected updater-aware UI, but the Compose UI was still the 0.1.0 signature. The existing workflow also reported green after v0.1.0 existed because it skipped the actual Android build.

### Decisions
- Michel's Lab is now the umbrella product identity.
- Android package migrated before Play publication to `com.michelslab.louderme`.
- Current-commit build validation and release publication are separate CI gates.
- Google Play is the production automatic-update channel.
- Global audio uses session 0 only as an experimental compatibility path because Android deprecates global insert effects on session 0.

### Implementation prepared
- Michel's Lab native About with canonical portrait and official links.
- Google Play update status/actions.
- User-started special-use foreground service.
- LoudnessEnhancer session-0 engine with DynamicsProcessing fallback.
- 100–200% quick levels and fine slider connected to engine requests.
- Engine/output-route diagnostics.
- Unit tests for gain mapping.
- Privacy and Play-readiness documentation.
- New CI that always builds/tests HEAD and publishes only after validation.

### Validation boundary
No claim of reliable system-wide boost is permitted until a physical target-device A/B test confirms external-app audio changes.

### Next
Run branch CI, fix compilation/test issues until green, merge to main, publish v0.1.1, then perform S26 Ultra audio validation.


## 2026-10-05 — LouderMe v0.1.1 validated and published

### Validation
- Release-candidate branch commit: `119b3a4982c411f3304c60e3163129d233cb0dd9`.
- Branch CI run `37299633996`: **success**.
- Branch validation actually executed unit tests, Android compilation, APK package-ID verification, checksum preparation, and artifact upload.
- Squash merge to main: `aec0f7c38923c237f12429c16e8b994622565dd1`.
- Main CI/release run `37299949758`: **success**.
- Main build again executed the full current-HEAD validation before publication.

### Release evidence
- Tag: `v0.1.1`.
- Release: **LouderMe v0.1.1**.
- APK: `LouderMe-v0.1.1-debug.apk`.
- APK size: 28,932,073 bytes.
- APK SHA-256: `e57e9589dc2cae76efaa1c4f697ec3da5a7a917563f2b3edbedebaaddad3102a`.
- Checksum file published alongside the APK.
- Package verified by CI: `com.michelslab.louderme`.

### Current status
The app now has a compiled experimental global-audio compatibility engine and production-oriented update infrastructure. **System-wide amplification is not yet considered validated** because Android deprecates global insert effects on session 0 and the target Samsung device still needs an external-app A/B test.

### Next device test
Install v0.1.1 on the target Samsung phone, play the same Spotify/YouTube passage at a fixed system volume, compare 100% vs 150% vs 200%, and report the in-app engine status/diagnostic implementation. That evidence determines whether session 0 is genuinely useful on the device or a different architecture is required.

## 2026-10-05 — Infrastructure / cloud audit

Added `docs/INFRASTRUCTURE_AUDIT.md`.

LouderMe's core audio path remains fully local. No Supabase/Google backend is required for v0.1.1. The important infrastructure gates are physical audio validation, Play production delivery/update validation and later persistence of mixer/EQ presets. Cloud sync becomes relevant only if account-based preset/profile features are deliberately added.


## 2026-10-05 — Target-device validation and 250% range

### Device validation
Michel reported that LouderMe v0.1.1 audibly increases general/external-app audio on the target Samsung device using the session-0 compatibility engine.

This validates the current path for the intended device, not universally across Android hardware.

### Product decision
Extend manual boost from 200% to **250%**.

New quick levels:
- 225% = +7.04 dB target signal gain.
- 250% = +7.96 dB target signal gain.

### Technical clarification
The displayed dB value is **digital target signal gain**, derived from the amplitude ratio:

`G_dB = 20 log10(A2/A1)`

It is not a measurement or prediction of loudspeaker sound-pressure level (dB SPL). Acoustic output depends on the output device, amplifier/headroom, vendor DSP, limiter/compression behavior, source material, frequency response, and listening geometry.

### Implementation
- All internal boost clamps raised to 250%.
- Slider extended to 250%.
- 225% and 250% quick controls added.
- Quick controls reflowed into two rows.
- UI wording changed to "target signal gain".
- Unit tests extended through 250%.
- Version bumped to v0.1.2 / versionCode 3.

### Status
Pending CI validation and release publication.

## 2026-10-05 — Michel's Lab parent/child governance contract

Added the repository-level Michel's Lab governance declaration:

- `.michelslab/project.yml` identifies `realmichelduarte/Michel-Software-Standards` as the shared standards authority.
- `MICHELS_LAB_PROJECT.md` documents the human-readable reporting contract.
- App-specific implementation evidence remains in this repository.
- Reusable/cross-app decisions are promoted to the master standards repository.
- The master repository polls child status centrally; this repository receives no credential that can write to the master.
- Secret values remain prohibited from both repositories.
