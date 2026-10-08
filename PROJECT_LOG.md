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


### Final Windows global-boost validation — 2026-10-06
- PR #13: **merged**.
- Merge commit: `443c8cd09af593bd15d9dc7d46d2cd43b8044a18`.
- Desktop PR CI run `37515489882`: **success**.
  - .NET 10 native build passed.
  - real-global-boost source contract passed.
  - 100–250% gain formula contract passed.
  - APO managed configuration contract passed.
  - portable self-contained EXE passed.
  - Inno Setup installer passed.
  - EXE/installer checksums passed.
  - Windows artifact upload passed.
- Android regression CI run `37515490245`: **success**.
  - unit/build validation passed;
  - Play and sideload builds passed;
  - package ID and permission separation passed.
- Additional branch Windows run `37515460340`: **success**.
- Merge used `[skip ci]` intentionally to avoid re-running the already released Android v0.1.6 publication path for a Desktop-only feature.
- Desktop version remains v0.1.0 and is **not yet released**.
- Physical Windows endpoint/APO attachment and audible 100/150/200/250% validation remain required before claiming hardware-level validation.


## 2026-10-06 — LouderMe Desktop v0.1.0 stable release candidate

### Authorization
Michel explicitly authorized publication of the first LouderMe Desktop release.

### Release
- product: LouderMe Desktop;
- version: `0.1.0`;
- channel: stable;
- release type: normal release, not prerelease;
- source tag: `desktop-v0.1.0`;
- public distribution tag: `louderme-desktop-v0.1.0`.

### Planned assets
- `LouderMe-Setup-v0.1.0.exe`;
- `LouderMe-Setup-v0.1.0.exe.sha256`;
- `LouderMe-v0.1.0.exe`;
- `LouderMe-v0.1.0.exe.sha256`.

### Release gate
Publication requires a build from the exact merged `main` commit:
- .NET 10 restore/build;
- real 100–250% global-boost contract;
- Equalizer APO managed-config contract;
- self-contained portable EXE;
- Inno Setup installer;
- SHA-256 files.

### Distribution
Public Windows binaries will be published in `realmichelduarte/michel-s-life-releases` under a Desktop-specific tag/feed, separate from LouderMe Android.

### Known release boundary
- Equalizer APO 1.4.2 x64 remains a local prerequisite for system-wide boost above 100%.
- Physical Windows/APO audible validation is still pending.
- Windows Authenticode publisher signing is not configured for v0.1.0; published SHA-256 files verify file integrity, but SmartScreen may warn.

### Current status
Stable release candidate prepared; pending PR CI, merge-to-main release build, public transfer verification and final publication.


## 2026-10-06 — LouderMe Desktop v0.1.0 stable release published

### Authorization
Michel explicitly authorized publication of the first LouderMe Desktop stable release.

### Source release
- tag: `desktop-v0.1.0`;
- release type: stable, not prerelease;
- target commit: `dbd775e5fc492a12dbfbb2bbcd931a9bd397a958`;
- release workflow run: `37519498262` — **success**;
- Desktop build workflow run: `37519498307` — **success**;
- Android regression workflow run: `37519498193` — **success**.

Assets:
- `LouderMe-Setup-v0.1.0.exe` — 36,163,803 bytes — SHA-256 `958a4e9545e20b7654b10331683958d9c8cd71ae64a1ef7b1492f2c1225a4a0f`;
- `LouderMe-v0.1.0.exe` — 118,431,673 bytes — SHA-256 `0e1832b282c5daa3c45e828ba5dcdab53dea3326b03a62c80a4422efccf499a8`;
- matching checksum files published.

### Public Michel's Lab Windows channel
Public release:
- repository: `realmichelduarte/michel-s-life-releases`;
- tag: `louderme-desktop-v0.1.0`;
- release type: stable, not prerelease;
- publisher workflow run: `37519908247` — **success**.

Public feed:
- `louderme-desktop/latest.json`;
- channel: `stable`;
- platform: `windows-x64`;
- versionName: `0.1.0`;
- installer SHA-256: `958a4e9545e20b7654b10331683958d9c8cd71ae64a1ef7b1492f2c1225a4a0f`;
- portable SHA-256: `0e1832b282c5daa3c45e828ba5dcdab53dea3326b03a62c80a4422efccf499a8`.

Transfer staging was removed after publication.

### Release contents
- native .NET 10 WinForms app;
- official LouderMe identity;
- Windows endpoint device/volume/mute control;
- opt-in Start LouderMe with Windows;
- real APO-backed Global Boost 100–250%;
- 7-band EQ and presets;
- Inno Setup installer;
- self-contained portable EXE;
- SHA-256 integrity files.

### Known release boundary
- Equalizer APO 1.4.2 x64 is required for system-wide gain above 100%;
- physical Windows endpoint/APO audible validation remains pending;
- Windows Authenticode publisher signing is not configured for v0.1.0, so SmartScreen can warn even when hashes match.

### Current status
**LouderMe Desktop v0.1.0 is fully published on the stable public Windows channel.**

## 2026-10-06 — Intelligent Michel's Lab brand-adoption guidance

Repository instructions now explicitly route logo, launcher, splash/startup and About work through the Michel-Software-Standards Product Identity Standard and Brand Adoption Playbook.

