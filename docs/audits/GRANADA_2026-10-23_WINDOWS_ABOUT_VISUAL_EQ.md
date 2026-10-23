# Operación Granada 2026-10-23 — Real Windows About, Logo, 7-band EQ

## Owner-reported bugs (Windows v0.1.15)
1. **About window is almost entirely black** with two very narrow empty vertical bordered strips, while real content is inaccessible.
2. **Official product logo looks pasted into a stark white rectangle**, unrelated to the otherwise dark LouderMe theme.
3. **Equalizer feels like a spreadsheet**, with a thin curve and seven numeric rows rather than a graphical musical control.

These are live user screenshots, not mockups or theoretical issues.

## Root causes and actual implementation
- In `AboutForm`, autosized `Panel` cards containing `DockStyle.Top` autosized children created an unstable circular preferred-size relationship. On the owner's computer the panels collapsed to roughly 30–50px despite the code requesting 800px. Replace autosizing of outer cards with **explicitly measured width/height** on a single scroll owner; ensure author, social, Update, privacy/legal and Close remain accessible. Add runtime `AssertVisualLayout` check of card dimensions, visible logo and working vertical scrolling to both About real screenshot evidence and compact layout tests.
- The existing official SVG **is canonical**, but its dark-navy text was simply rendered on a white-backed `PictureBox`. The Windows branding stage now derives the same official wave and wordmark with only navy-ink → light-ink and muted tagline → higher-contrast ink, preserves transparency, trims unused SVG margins and draws on native dark app background. The SVG master remains unchanged. The Main/Splash/About forms use the same embedded derivative; no white rectangle.
- `EqCurveControl` becomes a large 182px response graph with ±10dB labels, grid, zero guide, responsive filled curve and colored nodes. A new `EqBandFaderControl` draws **seven interactive vertical faders** with dB/frequency labels; mouse drag, keyboard arrows (0.5dB steps), Home reset, double-click zero. Seven compact NumericUpDown controls remain for exact input.
- Crucially, faders route through **existing NumericUpDown.ValueChanged** and the existing `_settings`/`EqualizerAPO` pipeline; selecting a preset updates plotted values, controls and saved APO settings; all controls are disabled while an audio test runs or Equalizer APO is not installed. Audio processing code itself is untouched.
- Do not change Michel's Lab / other app projects or Android identity.

## Acceptance / release gates
1. Exact PR head Windows CI compiles, renders canonical PNG/ICO in ImageMagick, runs installed Windows app and `--layout-smoke-test` plus real Home/About compact+wide visual screenshots. The test must **fail on collapsed cards**, not merely pass when a window opens.
2. CI must also prove plotted curve + seven faders use the real existing APO/UI settings, preserve presets and 0.5dB precision. No fake gain, sample prerecorded visualization, or fictitious processing status.
3. Android regression green; no Android version bump or APK re-sign.
4. Only after full CI pass may the distinct **Windows v0.1.16** source release be merged and generated, and then the separate unified-public publisher can be updated after validating four Windows release assets and the unchanged signed Android v0.1.12 binary. No RC.
5. Physical audible Windows processing still requires properly attached Equalizer APO. Do not claim native unsigned SysVAD driver or hardware gain verification from visuals.

Status: source implementation and CI pending; not a published binary until proven.
