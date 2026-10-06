# Changelog

## Desktop v0.1.0 — 2026-10-06

### Added
- First native Windows release of LouderMe.
- Self-contained `LouderMe-v0.1.0.exe` portable build.
- Installable `LouderMe-Setup-v0.1.0.exe` package via Inno Setup.
- Official LouderMe Option 4 Windows icon and Michel's Lab identity.
- Native Windows output-device detection, 0–100% master-volume control and mute.
- Opt-in **Start LouderMe with Windows**.
- Real **Global Boost 100–250%** using the Windows APO effects path.
- Percent-to-digital-gain mapping: 150% = +3.52 dB, 200% = +6.02 dB, 250% = +7.96 dB.
- 7-band EQ with Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones and Custom presets.
- Safe Equalizer APO configuration integration through LouderMe's own managed include.
- First-use backup of the existing Equalizer APO main configuration.
- SHA-256 checksums for the portable executable and installer.

### Windows audio dependency
- System-wide boost above the native Windows 0–100 endpoint requires Equalizer APO 1.4.2 x64.
- LouderMe does not redistribute Equalizer APO; the app links to its official project page.
- ASIO and WASAPI exclusive-mode streams can bypass Windows APO effects.

### Validation
- Native .NET 10 build passed.
- Real-global-boost contract passed.
- Portable EXE generation passed.
- Inno Setup installer generation passed.
- EXE and installer checksum generation passed.
- Android regression CI passed so the Desktop addition does not break the mobile product.

### Validation boundary
- Physical Windows endpoint/APO attachment and audible 100/150/200/250% behavior still require validation on the target PC.
- Windows Authenticode/code-signing is not configured for v0.1.0; SHA-256 integrity files are published.


## v0.1.6 — 2026-10-06

### Added
- Official LouderMe **Option 4 — flowing waveform** product identity from the Michel's Lab standards repository.
- Official Android launcher and round-launcher icon.
- Adaptive Android launcher foreground/background assets.
- Official branded launch/splash treatment.
- Official waveform branding in the command bar.
- Official waveform, wordmark and `SOUND THAT LIFTS YOU` treatment in About.
- Canonical SVG branding sources stored locally for provenance without runtime dependency on the private standards repository.

### Changed
- Replaced the temporary `LM` placeholder identity with the official LouderMe mark.
- Preserved the existing IG Cleaner-family layout, audio engine, EQ, updater, signing and distribution behavior.

### Validation
- Branding PR CI passed unit tests and Play/sideload builds.
- Package ID and flavor permission separation checks passed.
- Android resource linking issue found in the first splash implementation was corrected before merge.
- Audible EQ/high-gain quality remains a physical-device validation item and is unchanged by this branding release.


## v0.1.5 — 2026-10-06

### Added
- Explicit high-gain peak-protection policy for the DynamicsProcessing fallback.
- Real DynamicsProcessing Limiter configuration from 175% upward.
- Progressive limiter profiles for 175%, 200%, 225% and 250% boost ranges.
- Unit tests for protection thresholds, progression and input clamping.

### Changed
- The already-validated LoudnessEnhancer path remains the primary engine and is not replaced.
- High-gain diagnostics now explain that Android LoudnessEnhancer compresses samples that would exceed the supported sample range.
- DynamicsProcessing keeps working even if a device rejects LouderMe's explicit limiter configuration; that failure is surfaced in diagnostics instead of breaking the fallback.

### Validation boundary
- This release candidate still requires CI compilation/tests.
- Audible EQ and high-gain quality validation on the target Samsung device remain physical-device checks.
- Peak protection reduces digital overload risk; it is not a measurement or guarantee of acoustic listening safety.


## v0.1.4 — 2026-10-05

