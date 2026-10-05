# LouderMe Architecture

## Core success criterion

LouderMe exists to increase general phone audio output from apps such as Spotify, YouTube, Instagram and browsers, then let the user shape that signal transparently.

## UI

Native Jetpack Compose.

v0.1.3 adopts the current IG Cleaner Pro product-family visual language:
- background `#060910`;
- canvas/surfaces `#090E17`, `#0D1521`, `#111C2B`;
- cyan `#71D7FF`, blue `#5D9CFF`, gold `#EFBD62`;
- compact status badges and monospace operational labels;
- dashboard/workspace cards.

The layouts are Android-native rather than copied web markup.

## Audio foreground service

`AudioBoostService` is a user-started foreground service using Android's `specialUse` foreground-service type so audio effects can remain active while another app is foregrounded.

## Boost engine

`AudioEngine` currently tries:

1. `LoudnessEnhancer(0)`
2. fallback: `DynamicsProcessing(0)`

The target Samsung device has been audibly validated with external-app playback.

## Equalizer engine

`SessionZeroEqualizer` uses `Equalizer(0, 0)`.

The UI exposes seven stable target bands:
`60, 150, 400, 1000, 2500, 6000, 12000 Hz`.

Android Equalizer implementations expose a device-specific band count and center frequencies. LouderMe therefore:

1. asks Android which native band contains each target frequency;
2. groups LouderMe target bands that resolve to the same native band;
3. averages their requested gain;
4. clamps the request to the device-reported band-level range;
5. applies the native level;
6. exposes the resulting mapping in Diagnostics.

The user-facing EQ range is -10 to +10 dB target band gain.

## EQ persistence

`EqualizerStateStore` persists:
- enabled state;
- selected preset;
- seven requested band gains.

Preset changes and custom bands apply immediately while Global Boost is running. If boost is off, the configuration is saved and applies the next time the audio service starts.

## Gain semantics

Boost percentage is a digital amplitude ratio:

`signalGainDb = 20 * log10(percent / 100)`

The displayed dB value is target signal gain, not acoustic dB SPL.

## Updates

Google Play In-App Updates:
- automatic check on launch/resume;
- visible status on the Home workspace;
- manual Check action;
- Install update action when available;
- update controls also remain in About;
- interrupted immediate updates resume on foreground.

Direct/debug GitHub APKs cannot use Play ownership-based in-app updates.

## Remaining audio milestones

- validate the EQ audibly on the target Samsung device;
- limiter/compressor for high boost;
- output-device profiles;
- Smart Boost Beta;
- signed Play AAB/internal testing.