The required interpretation is structural integration rather than sticker placement: replace active legacy identity, adapt canonical geometry to the existing product design language, preserve unrelated behavior, validate the build, and keep release publication separate unless explicitly authorized.



## 2026-10-06 — Complete cross-platform LouderMe brand integration

### Audit trigger
Michel requested a direct audit of what still remained to implement in the real LouderMe product, especially branding.

The audit found that initial official-logo adoption was real but the newer Michel's Lab integrated-branding contract was not yet fully satisfied.

### Key findings
Android already had:
- official launcher/adaptive icon;
- official branded splash;
- official LouderMe command-bar mark;
- official LouderMe About hero.

But Android still had:
- a legacy Michel Duarte portrait instead of the current canonical portrait;
- text-only social rows plus arrow;
- no Facebook row from the canonical developer profile;
- Michel's Lab referenced only as text rather than its official parent-brand mark/lockup;
- mixed tagline language;
- mostly generic Boost/EQ visual feedback rather than waveform-derived product language.

Windows Desktop still had:
- official executable icon but a text-only in-app header;
- no branded startup/splash surface;
- a plain MessageBox About;
- no canonical portrait / Michel's Lab parent-brand asset in About;
- no canonical social hierarchy;
- no in-app stable updater despite the public Desktop feed already existing;
- little waveform-derived visual language inside Boost/EQ.

### Canonical asset reconciliation
Vendored the current master assets from `realmichelduarte/Michel-Software-Standards`:
- canonical Michel Duarte portrait blob: `be4d18572bec28d53783cd4db05cb6cd289a7916`;
- Michel's Lab mark blob: `f205c15c3b6bd7fe676ced83fd1ea2ae24c25586`;
- Michel's Lab lockup blob: `7819ef5c7d1a5c338c67c9bbd517e5448724a5cf`.

The Android portrait previously in use was legacy blob `1594e609850dd1632ede85306bc9d7fc719a3bff` and was replaced.

### Android implementation
- Added local canonical developer-profile constants sourced from the master profile.
- About now uses the canonical Michel Duarte portrait.
- Added official Michel's Lab lockup to the author/studio block.
- Social order is now Instagram → Facebook → LinkedIn → GitHub → Email.
- Social entries use native recognizable glyphs plus visible network names.
- Reconciled About tagline to the official `SOUND THAT LIFTS YOU` identity.
- Product version is visible in the product hero.
- Added canonical-waveform visual feedback to Global Boost.
- Added waveform-derived EQ curve visualization.
- Preserved existing audio/update/startup behavior.

### Windows Desktop implementation
- Added exact canonical-waveform geometry as a native GDI+ control.
- Replaced the text-only top identity with LouderMe waveform + product name + official tagline.
- Added waveform-derived live boost visualization.
- Added EQ curve visualization derived from the same audio/waveform language.
- Added a native branded startup splash.
- Added a dedicated native About experience replacing the MessageBox.
- Desktop About now includes:
  - product-first LouderMe identity;
  - build-derived Desktop version;
  - canonical Michel Duarte portrait;
  - official Michel's Lab lockup;
  - About the Author section;
  - Instagram / Facebook / LinkedIn / GitHub / Email rows with native recognizable glyphs + visible names;
  - privacy/legal copy;
  - Equalizer APO third-party notice;
  - explicit Authenticode/SmartScreen state.
- Embedded the canonical portrait and Michel's Lab brand assets inside the Windows executable.
- Aligned Inno Setup publisher/support/update metadata with Michel's Lab.

### Desktop stable updater
Connected the already-established public Windows stable feed:
`louderme-desktop/latest.json`.

Updater behavior:
1. compare running product version with the public stable manifest;
2. expose installation only for a newer version;
3. download the public installer;
4. verify the manifest SHA-256;
5. only then ask Windows to launch the verified installer.

No silent update execution was added.

### Authenticode boundary
This branding/product pass does **not** claim Authenticode signing.
No Windows publisher certificate currently exists in the repository release path.
The UI/About explicitly preserves the accurate state: SHA-256 integrity is supported; SmartScreen can still warn.

### CI hardening
Android CI now checks:
- canonical portrait Git blob;
- canonical Michel's Lab mark/lockup Git blobs;
- Facebook presence;
- parent-brand lockup presence;
- official tagline;
- waveform Boost/EQ components.

Desktop CI now checks:
- canonical author/studio asset blobs;
- dedicated About surface;
- full social hierarchy;
- branded splash;
- waveform UI control;
- stable updater feed and SHA-256 verification contract.

The Desktop release workflow is also protected by the same branding/updater contract.

### Intermediate Desktop CI failures
Several branch pushes failed while the new native Windows surfaces were being assembled. These were not treated as release evidence.
Observed/fixed issues included:
- About color field colliding with `Form.Text`;
- invalid null-coalescing `Process.Start` statement;
- WinForms designer-serialization diagnostics on runtime-only custom-control properties;
- embedded-image lifetime handling.

A final green current-HEAD run is required before merge.

### Release discipline
- Android remains v0.1.6 / versionCode 7.
- Desktop remains v0.1.0 in source.
- No new release/version bump is authorized by this branding implementation.
- A future release must bump to a new product version and cannot overwrite the already-published Desktop v0.1.0 release.


