# LouderMe

**Make everything louder.**

LouderMe is an Android audio booster by **Michel's Lab** focused on increasing low-volume media output with one-tap gain levels, diagnostics, future mixer/EQ controls, and an experimental Smart Boost path.

## Current product priorities

1. Real phone-wide audio boost is the primary goal.
2. One-tap boost levels: 100%, 125%, 150%, 175%, 200%, 225%, 250%.
3. Fine gain adjustment.
4. Clear engine status — the UI must not fake global processing.
5. Mixer / EQ and saved presets.
6. Smart Boost remains Beta and secondary to reliable manual boost.

## Identity

- Product: `LouderMe`
- Studio: **Michel's Lab**
- Android package: `com.michelslab.louderme`
- Current development version: `0.1.2`
- Developer: Michel Duarte / Michel Armando Duarte Flores
- License: Proprietary — All Rights Reserved

## Current audio engine

The first real audio-engine implementation uses an **experimental audio-session-0 compatibility path**. Android documents global insert effects on session 0 as deprecated, so a successful engine attachment is not treated as proof that every external app is being amplified.

The app reports:
- engine attachment state;
- requested gain in percent and dB;
- output route;
- underlying effect implementation;
- unsupported/error state.

The session-0 path has been audibly validated on the target Samsung device. Compatibility on other Android devices can still vary.

## Updates

Google Play builds use Play In-App Updates and check automatically for newer versions. Direct/debug GitHub APKs are test builds and are not owned by Google Play, so they cannot use the Play production update channel.

## Project standards

Shared engineering, About, update, branding, release and audit standards live in the private `Michel-Software-Standards` repository. This repository keeps LouderMe's app-specific Project Audit Log.


## Signal-gain terminology

Percent values are defined as digital amplitude multipliers, not as acoustic sound-pressure levels.

Examples:
- 150% = +3.52 dB target signal gain
- 200% = +6.02 dB target signal gain
- 225% = +7.04 dB target signal gain
- 250% = +7.96 dB target signal gain

These dB values do **not** mean the loudspeaker produces the same increase in dB SPL. Actual acoustic output depends on the phone/speaker/headphones, DSP, limiters, source content, frequency response, distance, and other hardware factors.