### Added
- Dual update channels: Google Play and Michel's Lab direct/sideload.
- Automatic public update-manifest check for sideload installs.
- Automatic direct APK download.
- SHA-256 verification before install.
- Package ID and versionCode verification.
- Signing-certificate pinning to the currently installed LouderMe sideload identity.
- PackageInstaller-based Android install handoff.
- Unknown-source permission flow only in the sideload flavor.
- Home/About update states for download progress, ready-to-install, permission required and installing.
- Separate `play` and `sideload` Android product flavors.
- Public LouderMe bootstrap update feed without exposing private source.
- One-time stable sideload signing-key bootstrap workflow.

### Security
- The Play flavor does not request `REQUEST_INSTALL_PACKAGES`.
- The direct updater never embeds a GitHub private token.
- A downloaded APK is rejected if checksum, package, version or signing identity do not match.
- Android retains final install confirmation.

### Migration
v0.1.3 and earlier GitHub debug APKs used non-durable CI debug signing. v0.1.4 establishes a stable sideload signing identity, so the transition from an existing debug install requires one uninstall/reinstall. Future stable-signed direct updates can update in place.

## v0.1.3 — 2026-10-05

### Added
- Native IG Cleaner-inspired Audio Workspace visual system.
- Real 7-band equalizer controls: 60 Hz, 150 Hz, 400 Hz, 1 kHz, 2.5 kHz, 6 kHz, 12 kHz.
- EQ presets: Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones.
- Custom mode after manual band edits.
- Persistent local EQ state and presets.
- Device-band mapping through Android Equalizer session 0.
- EQ attached/degraded/unsupported diagnostics.
- Home-screen software update status and Check/Install actions.
- Unit tests for EQ preset structure and range sanitization.

### Changed
- LouderMe now uses the current Michel's Lab / IG Cleaner family palette and dashboard hierarchy.
- About was restyled to the same product family.
- Output, engine, signal and EQ state are visible as compact dashboard metrics.
- Mixer/EQ is now functional instead of a disabled placeholder.
- Privacy documentation now includes locally stored EQ configuration.

### Validation boundary
Boost is already audibly validated on the target Samsung device. The new EQ implementation still requires an audible target-device test after this build passes CI.

## v0.1.2 — 2026-10-05

### Added
- 225% quick boost level (~+7.04 dB target signal gain).
- 250% quick boost level (~+7.96 dB target signal gain).
- Fine boost slider extended through 250%.
- Gain-conversion tests for 225% and 250%.

### Changed
- All boost clamps now support 100–250%.
- Quick controls wrap into two rows for better mobile spacing.
- UI terminology now says **target signal gain** to distinguish digital gain from acoustic dB SPL.
- Engine messaging now records that the target Samsung device audibly responded to the session-0 path.

### Technical note
The reported dB values are digital amplitude-gain values. They are not measurements of loudspeaker SPL, and higher gain may trigger device/source-dependent limiting, compression, or distortion.

## v0.1.1 — 2026-10-05

### Added
- Michel's Lab Android identity: `com.michelslab.louderme`.
- Google Play in-app update checking with automatic availability checks and manual About action.
- Michel's Lab native About surface with canonical developer portrait and official links.
- User-started foreground audio-boost service.
- Experimental session-0 LoudnessEnhancer engine with DynamicsProcessing fallback.
- Explicit engine states: off, starting, attached, degraded, unsupported, error.
- Output-route diagnostics and underlying-effect diagnostics.
- Quick boost controls wired to real engine requests.
- Fine 100–200% slider.
- Unit tests for percent → dB / millibel conversion.
- CI that always builds/tests current HEAD before publication.
- Versioned APK SHA-256 checksum in GitHub releases.
- Privacy and Play-readiness documentation.

### Changed
- Package/application ID migrated from `com.realmichelduarte.louderme` to `com.michelslab.louderme` before first Google Play production publication.
- Release publication is now separate from build validation; an existing release can skip upload but cannot skip the build.

### Known limitations
- Android deprecates global insert effects attached through audio session 0.
- A successful “Engine attached” state only proves the public effect engine was acquired; cross-app amplification still requires real-device validation.
- Mixer / EQ UI and Smart Boost remain future milestones.
- GitHub debug APKs do not receive Google Play in-app updates.