### Final integrated-branding validation / merge — 2026-10-06
- PR #17 **merged**.
- Merge commit: `ab0b127c3b27ba640724e0abb5125db7195a1d49`.
- Final Desktop PR CI run `37540074990`: **success**.
  - native .NET build passed;
  - canonical asset/About/splash/updater contract passed;
  - portable EXE passed;
  - installer passed;
  - SHA-256 generation passed;
  - artifact upload passed.
- Final Android PR CI run `37540075001`: **success**.
  - canonical branding contract passed;
  - Play + Direct unit/build validation passed;
  - package IDs and permission split passed;
  - validation artifacts uploaded.
- Android intermediate AAPT2 failure was traced to compiling the canonical Michel's Lab PNG from `res/drawable`.
  - Resolution: preserve the exact canonical PNG bytes under `app/src/main/assets/branding/michels-lab/` and decode them natively from Compose.
  - This keeps the master asset unchanged while avoiding AAPT2 raster compilation.
- Desktop release workflow trigger was hardened so editing the release workflow cannot republish an already-released version; stable publication requires an explicit `desktop/releases/vX.Y.Z.md` release note or manual dispatch.
- Merge used `[skip ci]` intentionally because this product/branding change is **not** a release and must not republish Android v0.1.6 or Desktop v0.1.0.

### Current product state after merge
- Android source: integrated LouderMe + Michel's Lab branding complete under the current contract.
- Desktop source: integrated branding, native About/splash, waveform-derived UI and verified stable-feed updater implemented.
- Existing public binaries remain unchanged:
  - Android stable: v0.1.6;
  - Desktop stable: v0.1.0.
- A new Desktop/Android release requires separate explicit authorization and a new version; the already-published Desktop v0.1.0 must not be overwritten.
- Windows Authenticode remains unimplemented because no publisher certificate has been configured.


## 2026-10-06 — Agent contracts reconciled with dual-platform product state

After LouderMe branding/Desktop work landed, the repository-local agent prompts still described LouderMe primarily as Android-only.

Updated the local contract surfaces to current product truth:
- `AGENTS.md` now treats LouderMe as Android + native Windows Desktop;
- App Maintainer covers Compose/Gradle and .NET/APO/Inno Setup/updater work;
- QA Regression covers Windows endpoint/APO/config/installer/feed/SmartScreen boundaries in addition to Android;
- Release Manager covers Android Play/Direct plus Desktop source/public tags, checksums, public feed and Authenticode truthfulness;
- Copilot instructions now preserve both Android and Windows engine/update boundaries;
- release wording is stable-by-default when Michel explicitly says "release" and does not request prerelease/beta/RC.

The canonical Michel's Lab managed contract block was preserved unchanged.

No product code, version metadata, release artifact or public feed was changed by this agent-contract synchronization.


## 2026-10-06 — Android v0.1.7 + Desktop v0.1.1 stable release candidate

### Authorization
Michel explicitly authorized publication of the next LouderMe release after the integrated branding implementation.

### Versions
- Android: `0.1.7` / versionCode `8`.
- Windows Desktop: `0.1.1`.
- Release type: **stable**, not prerelease.

### Included Android changes
- integrated About/product branding;
- current canonical Michel Duarte portrait;
- official Michel's Lab parent-brand lockup;
- canonical social rows including Facebook;
- waveform-derived Boost/EQ visual language;
- opt-in Start with phone;
- existing audio engine, EQ, Play/Direct updater and stable signing preserved.

### Included Desktop changes
- native LouderMe waveform header and branded splash;
- dedicated About surface with canonical author/studio/social hierarchy;
- waveform-derived Boost/EQ visuals;
- stable-feed in-app updater with installer SHA-256 verification;
- existing APO-backed Global Boost, EQ, startup and installer behavior preserved.

### Release gates
Candidate must pass PR validation before merge:
- Android current-commit unit/build validation for Play + Direct;
- Android branding identity contract;
- package ID and permission split;
- Desktop native .NET build;
- Desktop branding/About/splash/updater contract;
- APO boost contract;
- portable EXE + Inno Setup installer + checksums.

### Publication plan
After green PR merge to `main`:
- Android workflow publishes private source tag `v0.1.7` and stable-signed APK.
- Desktop workflow publishes private source tag `desktop-v0.1.1`.
- validated artifacts are transferred to the public Michel's Lab release repo;
- public tags become `louderme-v0.1.7` and `louderme-desktop-v0.1.1`;
- public feeds `louderme/latest.json` and `louderme-desktop/latest.json` are advanced only after checksum verification.

### Known boundaries
- Android audible high-gain/EQ and Play production delivery remain device/store validation items.
- Windows APO endpoint/audible validation remains a physical-machine item.
- Windows Authenticode publisher signing remains unconfigured.


## 2026-10-06 — Android v0.1.7 + Desktop v0.1.1 stable releases published

### Authorization
Michel explicitly authorized publication. Per repository release policy, both releases are **stable normal releases**, not prereleases.

### Release source
- PR #18 merged to `main`.
- Release target commit: `da196b542a3f2e0cd196120006c2d40c40ff6ae5`.
- Android version: `0.1.7` / versionCode `8`.
- Desktop version: `0.1.1`.

