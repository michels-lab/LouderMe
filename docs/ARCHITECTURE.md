# LouderMe Architecture

## Core success criterion
LouderMe exists to increase general phone audio output from apps such as Spotify, YouTube, Instagram and browsers — not merely to change Android's normal media-volume slider.

## Layers

### UI
Jetpack Compose.
- master boost toggle;
- 100 / 125 / 150 / 175 / 200 / 225 / 250% quick controls;
- fine slider;
- output route;
- engine diagnostics;
- About / updater.

### Audio service
`AudioBoostService` is a user-started foreground service using the Android `specialUse` foreground-service type so the requested audio effect can remain alive while another app is foregrounded.

Google Play reviews `specialUse` declarations, so the production store submission must explain this user-visible audio-processing purpose.

### Audio engine abstraction
`AudioEngine` isolates platform/vendor strategies from UI.

Current implementation:
1. `LoudnessEnhancer(0)`
2. fallback: `DynamicsProcessing(0)`

Both use audio session 0 only as an experimental compatibility path.

Android's public AudioEffect documentation says attaching insert effects to the global output mix with session 0 is deprecated. Therefore:
- ATTACHED means the effect object was enabled and LouderMe has control;
- it does **not** mean cross-app gain has been proven;
- The target Samsung device has been audibly validated with external-app playback. Other Android devices can still behave differently because session-0 global insert effects are deprecated.

### Gain semantics
UI percentage is a digital amplitude ratio. The resulting dB value is **target signal gain**, not acoustic dB SPL:

`signalGainDb = 20 * log10(percent / 100)`

Examples:
- 100% = 0.00 dB
- 125% ≈ +1.94 dB
- 150% ≈ +3.52 dB
- 175% ≈ +4.86 dB
- 200% ≈ +6.02 dB
- 225% ≈ +7.04 dB
- 250% ≈ +7.96 dB

LoudnessEnhancer uses millibels: 100 mB = 1 dB.

### Updates
Google Play In-App Updates:
- automatic availability check;
- immediate update prompt when Play reports an allowed newer version;
- manual Check for updates in About;
- interrupted immediate updates are resumed on app foreground.

Direct/debug GitHub APKs remain a test channel and cannot use Play ownership-based in-app updates.

## Next audio milestones
- physical S26 Ultra session-0 validation;
- device/vendor fallback research if session 0 is rejected or ineffective;
- EQ/mixer;
- limiter/compression strategy;
- device-specific profiles;
- Smart Boost Beta.


### Signal gain vs acoustic level

LouderMe does not infer speaker loudness in dB SPL from the requested digital gain. A +7.96 dB target signal gain at 250% does not guarantee +7.96 dB SPL at the listener. Hardware sensitivity, amplifier headroom, Android/vendor DSP, source crest factor, limiting, frequency response, output route, and listening distance all affect the acoustic result.
