# LouderMe v0.1.3 — Audio Workspace + Equalizer

v0.1.3 turns LouderMe into a more complete Michel's Lab audio workspace.

## New visual system
The app now follows the current IG Cleaner Pro family language:
- near-black workspace;
- layered blue-black surfaces;
- cyan/blue operational accents;
- gold highlights;
- compact status badges;
- dashboard cards and workspace hierarchy.

The implementation remains native Jetpack Compose.

## Equalizer
A real session-0 Equalizer is now wired to the audio service.

Target bands:
- 60 Hz
- 150 Hz
- 400 Hz
- 1 kHz
- 2.5 kHz
- 6 kHz
- 12 kHz

Presets:
Flat, Bass, Deep Bass, Dialogue, Treble, Speaker, Headphones.

Manual edits become Custom.

Because Android hardware may expose a different number of EQ bands, LouderMe maps the seven target frequencies to the device's reported native bands and exposes that mapping in Diagnostics.

## Updater
Update status is now visible directly on Home in addition to About.

Play builds retain:
- automatic update checks;
- manual Check;
- Install update action;
- resume behavior for interrupted immediate updates.

## Existing boost
100–250% boost remains intact.

## Validation boundary
The boost path is already audibly validated on the target Samsung device.

The newly added equalizer must still be audibly validated on that device before LouderMe treats the EQ path as device-validated.