### Candidate validation
- Android PR CI run `37544585036`: **success**.
  - branding identity contract passed;
  - Play + Direct unit/build validation passed;
  - package ID and permission separation passed;
  - validation artifacts uploaded.
- Desktop PR CI run `37544585024`: **success**.
  - native .NET build passed;
  - real-global-boost contract passed;
  - portable EXE passed;
  - Inno Setup installer passed;
  - checksums passed.

### Android source release
- Main Android release run `37544958547`: **success**.
- Build and validate: success.
- Stable Michel's Lab Direct signing: success.
- Private source release publication: success.
- Tag: `v0.1.7`.
- Release type: stable / `prerelease=false`.
- APK: `LouderMe-v0.1.7-sideload.apk`.
- APK size: 22,118,266 bytes.
- APK SHA-256: `ec61f53a8e80fa5f9b5cbda944ef4fa27b99a299508425981e24f6ef659d9df9`.

### Android public Direct channel
- Public publisher run `37545535455`: **success**.
- Public tag: `louderme-v0.1.7`.
- Release type: stable / not prerelease.
- Public APK SHA-256: `ec61f53a8e80fa5f9b5cbda944ef4fa27b99a299508425981e24f6ef659d9df9`.
- `louderme/latest.json` now advertises:
  - versionName `0.1.7`;
  - versionCode `8`;
  - the v0.1.7 public APK URL;
  - the matching SHA-256.
- Transfer staging cleanup run `37545641762`: **skipped as intended** by the cleanup guard.

### Desktop source release
- Desktop release workflow run `37544958566`: **success**.
- Independent Windows build run `37544958503`: **success**.
- Tag: `desktop-v0.1.1`.
- Release type: stable / `prerelease=false`.
- Installer: `LouderMe-Setup-v0.1.1.exe`.
  - size: 36,206,793 bytes;
  - SHA-256: `6175aef2f06c8563ccd0273acc9204f376d46112c2ec5008b6012e56d18dcb38`.
- Portable: `LouderMe-v0.1.1.exe`.
  - size: 118,505,401 bytes;
  - SHA-256: `733c1a2b270cc0e3737763eb0a71715edbdc90baca3736f6e27a1fe02196ed53`.

### Desktop public channel
- Public publisher run `37545285435`: **success**.
- Public tag: `louderme-desktop-v0.1.1`.
- Release type: stable / not prerelease.
- Public hashes match the private source release:
  - installer: `6175aef2f06c8563ccd0273acc9204f376d46112c2ec5008b6012e56d18dcb38`;
  - portable: `733c1a2b270cc0e3737763eb0a71715edbdc90baca3736f6e27a1fe02196ed53`.
- `louderme-desktop/latest.json` now advertises stable `0.1.1` with installer + portable URLs and matching SHA-256 values.
- Transfer staging cleanup run `37545431086`: **skipped as intended** by the cleanup guard.

### Delivered product changes
Android v0.1.7:
- complete integrated LouderMe / Michel's Lab branding;
- current canonical Michel Duarte portrait;
- official Michel's Lab lockup;
- complete canonical social hierarchy including Facebook;
- waveform-derived Boost/EQ visuals;
- opt-in Start with phone;
- existing 100–250% boost, EQ, Play/Direct split and updater preserved.

Desktop v0.1.1:
- native LouderMe waveform header and branded splash;
- dedicated About hierarchy;
- canonical author/studio/social identity;
- waveform-derived Boost/EQ visuals;
- stable-channel in-app updater with SHA-256 installer verification;
- existing APO boost, EQ, Start with Windows and installer behavior preserved.

### Known release boundaries
- Android audible high-gain/EQ quality and Google Play production delivery remain device/store validation items.
- Windows APO endpoint attachment/audible behavior remains a physical-machine validation item.
- Windows Authenticode publisher signing is still not configured, so SmartScreen can warn.

### Current status
**Android v0.1.7 and LouderMe Desktop v0.1.1 are fully published on their stable public Michel's Lab channels.**


## 2026-10-06 — LIMÓN handoff after v0.1.7 / Desktop v0.1.1 field feedback

### Production regression — Android About
User reported that the public Android **v0.1.7 / versionCode 8** app closes/crashes when opening About.

Evidence/state:
- issue created: **#19 — Android v0.1.7 crashes when opening About**;
- repository CI for v0.1.7 had passed, so the failure is a runtime/device regression not caught by the current build/static checks;
- root cause is **not yet confirmed**;
- likely investigation surface is the new About/branding runtime path: `LouderMeApp.kt`, `BrandComponents.kt`, canonical Michel's Lab raster asset loading and target-device rendering;
- next session must reproduce on the target Samsung and capture the actual stack trace/logcat before declaring a fix;
- do not overwrite v0.1.7; any production correction requires a new stable Android version.

### Desktop installer experience feedback
User expected LouderMe Desktop to install like FoamLens / Michel's Life and reported that the current public v0.1.1 flow does not communicate that clearly enough.

Audit finding:
- FoamLens and Michel's Life also use GitHub-built Windows releases with Inno Setup installers;
- Michel's Life additionally demonstrates optional Authenticode signing through `WINDOWS_CERT_BASE64` / `WINDOWS_CERT_PASSWORD` and `signtool`;
- LouderMe v0.1.1 does have an Inno Setup installer, but the public release also exposes a generically named portable EXE, making the normal install path ambiguous;
- LouderMe is currently unsigned with Authenticode, so Windows can show **Unknown publisher** before the installer wizard. This cannot be honestly removed without a real publisher certificate.

