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


## 2026-10-05 — LouderMe v0.1.2 published

### Validation
- Release-candidate branch CI: `37304511063` — **success**.
- Main CI/release run: `37304856564` — **success**.
- Current-HEAD unit tests: passed.
- Android build: passed.
- Packaged application ID: `com.michelslab.louderme`.
- Release publication: passed.

### Release evidence
- Tag: `v0.1.2`.
- Release: **LouderMe v0.1.2**.
- APK: `LouderMe-v0.1.2-debug.apk`.
- APK size: 28,948,461 bytes.
- APK SHA-256: `5516c990541bf865189d8ae5be8f718d2edb278fca1bbaed277f559ca6c3e39a`.
- Checksum file published alongside the APK.

### Product state
The manual boost range now spans 100–250%.

The displayed dB figure is explicitly **target digital signal gain**, not a measurement of speaker/headphone sound-pressure level (dB SPL).

Target Samsung device evidence from v0.1.1 remains positive: Michel reported audible general/external-app amplification using the current session-0 engine path.

### Next
- evaluate high-gain quality at 225% and 250%;
- implement Mixer / EQ;
- add limiter/compression strategy for high-gain presets;
- continue Play Store production readiness.


## 2026-10-05 — v0.1.3 IG Cleaner visual family + real equalizer

### Request
LouderMe should visually resemble the latest IG Cleaner Pro and should already include the equalizer, updater and surrounding production infrastructure rather than being only a visual reskin.

### Source UI audit
The current IG Cleaner Pro visual system uses:
- near-black workspace background `#060910`;
- canvas/surfaces `#090E17`, `#0D1521`, `#111C2B`, `#162335`;
- cyan `#71D7FF`, blue `#5D9CFF`, gold `#EFBD62`;
- thin translucent borders;
- ~20 px rounded surfaces;
- compact monospace operational labels;
- command-bar / workspace / dashboard-card hierarchy.

### Visual implementation
LouderMe now translates that language into native Jetpack Compose:
- Audio Workspace command bar;
- Boost Center workspace header;
- large live-engine hero;
- quick boost cards;
- fine-tune panel;
- metric dashboard;
- integrated equalizer panel;
- engine diagnostics;
- Audio Lab next-module deck;
- matching About surface.

No WebView or copied HTML is used.

### Equalizer implementation
Added a real Android session-0 Equalizer path.

LouderMe exposes seven stable target bands:
60 / 150 / 400 / 1000 / 2500 / 6000 / 12000 Hz.

Because device Equalizer implementations can expose a different band count:
- each target frequency is mapped using Android's native band lookup;
- targets resolving to the same native band are grouped;
- requested gain is averaged for that native band;
- requests are clamped to the device-reported supported level range;
- native center-frequency mapping is surfaced in Diagnostics.

User-facing target EQ range: -10 to +10 dB.

Presets:
Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones.

Manual adjustment creates Custom.

EQ settings persist locally and apply immediately while Global Boost is active or on the next service start.

### Updater
The existing Google Play updater remains intact and is now visible directly on Home:
- automatic availability checks;
- current status;
- Check action;
- Install action when available;
- About retains the same controls.

### Version
Bumped to v0.1.3 / versionCode 4.

### Validation boundary
The boost path is already audibly validated on the target Samsung device.

The equalizer is a real connected effect, but must still pass compile/tests and then audible target-device validation before its Samsung behavior is considered proven.

### Current status
Release candidate implementation prepared. Pending CI.


## 2026-10-05 — LouderMe v0.1.3 validated and published

### Validation
- Release-candidate commit: `1451fdbc1fa995b25f419e27cd0f85d5499784bd`.
- Branch CI run `37384205448`: **success**.
- Branch validation executed unit tests, Android compilation, package verification, APK/checksum generation and artifact upload.
- Squash merge to main: `987d4ab3193c67df3c216c13af244ce4f84c48e2`.
- Main CI/release run `37384489316`: **success**.
- Main validation again compiled/tested current HEAD before publication.

### Release evidence
- Tag: `v0.1.3`.
- Release: **LouderMe v0.1.3**.
- APK: `LouderMe-v0.1.3-debug.apk`.
- APK size: 29,112,349 bytes.
- APK SHA-256: `d34028c20423245b26626c6f2383cddbefc690063ba8c76a4b3deb36170fad90`.
- Checksum file published alongside the APK.

