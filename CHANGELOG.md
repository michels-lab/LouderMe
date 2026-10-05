# Changelog

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