### Desktop hotfix branch
Branch: `fix/desktop-installer-experience`
Current branch head at handoff: `84b8d5ec740938112796fec25ddca3a639f39493`

Changes already staged on that branch:
- Desktop source version bumped from 0.1.1 to **0.1.2**;
- `desktop/installer/LouderMe.iss` reworked toward the Michel's Lab / FoamLens / Michel's Life install pattern;
- bilingual English/Spanish installer messages added;
- installer-side Start with Windows option retained;
- branded installer wizard-image references added;
- `desktop/installer/prepare-branding.ps1` added to generate ICO + installer wizard BMP assets from canonical LouderMe branding;
- normal Desktop CI updated to generate installer branding, rename the portable artifact toward **LouderMe-Portable-vX.Y.Z.exe**, validate installer contract and smoke-test an actual install/uninstall;
- release workflow partially updated to use the branded installer assets and explicit portable naming.

### Desktop hotfix validation status
**Not validated yet.**
No PR has been opened and no CI result exists for the v0.1.2 installer branch at this handoff.

Before merge/release:
1. finish/review both Desktop workflows for consistent `LouderMe-Portable-vX.Y.Z.exe` naming;
2. ensure public publisher/feed logic also understands the new portable filename;
3. add/update v0.1.2 release notes/changelog;
4. run Windows PR CI;
5. confirm branded installer builds;
6. confirm CI installs `LouderMe.exe` into an isolated directory and uninstalls cleanly;
7. publish only after explicit stable release authorization.

### Shared standards finding
The Michel's Lab master repository already documents:
- Windows installer + portable + SHA-256;
- current-commit validation;
- installer smoke installation/uninstallation;
- public binary release repositories;
- automatic updater expectations.

However, it does **not yet state strongly enough** that the normal Windows production path should be built/published from GitHub Actions, that the installer is the recommended user-facing artifact, and that a portable build should be clearly secondary/named as portable.

A master-standard update was investigated but **not yet committed** at this LIMÓN stop.

### Stop state
Public versions remain:
- Android **v0.1.7** — contains the About crash regression;
- Desktop **v0.1.1** — stable/public, but installer UX/naming needs the v0.1.2 hotfix.

No hotfix release was published in this session after the field reports.


## 2026-10-06 — Desktop v0.1.2 installer hotfix validated in PR #20

### Scope
Continuation of the LIMÓN handoff for the Windows installer experience. No stable release was published by this work.

### Implemented
- Desktop candidate remains **v0.1.2** on `fix/desktop-installer-experience`.
- The normal Windows artifact is the branded Inno Setup installer: `LouderMe-Setup-vX.Y.Z.exe`.
- The standalone secondary artifact is explicitly named `LouderMe-Portable-vX.Y.Z.exe`.
- Installer wizard branding is generated from canonical LouderMe assets.
- English/Spanish installer messages and the optional **Start LouderMe with Windows** task are preserved.
- Normal Windows CI now performs a real silent install, verifies `LouderMe.exe` and `unins000.exe`, and uninstalls the test installation.
- The public Michel's Lab Desktop publisher was aligned to the explicit Portable filename while retaining a legacy-transfer fallback for older artifacts.
- The current public stable feed remains Desktop v0.1.1 until an explicitly authorized v0.1.2 publication.

### Verified evidence
PR: **#20 — Desktop v0.1.2 — installer-first Windows experience**

Head validated: `c687e45d8309e9b631387acdd6da91c222c5ceab`

- Windows PR CI run **37550569236**: **success**.
- Windows same-head push run **37550491583**: **success**; step **Smoke-test real Windows installation** passed.
- Android PR CI run **37550569270**: **success**, confirming the Desktop hotfix did not regress the Android Play/Direct build gates.

### Remaining release boundary
- PR #20 is intentionally left open/unmerged because merging `desktop/releases/v0.1.2.md` to `main` triggers the stable Desktop release workflow.
- Windows Authenticode remains unconfigured; SmartScreen/Unknown publisher can still appear.
- Physical Windows APO endpoint/audible behavior remains a real-machine validation item.

### Separate Android regression
Issue **#19** remains open for the Android v0.1.7 About crash. Source inspection confirms the canonical Michel's Lab lockup and Michel Duarte portrait resources are present; no runtime root cause has been proven without target-device stack-trace/logcat evidence. Do not claim this Android regression fixed yet.


## 2026-10-06 — Stable releases published: Desktop v0.1.2 + Android v0.1.8

### Release decision
Michel explicitly authorized publication after the canonical About portrait correction. The validated PR #20 was merged to `main` at commit `654635407ef3f63a5a00ed04db7e0fa1e92f3a5e`.

