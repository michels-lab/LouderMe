# LouderMe

**Make everything louder.**

LouderMe is an Android audio utility by **Michel's Lab** for increasing quiet media output, shaping sound with a real equalizer, and exposing the state of the Android audio engine instead of hiding it behind fake controls.

## Current release line

**v0.1.3 — Audio Workspace + 7-band EQ**

### Global Boost
- 100%, 125%, 150%, 175%, 200%, 225%, 250%.
- Fine 100–250% slider.
- Target signal-gain readout.
- Output-route and engine diagnostics.

### Equalizer
Seven LouderMe target bands:
- 60 Hz
- 150 Hz
- 400 Hz
- 1 kHz
- 2.5 kHz
- 6 kHz
- 12 kHz

Presets:
- Flat
- Bass
- Deep Bass
- Dialogue
- Treble
- Speaker
- Headphones
- Custom when a band is edited manually

Android devices can expose a different number of native EQ bands. LouderMe maps each target frequency to the nearest band reported by Android and reports the mapping in Diagnostics.

### Updates
Google Play builds check automatically for a newer version and expose update state directly on Home and in About. Direct/debug GitHub APKs remain a test channel and are not owned by Google Play.

## Visual system

v0.1.3 adopts the same Michel's Lab product-family language used by the current IG Cleaner Pro UI:
- near-black workspace;
- blue-black layered surfaces;
- cyan/blue operational accents;
- gold for highlighted/special actions;
- compact monospace status labels;
- rounded dashboard cards;
- visible engine/update status.

This is a native Compose implementation, not an embedded copy of IG Cleaner.

## Identity

- Product: `LouderMe`
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Current version: `0.1.3`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved

## Audio-engine boundary

Boost and EQ use Android audio-effect APIs on the session-0 compatibility path. The boost path was audibly validated on the target Samsung device. Android deprecates global insert effects on session 0, so compatibility on other Android devices can differ.

## Signal-gain terminology

Percent values are digital amplitude multipliers, not acoustic sound-pressure levels.

Examples:
- 150% = +3.52 dB target signal gain
- 200% = +6.02 dB target signal gain
- 225% = +7.04 dB target signal gain
- 250% = +7.96 dB target signal gain

Those values do **not** predict dB SPL from a speaker or headphones.
