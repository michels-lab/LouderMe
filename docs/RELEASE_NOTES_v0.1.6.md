# LouderMe v0.1.6

LouderMe now ships with its official Michel's Lab product identity: **Option 4 — flowing waveform**.

## What's new
- Official LouderMe launcher and round-launcher icon.
- Android adaptive icon support.
- Official branded splash/launch treatment.
- Official waveform replaces the temporary `LM` mark in the app command bar.
- About now uses the LouderMe waveform, wordmark and `SOUND THAT LIFTS YOU` identity.
- Canonical branding sources are tracked locally with provenance back to `Michel-Software-Standards`.

## Preserved behavior
This release does not alter the audio engine, 100–250% boost range, EQ behavior, updater architecture, signing identity, Play/sideload permission split, or local privacy model.

## Validation
The release candidate must pass the full LouderMe Android CI before publication:
- unit tests;
- Play build;
- sideload build;
- package-ID verification;
- Play/sideload permission separation;
- stable Michel's Lab Direct signing after merge to `main`.

The official branding implementation already passed its dedicated PR validation before this version bump.