### Desktop v0.1.2
- Source release tag: `desktop-v0.1.2` — stable, not prerelease.
- Public release tag: `louderme-desktop-v0.1.2` — stable, not prerelease.
- Recommended installer: `LouderMe-Setup-v0.1.2.exe`.
- Secondary portable build: `LouderMe-Portable-v0.1.2.exe`.
- Both binaries publish matching SHA-256 files.
- Public `louderme-desktop/latest.json` now points to v0.1.2.
- Installer SHA-256: `a042c5471a4b733f8503e1a809b57025cca33f4ebc1e9f16fccac29e1db12d36`.
- Portable SHA-256: `ff4c465f2f48dfd2688ab67fbcd468ee04254ae457f3590f1e9d14b19ec07482`.
- Source release workflow `37555024781`: success, including real install/uninstall smoke test.
- Public publisher workflow `37555336629`: success.

### Android v0.1.8
- versionName: `0.1.8`; versionCode: `9`.
- v0.1.7 was not overwritten.
- Source release tag: `v0.1.8` — stable, not prerelease.
- Public release tag: `louderme-v0.1.8` — stable, not prerelease.
- Public APK: `LouderMe-v0.1.8-sideload.apk` with SHA-256 companion.
- Public `louderme/latest.json` now points to v0.1.8 / code 9.
- APK SHA-256: `6edd30284a6104c03cd3bd2c017f83ab318f01b8c8456bb7c804512d7936cb5f`.
- Source Android workflow `37555024818`: success.
- Public publisher workflow `37555426309`: success.

### Canonical About portrait correction
- Replaced rejected interim portrait blob `be4d18572bec28d53783cd4db05cb6cd289a7916` with canonical blob `18fe1a68722850c3d8f918dc0799f46ffeb6dbaf` in Android and Desktop.
- Canonical JPEG remains byte-for-byte unchanged; only render size was reduced (Android 78×104 dp; Desktop 96×128 px).
- CI now rejects portrait drift on both platforms.

### Android issue #19
The corrupt/rejected portrait was a confirmed About defect and a plausible contributor to the Android crash. Issue #19 remains open until v0.1.8 is opened on the target Samsung and About is confirmed not to crash. Do not claim the runtime crash conclusively fixed until that device validation occurs.

### Distribution state
Both public stable feeds and release assets were verified after publication. Temporary transfer-staging files were removed from the public binary repository after successful publishing.


## 2026-10-06 — Android v0.1.9 system Back hotfix + Desktop v0.1.3 FoamLens installer parity

### Android v0.1.9 / versionCode 10
- User validated on the target Samsung that v0.1.8 About opens normally and the in-app Back control returns to the main screen.
- A separate navigation defect remained: Android system/gesture Back from About exited the app.
- Added Compose `BackHandler(enabled = showAbout)` so system Back from About now performs `showAbout = false`.
- System Back from the LouderMe main screen remains unchanged and exits normally.
- Android CI now explicitly guards the About BackHandler contract.
- PR #21 merged at `e475940758a4b39a1957f2f4ba7f629ce349deed`.
- Source release tag `v0.1.9`: stable, not prerelease.
- Public release tag `louderme-v0.1.9`: stable, not prerelease.
- Public feed `louderme/latest.json` points to v0.1.9 / versionCode 10.
- Public APK SHA-256: `1c368d5c4c76d3622f1da132e335243cfa70e6ab8d4583c3b1f457ae8b328e64`.
- Source workflow `37556701800`: success.
- Public publisher workflow `37557109373`: success.
- Original issue #19 (v0.1.7 About closes/crashes on open) was closed after target-device confirmation that About now opens normally. The asset defect was confirmed, but the exact sole runtime cause was not asserted.

### Desktop v0.1.3
- User screenshot showed v0.1.2 Setup did not visually match the FoamLens installer family because LouderMe added custom `WizardImageFile` and `WizardSmallImageFile` branding.
- Compared directly with FoamLens `desktop/installer/FoamLens.iss`, which uses the clean modern Inno Setup wizard without custom wizard-banner images.
- Removed LouderMe's custom side/header wizard imagery while preserving:
  - `WizardStyle=modern`;
  - LouderMe icon, product name/version and Michel's Lab publisher identity;
  - English/Spanish installer text;
  - optional desktop shortcut;
  - optional Start LouderMe with Windows.
- Simplified installer branding generation to the official Windows ICO only.
- Windows CI now fails if `WizardImageFile` or `WizardSmallImageFile` is reintroduced.
- PR #22 was rebased cleanly after the Android v0.1.9 merge, then merged at `67a6477798777a1d56dd039ee469db0b7b1cc32c`.
- Source release tag `desktop-v0.1.3`: stable, not prerelease.
- Public release tag `louderme-desktop-v0.1.3`: stable, not prerelease.
- Recommended installer: `LouderMe-Setup-v0.1.3.exe`.
- Secondary portable: `LouderMe-Portable-v0.1.3.exe`.
- Public `louderme-desktop/latest.json` points to v0.1.3.
- Installer SHA-256: `00f1a2a0ed289d47a4c36cfc09412ef6e721fd7c197c9201e60a635616acac39`.
- Portable SHA-256: `b5cec6f3ba79687242becff4d7b81683f79e6e413c931dbc4612ccb25765167c`.
- Source release workflow `37557224444`: success, including real install/uninstall smoke test.
- Public publisher workflow `37557459967`: success.

### Distribution state
- Android v0.1.9 and Desktop v0.1.3 are both available in the public binary repository.
- Both public update feeds point to the new stable versions.
- Temporary transfer-staging files were removed after publication.