### Delivered
- IG Cleaner-inspired native Audio Workspace.
- Home-visible updater status/actions.
- Real session-0 Equalizer integrated with the foreground audio service.
- Seven LouderMe target EQ bands with device-native mapping.
- Flat, Bass, Deep Bass, Dialogue, Treble, Speaker and Headphones presets.
- Custom EQ mode.
- Local EQ persistence.
- EQ diagnostics.
- 100–250% boost preserved.

### Validation boundary
Boost remains audibly validated on the target Samsung device.

The new EQ compiled and is genuinely connected to Android's Equalizer API, but its audible system-wide behavior on the target Samsung device is still pending direct device validation.

### Next
Install v0.1.3 on the target Samsung, enable Global Boost, switch between Flat / Bass / Dialogue / Treble, confirm audible tonal changes, and inspect EQ Diagnostics for ATTACHED/DEGRADED/UNSUPPORTED plus the native band mapping.


## 2026-10-05 — v0.1.4 direct/sideload automatic updater

### Problem
The v0.1.3 updater only used Google Play In-App Updates. The APK currently installed directly from GitHub is not owned by Google Play, so it cannot use that production Play channel.

The private LouderMe source repository also cannot be queried anonymously from a shipped APK without embedding a private credential, which is prohibited.

### Architecture decision
Create two distribution flavors:
- `play` — Google Play updater; no APK-install permission.
- `sideload` — Michel's Lab direct updater.

The direct updater reads a public manifest from the existing public release infrastructure while the application source stays private.

### Direct updater implementation
The sideload channel now:
1. checks the public manifest automatically;
2. compares versionCode;
3. downloads a newer APK;
4. verifies SHA-256;
5. verifies package ID;
6. verifies candidate versionCode;
7. verifies that candidate and installed LouderMe share an accepted signing certificate;
8. requests Android's per-source install permission when necessary;
9. streams the verified APK through PackageInstaller;
10. lets Android present mandatory user confirmation.

### Play policy separation
`REQUEST_INSTALL_PACKAGES` exists only in `src/sideload/AndroidManifest.xml`.

The Play flavor does not request it.

### Signing finding
The v0.1.3 and earlier GitHub APKs were hosted-CI debug builds. A runner debug key is not an acceptable long-term update identity.

v0.1.4 therefore creates a one-time stable sideload signing key in a private CI bootstrap artifact. The key must be transferred into secure CI secrets and never committed.

Because the currently installed v0.1.3 has a different signing identity, migration to stable v0.1.4 requires one uninstall/reinstall. Future stable sideload builds can update in place.

### Public feed
Bootstrap feed created:
`realmichelduarte/michel-s-life-releases/louderme/latest.json`

No LouderMe source code or private token is exposed by this feed.

### Version
v0.1.4 / versionCode 5.

### Status
Implementation prepared on `release/v0.1.4`. Pending CI validation and stable signing bootstrap.


## 2026-10-05 — v0.1.4 release-Lint failure and Fragment fix

### Failure
GitHub Actions run `37388581098` failed during the real dual-flavor release build.

The failure was not hidden or bypassed. Android Lint reported `InvalidFragmentVersionForActivityResult` for the two `registerForActivityResult` calls in `MainActivity` while assembling the release target.

### Root cause
The release configuration did not have a sufficiently modern explicit AndroidX Fragment runtime available for ActivityResult's release-Lint contract.

### Fix
Added the current stable AndroidX Fragment KTX dependency:

`androidx.fragment:fragment-ktx:1.9.1`

No lint baseline, suppression, or fatal-check disabling was introduced.

### Status
A fresh v0.1.4 branch validation is required. The stable sideload signing bootstrap remains blocked until the full build succeeds.


## 2026-10-05 — v0.1.4 direct auto-update architecture

### Problem
The existing updater worked only for Google Play-owned installations. LouderMe is currently being tested through direct GitHub APK installation, so the user expected the APK build itself to discover and install newer versions.

The private LouderMe repository cannot be queried anonymously from a shipped APK without embedding a private GitHub credential, which is prohibited.

Older GitHub releases were also CI debug-signed. Hosted debug signing is not a permanent release identity, so Android cannot guarantee in-place updates across future runners.

