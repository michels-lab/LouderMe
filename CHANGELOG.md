# Changelog

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