## 2026-10-06 — Desktop v0.1.3 installed-startup regression reproduced and fixed in v0.1.4 candidate

### Field report
- User installed public Desktop v0.1.3 successfully, but the application did not open afterward.

### CI gap discovered
- The prior Windows installer smoke test verified installation and uninstallation only.
- It did **not** launch the installed `LouderMe.exe`.
- This differed from FoamLens, whose Windows CI validates the packaged/installed executable at runtime.

### Reproduction
Branch: `fix/desktop-installed-startup`

Initial diagnostic CI run: **37558473562**
- installer build: success;
- install: success;
- installed EXE launch: **failure**;
- process exit code: `-532462766` / `0xE0434352` (.NET unhandled exception).

A temporary startup diagnostic layer was then added so the installed smoke test could print the real exception.

Diagnostic CI run: **37558716952**

Confirmed stack trace:
```
System.ArgumentException: Control does not support transparent background colors.
   at System.Windows.Forms.Control.set_BackColor(Color value)
   at LouderMeDesktop.WaveformMarkControl..ctor()
   at LouderMeDesktop.SplashForm..ctor()
   at LouderMeDesktop.Program.Main(String[] args)
```

### Root cause
Custom WinForms controls were assigning `BackColor = Color.Transparent` without first enabling `ControlStyles.SupportsTransparentBackColor`.

The unsafe pattern existed in four controls:
- `WaveformMarkControl`;
- `AudioPulseControl`;
- `EqCurveControl`;
- `SocialGlyphControl`.

### Fix
- All four controls now enable `SupportsTransparentBackColor` before assigning transparent backgrounds.
- Startup exceptions are logged to:
  `%LOCALAPPDATA%\Michel's Lab\LouderMe\startup-crash.log`
- Production startup failures show the diagnostic path instead of silently terminating.
- Windows build CI now performs: install → launch installed EXE → require it to stay alive → terminate → uninstall.
- Stable release CI now carries the same installed-runtime launch gate.
- CI statically guards that all four transparent custom controls keep the required WinForms style.

### v0.1.4 candidate validation
PR: **#24 — Desktop v0.1.4 — fix installed startup crash**

Validated head: `14d2482e898a1cd66014eb1659c3ff9334b4fa54`

- Windows PR CI run **37559243120**: **success**, including installed executable startup.
- Android PR CI run **37559243046**: **success**.
- Same-head Android push CI run **37559235544**: **success**.
- PR #24 state: mergeable / clean.

### Release boundary
Desktop v0.1.4 is prepared and validated but **not yet published** in this entry. Merge/publication still requires explicit stable release authorization.


## 2026-10-06 — Desktop v0.1.4 published + unified Android/Windows public release

### Desktop v0.1.4 publication
- PR #24 merged at `1ab6e9f786c28be2657dc89af27aef9396123eea`.
- Source release workflow `37560800602`: **success**.
- Installed-runtime gate passed again in release CI: install → launch installed LouderMe → remain alive → terminate → uninstall.
- Source release tag `desktop-v0.1.4`: stable, not prerelease.
- Source assets:
  - `LouderMe-Setup-v0.1.4.exe`
  - `LouderMe-Portable-v0.1.4.exe`
  - matching SHA-256 files.

### Unified public release migration
The user requested that Android and Windows downloads appear together on one public release page, matching the distribution style used by other Michel's Lab apps.

Public release workflows were changed so either platform publisher:
- reads the current stable metadata for the other platform;
- downloads and checksum-verifies the unchanged platform's current binaries;
- publishes both platforms into one unified GitHub release;
- points both platform-specific update feeds to that same unified release;
- serializes Android/Windows publication through one shared concurrency group.

Current unified public release:
- tag: `louderme-android-v0.1.9-desktop-v0.1.4`
- title: `LouderMe — Android v0.1.9 + Windows v0.1.4`
- stable / not prerelease / GitHub Latest release.

Assets in the same release:
- `LouderMe-v0.1.9-sideload.apk`
- `LouderMe-v0.1.9-sideload.apk.sha256`
- `LouderMe-Setup-v0.1.4.exe`
- `LouderMe-Setup-v0.1.4.exe.sha256`
- `LouderMe-Portable-v0.1.4.exe`
- `LouderMe-Portable-v0.1.4.exe.sha256`

Verified public feeds:
- Android `louderme/latest.json`: v0.1.9 / versionCode 10, APK URL points to the unified release.
- Desktop `louderme-desktop/latest.json`: v0.1.4, installer/portable URLs point to the same unified release.
- Android APK SHA-256: `1c368d5c4c76d3622f1da132e335243cfa70e6ab8d4583c3b1f457ae8b328e64`.
- Windows Setup SHA-256: `29d4813b9ca7cc8dd83ae13e1d86140ce1a5cbd957ec7e1ae49aceb633c52e01`.
- Windows Portable SHA-256: `1a39efca925444960dc08d847e8209a1fb4bd861d6573f747e7761841585d646`.

Publisher validation:
- Windows-driven unified publisher `37561008337`: **success**.
- Android-driven unified publisher `37561124781`: **success**.
- Both directions were tested to prove that future updates from either platform keep one complete release bundle.
- Temporary transfer staging files were removed after validation.