### Architecture
LouderMe now has two explicit distribution flavors:

- `play` — Google Play In-App Updates; no `REQUEST_INSTALL_PACKAGES`.
- `sideload` — Michel's Lab Direct updater; includes `REQUEST_INSTALL_PACKAGES`.

The direct channel:
- reads a public update manifest from the Michel's Lab release hub;
- auto-downloads newer versions;
- verifies SHA-256;
- verifies package ID;
- verifies manifest/APK versionCode;
- verifies signing-certificate continuity;
- installs through Android PackageInstaller;
- handles the Android unknown-sources permission flow;
- records PackageInstaller results.

### Public/private boundary
Source remains private.

The public update feed and signed APKs live in `realmichelduarte/michel-s-life-releases` under the LouderMe channel.

No private repository token is shipped in LouderMe.

### Signing migration
v0.1.4 establishes a dedicated stable sideload signing key.

Because v0.1.3 and earlier were debug builds, the installed old build must be uninstalled once before installing the stable v0.1.4 baseline.

After that one-time migration, same-key direct releases can update in place.

### CI safety
- validation builds both Play and sideload flavors;
- CI explicitly asserts Play does not request `REQUEST_INSTALL_PACKAGES`;
- CI explicitly asserts sideload does;
- one-time signing-key bootstrap is gated behind an explicit `[bootstrap-signing]` commit;
- normal main releases require stable key secrets;
- public-feed publication requires a dedicated cross-repo token;
- the signing key is never committed to source.

### Current status
Code and CI architecture prepared. Pending final branch validation, one-time signing bootstrap, secret installation and public v0.1.4 baseline publication.


## 2026-10-06 — Stable sideload signer bootstrap validated

### Validation
- Bootstrap workflow run: `37392415939`.
- Current-HEAD Android validation: **success**.
- Play + sideload flavor builds/tests: **success**.
- Play permission audit: **success** — no `REQUEST_INSTALL_PACKAGES`.
- Sideload permission audit: **success** — `REQUEST_INSTALL_PACKAGES` present.
- Stable sideload signing bootstrap: **success**.
- Signed bootstrap artifact: `LouderMe-v0.1.4-stable-sideload`.
- Private signing handoff artifact: `LouderMe-sideload-signing-material-PRIVATE`.
- Private handoff expires 2026-10-13 and must be transferred to GitHub Actions secrets plus a secure offline backup.

