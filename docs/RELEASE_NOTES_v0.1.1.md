# LouderMe v0.1.1 — Audio Engine & Update Foundation

This release turns LouderMe from a static UI shell into a real Android audio-engine test build.

## Included
- Michel's Lab package identity: `com.michelslab.louderme`.
- Automatic Google Play update checking and About update controls.
- Michel's Lab About screen with canonical developer portrait.
- Persistent user-started foreground audio service.
- 100–200% controls connected to an experimental Android audio-effect engine.
- LoudnessEnhancer session-0 path with DynamicsProcessing fallback.
- Live engine state, output route and diagnostic implementation details.
- Gain conversion tests.
- CI that always compiles/tests current HEAD.
- APK SHA-256 checksum.

## Important validation boundary

Android explicitly documents global insert effects through audio session 0 as deprecated. LouderMe therefore does **not** claim that v0.1.1 reliably boosts Spotify/YouTube on every phone.

If the app reports **Engine attached**, Android accepted and enabled the effect and LouderMe has control of it. The next required test is a real A/B playback test on the target Samsung phone.

## Not included yet
- full Mixer / EQ;
- Smart Boost DSP;
- proven universal Android system-wide amplification.