### Standards
The reusable rule was added to Michel-Software-Standards `standards/REPOSITORY_DISTRIBUTION_STANDARD.md`: multi-platform products should expose one unified customer-facing public release containing all current stable platform binaries while preserving real platform versions and separate updater feeds when needed.

Historical platform-specific releases were intentionally retained for compatibility.


## 2026-10-07 — Repository transferred to Michel's Lab organization

**Change:** repository ownership moved from `realmichelduarte/LouderMe` to `michels-lab/LouderMe`.

**Active references updated:** `.michelslab` governance and agent/Copilot authority now use `michels-lab`; Android Direct and Windows Desktop update manifests now resolve from `michels-lab/michel-s-life-releases` instead of the old-owner redirect.

**Preserved intentionally:** Michel Duarte personal developer/social identity remains under `realmichelduarte`. No release is authorized by this migration change.

**Validation:** Android and Windows branch CI are required before merge.

## 2026-10-08 — Device-volume/identity continuation (partial)

- Closed superseded PR #11 and resolved Desktop startup issue #23 after published v0.1.4 evidence.
- Corrected README direct-update manifest ownership to michels-lab.
- Added Michel's Lab 'Tools with identity' subtitle in Windows About; LouderMe tagline remains 'SOUND THAT LIFTS YOU'.
- Added Windows polling of active endpoint master volume and mute; this is source-level work, not yet runtime-validated. Device volume remains 0–100%, independent from Global Boost 100–250%.
- Android About + media-volume implementation remain pending; attempted Android source updates were blocked, so no Android result is claimed.
- Windows build, installed startup test, real device test and current-SHA CI are pending. No release has been authorized or published.

## 2026-10-08 — Android device volume and About implementation

- After repository visibility changed to public, successfully added 'Tools with identity' under the Michel's Lab lockup in Android About.
- Android MainActivity now reads and sets AudioManager.STREAM_MUSIC (true media stream), normalizes display to 0–100%, and resyncs once per second while the Activity is started. Foreground callbacks are removed on stop.
- Android Compose now has a separate Device Volume slider panel above Global Boost; its changes request Android media stream volume independently from gain.
- Pull request #31 opened with both platform implementations and documentation.
- Android and Windows current-head CI/build status and real-device tests were not confirmed at time of this entry. Do not claim the volume feature functionally validated or close #26 until validation and merge.

## 2026-10-08 — Remove unconditional human-review publication blocker

- User explicitly requested that visual inspection take place after publication rather than requiring manual human approval to start the release.
- Removed the unconditional `throw` step from the Windows Desktop release workflow.
- Existing build, checksum, real installer installation/launch and uninstall smoke checks are retained unchanged.
- This change does **not** establish rendered UI PASS or human visual acceptance. Post-release visual review remains outstanding and any defect found requires a follow-up fix.
- No release was published in this change.

## 2026-10-08 — Stable release preparation: Android v0.1.10 / Windows v0.1.5

- User explicitly authorized release. Version changes: Android versionName 0.1.10 / versionCode 11; Desktop 0.1.5.
- Includes real-device volume controls (Android media stream and Windows playback endpoint) distinct from 100–250% Global Boost, plus Michel's Lab studio About slogan.
- Source repository visibility is public. Removed Android workflow fallback that retrieved production signing keys/passwords from GitHub Actions artifacts, and removed private signing-vault artifact publication. Stable sideload signing now requires valid GitHub Actions Secrets; fail closed if absent. Existing public artifacts and old credentials should be reviewed for exposure; do not assume a key is safe merely because the new workflow is hardened.
- Release notes prepared for both platforms. Same-SHA CI and published artifacts must be rechecked before claiming the release completed. Physical device behavior remains pending.

## 2026-10-08 — Windows compact-window regression and persistent About fix

- User screenshot from Windows v0.1.5 at a small working-area height showed the main window clipped below the Boost card, no working mouse-wheel scroll and About unreachable because it was in the footer.
- Reworked MainForm with monitor-clamped initial bounds, a fixed shell header with always-visible About at top-right and a separate vertically scrollable content Panel; mouse wheel is forwarded to that viewport even if a nested control has focus. Removed footer-only About entry.
- Made AboutForm initial size fit the monitor and responsive card, artwork and social-entry widths.
- Replaced the oversized APO missing-dependency text with a shorter actionable message and explicit distinction between independent Windows device volume and third-party Equalizer APO-dependent Boost/EQ. Equalizer APO remains a user-installed prerequisite; do not claim the boost works without it.
- Added static CI safeguards for top About and scrolling in both Desktop build and release workflows, and added local agent requirements.
- Validation: pending same-SHA CI Windows build, installation/launch smoke, and user's real-window follow-up. No new Windows release published by these source changes.

## 2026-10-08 — Stop existing Android releases being silently replaced

- Desktop-only commits to main still trigger the Android CI & Direct Release workflow. Existing source workflow previously re-signed and uploaded the published APK with `--clobber` even when `versionName` was unchanged, risking mismatch with stable updater SHA-256.
- Added `release_guard` to check whether the stable Android tag already exists. If so, signing and publication jobs skip while build validation continues. The publisher also refuses to overwrite an existing tag as a second safeguard.
- Future Android releases require a unique version and validated continuity of the original signing identity. No new APK version or release authorized by this guard.