### Stable signing identity
- Certificate DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`.
- Certificate SHA-256: `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3`.
- RSA key size: 4096 bits.
- APK signature scheme verified: v3.
- Signed v0.1.4 sideload APK size: 22,100,564 bytes.
- Signed APK SHA-256: `7eee11c065330d0064378172840cdb5aa067c17463332db8a6c6626e674f9d83`.

### Security boundary
The private key/password material is not committed to Git and must never be copied into app source, documentation, logs, chat, or the public update feed.

### Remaining infrastructure gate
The GitHub connector cannot write Actions Secrets. Before merging v0.1.4 to `main`, the private handoff values must be added to the LouderMe repository as Actions secrets, and a fine-grained cross-repository token must be added for publishing the public Michel's Lab release feed.

### Migration
v0.1.3 and older direct APKs were CI-debug signed. Android cannot update those in-place to the new stable signer.

One-time migration:
1. uninstall the old direct/debug LouderMe;
2. install the stable signed v0.1.4 sideload baseline;
3. from v0.1.5 onward, same-signer direct updates can install in place after Android confirmation.


## 2026-10-06 — v0.1.4 final direct-update channel validated and released

### Final validation

The permanent Michel's Lab Direct update path is now operational.

- Final release-candidate build/sign run: `37424013605` — **success**.
- Squash merge to `main`: `ad1f3284ea8e39552ae0711a48f995ea7976f2c3`.
- Final `main` build/sign/private-release run: `37424364775` — **success**.
- Public binary publisher run: `37423737829` — **success**.
- Play and sideload unit/build validation: passed.
- Play permission audit: passed — no `REQUEST_INSTALL_PACKAGES`.
- Sideload permission audit: passed — install permission present.
- Stable sideload signing: passed.
- Private signing vault refresh: passed.
- Private v0.1.4 release publication: passed.
- Public v0.1.4 binary release publication: passed.
- Public `latest.json` update: passed.

### Authoritative stable signer

The final canonical Michel's Lab Direct signing identity is:

- DN: `CN=Michel's Lab, OU=Software, O=Michel's Lab, L=Saltillo, ST=Coahuila, C=MX`.
- certificate SHA-256: `e0d497f4872c116632f040e51e08b7beb410f0a3df4d2b959d22fbd7bde48479`.
- RSA: 4096-bit.
- APK signature scheme: v3.

**Supersession note:** earlier v0.1.4 development/bootstrap entries that recorded certificate SHA `4a5bb9d9456a656821c3c1105854bda17e24273fd0d09e9495edd5b01e783aa3` and bootstrap APK SHA `7eee11c065330d0064378172840cdb5aa067c17463332db8a6c6626e674f9d83` are provisional historical evidence and are **not** the final release identity. The `e0d497...` certificate above is authoritative.

### Release evidence

Private source-repo release:
- tag: `v0.1.4`;
- APK: `LouderMe-v0.1.4-sideload.apk`;
- size: 22,100,564 bytes;
- APK SHA-256: `b2687abd2242265abe7322426a22ba7069e58aecfe228b2206d79151f89331df`.

Public Michel's Lab Direct release:
- tag: `louderme-v0.1.4`;
- APK: `LouderMe-v0.1.4-sideload.apk`;
- size: 22,100,564 bytes;
- APK SHA-256: `7c4231e718fba60a74df0717a68f0f5ff0b9273b79194de501ffc41b4386958a`.

The two whole-file hashes differ because they were independent builds. Both APKs were verified against the same final canonical signing certificate, which is the property Android requires for update continuity.

### Public feed

`realmichelduarte/michel-s-life-releases/louderme/latest.json` now advertises:

- package: `com.michelslab.louderme`;
- versionName: `0.1.4`;
- versionCode: `5`;
- public APK URL;
- SHA-256 `7c4231e718fba60a74df0717a68f0f5ff0b9273b79194de501ffc41b4386958a`.

The LouderMe APK does not contain a private GitHub token.

### Signing continuity

Normal CI now preserves one stable signing identity.

The workflow prefers valid GitHub Actions signing Secrets but can recover from missing/stale Secrets through a private Actions signing-vault artifact. The vault is refreshed after successful signing and by a scheduled monthly run.

The stable signer is not regenerated during normal releases.

### Migration requirement

v0.1.3 and older direct builds are debug-signed and cannot update in place to the permanent signer.

Exactly one migration reinstall is required:

1. uninstall the old debug-signed LouderMe;
2. install the stable v0.1.4 Michel's Lab Direct APK;
3. from v0.1.5 onward, LouderMe can automatically discover/download/verify newer direct releases and Android can accept them as same-signer updates after the required system confirmation.

### Status

**v0.1.4 is released and the Michel's Lab Direct update channel is operational.**

No additional manual GitHub browsing should be necessary for normal same-signer sideload updates after the one-time v0.1.4 migration.


## 2026-10-06 — v0.1.5 high-gain peak protection candidate

### Goal
Improve quality and overload handling at 225–250% without replacing the Samsung-validated LoudnessEnhancer path.

### Platform finding
Android's LoudnessEnhancer already compresses samples that would otherwise exceed the platform sample range. DynamicsProcessing exposes a dedicated Limiter stage intended to protect the signal from overloading and distortion.

### Implementation
- Kept LoudnessEnhancer as the primary session-0 engine.
- Added `PeakProtectionPolicy` for the DynamicsProcessing fallback.
- Explicit limiter begins at 175% and becomes progressively stronger at 200%, 225% and 250%.
- Limiter configuration failure is non-fatal; the fallback continues and reports the condition in diagnostics.
- Added unit tests for policy thresholds, progression and supported-range clamping.
- Bumped candidate version to v0.1.5 / versionCode 6.

### Validation boundary
Code is prepared on `release/v0.1.5`. CI must pass before merge or release. Real-device listening remains required to judge tonal quality and audible limiting behavior.

### Current status
Release candidate prepared; pending CI.


## 2026-10-06 — Android status-bar collision fix

### Finding
A physical target-device screenshot showed the custom LouderMe Home command bar occupying the Android status-bar region. The LM mark, product title and About action visually collided with the clock, media/notification indicators and system icons. The About screen used the same unsafe custom-top-bar pattern.

### Root cause
The app intentionally renders edge-to-edge on modern Android, but its custom Compose top bars did not apply system safe-drawing insets. Material 3 `Scaffold` does not automatically make arbitrary custom top-bar content respect the status-bar/cutout inset.

### Action completed
- Applied `WindowInsets.safeDrawing` to the foreground content of the Home command bar.
- Limited the inset application to Top + Horizontal sides so the decorative bar background can remain edge-to-edge without adding irrelevant bottom navigation padding.
- Applied the same correction to the About top bar.
- Preserved the existing layout proportions and Michel's Lab visual design.
- Did not use a device-specific fixed dp/pixel workaround.

### Validation
- Source-level review confirms both custom top bars now consume safe-drawing top/horizontal insets before their ordinary visual padding.
- Android compilation/CI is the next automated gate.
- Final visual confirmation remains a physical-device check because the original defect was device-rendered.

### Current status
Implementation is on `main` in commit `ed524ce1fb5ba97499fe53229781a1f5cb2a0fb1`. Pending CI result and on-device screenshot confirmation.

## 2026-10-06 — GitHub Copilot agent delegation

Added repository-level Copilot instructions and custom App Maintainer, QA Regression and Release Manager agents. The contracts preserve Play/sideload separation, stable direct-update signing, SHA/package/version/signing checks, correct gain-vs-dB-SPL semantics, EQ mapping transparency, Android safe-area rules and explicit target-device validation gates.

Purpose: delegate bounded implementation, regression checks and release preparation to repository agents so cross-project ChatGPT work can focus on product decisions and coordination.

No audio behavior, version, signing material or release artifact changed in this infrastructure-only update.

## 2026-10-06 — ChatGPT-ready task intake

Added `.github/ISSUE_TEMPLATE/chatgpt-task.yml` so new implementation/audit tasks can capture the desired outcome, evidence, scope, acceptance criteria, required validation and release permission up front.

Purpose: reduce repeated context reconstruction in future ChatGPT sessions and make repository work resumable from a bounded GitHub Issue without changing product behavior.
\n

## 2026-10-06 — Official LouderMe product identity integration

### Source of truth
Consumed the official LouderMe branding from `realmichelduarte/Michel-Software-Standards`:
- approved concept: **Option 4 — flowing waveform**;
- `shared-assets/product-logos/louderme/official-app-icon.svg`;
- `shared-assets/product-logos/louderme/official-mark.svg`;
- `shared-assets/product-logos/louderme/official-lockup.svg`.

The canonical SVGs are copied into `app/src/main/assets/branding/louderme/` for provenance. Android runtime assets are local platform derivatives; the app does not hotlink the private standards repository.

### Implementation
- Added official launcher and round-launcher identity.
- Added adaptive Android launcher foreground/background assets.
- Added official branded launch/splash treatment.
- Replaced the temporary `LM` command-bar placeholder with the official waveform mark.
- Added the official waveform/wordmark/tagline treatment to the About hero.
- Preserved the existing IG Cleaner-family LouderMe layout and colors outside the requested branding surfaces.
- No audio, updater, permission, signing, distribution or cloud behavior was changed.
- No version bump or release publication was performed as part of this branding-only implementation.

### Validation
Pending pull-request CI for Android resource compilation, both distribution flavors, package IDs and permissions.

### Current status
Official LouderMe identity is implemented on the branding branch and awaiting CI validation before merge.


### Branding validation evidence — 2026-10-06
- PR #9 merged to `main` as `8b1aa2ff4efabbcbdc0b5db063cec0df4ae91898`.
- First PR CI run `37437629602` failed during Android resource linking because a literal color was incorrectly used as a layer-list `drawable` reference in `louderme_launch_background.xml`.
- Corrected the splash background to use an embedded shape/solid drawable.
- Second PR CI run `37437798285`: **success**.
- Unit tests: passed.
- Play debug build: passed.
- Sideload debug/release builds: passed.
- Package-ID and Play/sideload permission separation checks: passed.
- Validation artifacts uploaded successfully.
- No product version bump was made for this branding integration.


## 2026-10-06 — v0.1.6 stable branding release candidate

### Release authorization
Michel explicitly authorized a **stable release**, not a prerelease.

### Scope
v0.1.6 packages the already-merged official LouderMe branding:
- Option 4 — flowing waveform;
- official launcher/round launcher;
- Android adaptive icon;
- branded launch/splash;
- official command-bar mark;
- official About waveform/wordmark/tagline treatment.

### Version
- versionName: `0.1.6`
- versionCode: `7`

### Preserved behavior
No change to:
- 100–250% boost behavior;
- LoudnessEnhancer/DynamicsProcessing engine selection;
- EQ mapping or presets;
- updater verification;
- stable sideload signer;
- Play/sideload permission separation;
- local privacy behavior.

### Validation / release gate
The release branch must pass full Android CI. After merge to `main`, the stable signing job and private release publication must succeed. The signed artifact must then be propagated to the public Michel's Lab Direct release repository and `louderme/latest.json` must advertise versionCode 7 / versionName 0.1.6.

### Current status
Release candidate prepared; pending CI.


## 2026-10-06 — LouderMe v0.1.6 stable release published

### Authorization
Michel explicitly requested a **release**, not a prerelease.

### Validation
- Release PR: #10.
- Release-candidate CI run: `37497219727` — **success**.
- Merge to `main`: `1e8c29a99ce402fe24e2bdeba7dc4180771d0ff8`.
- Main build/sign/release run: `37497675432` — **success**.
- Unit tests: passed.
- Play build: passed.
- Sideload build: passed.
- Package ID verification: passed.
- Play/sideload permission separation: passed.
- Stable Michel's Lab Direct signing: passed.
- Private release publication: passed.
- Public publisher run: `37498269151` — **success**.
- Transfer-staging cleanup run: `37498345236` — correctly **skipped** by the cleanup guard.

### Release evidence
Private source release:
- tag: `v0.1.6`;
- release type: stable, not prerelease;
- APK: `LouderMe-v0.1.6-sideload.apk`;
- APK size: 22,130,370 bytes;
- APK SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

Public Michel's Lab Direct release:
- tag: `louderme-v0.1.6`;
- release type: stable, not prerelease;
- APK: `LouderMe-v0.1.6-sideload.apk`;
- APK size: 22,130,370 bytes;
- APK SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

### Public updater feed
`louderme/latest.json` now advertises:
- appId: `com.michelslab.louderme`;
- channel: `sideload`;
- versionName: `0.1.6`;
- versionCode: `7`;
- APK URL: public `louderme-v0.1.6` asset;
- SHA-256: `b0b0165b2b7b967e090083ae0b681d3bf2591eafbc8b9277129fc27cafc3a6e5`.

### Delivered branding
v0.1.6 is the first stable LouderMe release carrying the official Michel's Lab **Option 4 — flowing waveform** identity across launcher, adaptive icon, splash, command bar and About.

### Current status
**LouderMe v0.1.6 is fully released on the stable direct channel and discoverable by the in-app sideload updater.**


## 2026-10-06 — Cross-device startup + native Windows desktop foundation

### User request
- LouderMe Android and LouderMe Desktop must both offer opt-in startup with the device.
- LouderMe Desktop must be a real installable Windows application, not an HTML file.

### Android implementation
- Added an opt-in **Start with phone** preference.
- Added `RECEIVE_BOOT_COMPLETED`.
- Added `BootCompletedReceiver`.
- On Android boot, LouderMe resumes the previously active boost only when:
  - the user enabled start-on-boot;
  - boost was active before shutdown/reboot;
  - notification permission is available where Android requires it.
- The receiver starts the existing foreground audio service instead of attempting to force-open an Activity.
- Startup remains disabled by default.
- Manufacturer/OS background-start restrictions remain an Android platform boundary.

### Windows Desktop foundation
Created `desktop/` as a native Windows product surface:
- .NET 10 WinForms native application;
- NAudio 3.1.0 stable package for Windows Core Audio endpoint access;
- no browser/WebView desktop UI;
- default playback-device detection;
- real Windows master-volume control;
- mute control;
- native **Start LouderMe with Windows** setting via `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`;
- Inno Setup installer with optional startup and desktop-shortcut tasks;
- self-contained single-file portable EXE build;
- official LouderMe icon generated from the canonical Option 4 SVG;
- SHA-256 checksums for portable EXE and installer.

### Accuracy boundary
The current Desktop milestone does **not** claim system-wide gain above the normal Windows endpoint maximum. Windows master volume is 0–100. A true >100% system-wide boost needs a separately validated audio-processing path; the UI explicitly says so rather than relabeling endpoint volume as 250%.

### Versioning
- Android remains v0.1.6 / versionCode 7 on this feature branch.
- Desktop starts as v0.1.0.
- No release publication is authorized by this feature implementation.

### Validation
- Desktop Windows CI is required to prove .NET restore/build, self-contained EXE packaging, icon generation, Inno Setup installer creation and checksums.
- Android PR CI is required to prove the boot receiver/UI integration compiles in both Play and sideload flavors.
- Actual reboot/sign-in behavior remains a physical-device / Windows-session validation item.


### Final validation / merge evidence — 2026-10-06
- PR #12 merged to `main` as `8ef1e49ee40fd4ea565de31c4df9fc5bf5c16b27`.
- Android PR CI run `37512825591`: **success**.
  - Play + sideload tests/builds passed.
  - package IDs and permission split passed.
  - Android validation artifacts uploaded.
- Desktop PR CI run `37512825617`: **success**.
  - .NET 10 restore/build passed.
  - official Windows icon generation passed.
  - self-contained portable EXE publication passed.
  - native startup integration checks passed.
  - Inno Setup installer build passed.
  - portable + installer SHA-256 generation passed.
  - Windows artifact upload passed.
- Earlier desktop branch run `37512672065`: **success**, artifact `LouderMe-Desktop-v0.1.0`.
- Merge commit intentionally includes `[skip ci]` so this feature merge does not mutate or republish the already released Android v0.1.6 artifacts.
- No Desktop release has been published yet.


## 2026-10-06 — Windows Desktop real 100–250% system-wide boost

### Goal
Replace the Desktop placeholder boundary with a real Windows processing path for gain above the normal 0–100 endpoint volume range.

### Architecture decision
Windows endpoint master volume remains a separate 0–100 control through Core Audio.

LouderMe **Global Boost** now targets the Windows system-effects/APO path through Equalizer APO 1.4.2:
- 100–250% is converted with `20 * log10(percent / 100)`;
- 100% = 0.00 dB;
- 150% = +3.52 dB;
- 200% = +6.02 dB;
- 250% = +7.96 dB.

This is real post-mix digital gain, not a relabeled Windows volume slider and not an acoustic dB SPL claim.

### Equalizer
Desktop now exposes the same seven stable product-facing bands as Android:
- 60 Hz;
- 150 Hz;
- 400 Hz;
- 1 kHz;
- 2.5 kHz;
- 6 kHz;
- 12 kHz.

Presets: Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones and Custom.

### Configuration safety
- LouderMe writes only `LouderMe.txt`.
- It adds one `Include: LouderMe.txt` entry to Equalizer APO's main config.
- It creates `config.txt.louderme.bak` before the first main-config modification.
- The managed file explicitly resets `Device: all`, `Stage: post-mix` and `Channel: all` so inherited configuration state does not accidentally scope LouderMe to an unrelated device/channel.
- Existing user Equalizer APO filters are preserved.

### Third-party engine
- Supported engine: Equalizer APO 1.4.2 x64.
- LouderMe does not redistribute the installer.
- The UI opens the official SourceForge download page.
- Official x64 SHA-256 recorded in UI/docs: `7403be7427bbe1936a40dded082829b6e217fc4f5990fee5cba501f0ae055afa`.
- The user selects/attaches the intended playback endpoint in Equalizer APO Configurator.

### Compatibility / audio-quality boundary
- ASIO and WASAPI exclusive-mode streams can bypass Windows APO effects.
- Positive preamp consumes digital headroom; high gain can cause downstream Windows limiting/compression on loud material.
- Repository CI cannot prove endpoint attachment or audible quality.
- Physical Windows validation is still required at 100/150/200/250% and with each important output route.

### Privacy
Processing remains local. No account, telemetry, cloud service, audio upload or new secret was added.

### Version / release
Desktop remains v0.1.0 during this implementation. No Desktop release is published by this change unless separately authorized.
