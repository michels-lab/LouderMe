# Architecture notes

## Core principle
The app's success criterion is real amplification of general phone audio, not merely changing Android's normal media-volume slider.

## Planned layers
- UI: Jetpack Compose.
- State: ViewModel/state-holder layer.
- Audio engine: isolated behind an interface so device-specific/global processing strategies can be swapped without redesigning the UI.
- Presets: persistent local storage.
- Diagnostics: expose whether the processing engine is attached and active.

## Initial controls
- Booster power toggle.
- Quick levels: 100%, 125%, 150%, 175%, 200%.
- Fine boost slider.
- Preset selector.
- Mixer/EQ entry point.
- Smart Boost Beta toggle, disabled by default.

## Safety / quality
Use limiter/dynamics protection where available to reduce clipping and distortion. Do not present a percentage as functioning unless the audio engine confirms that processing is active.
