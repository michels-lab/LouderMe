# LouderMe v0.1.7

Stable Android release by **Michel's Lab**.

## What's new
- Completes LouderMe's integrated product branding beyond the initial launcher/logo migration.
- Uses the current canonical Michel Duarte portrait and official Michel's Lab parent-brand lockup in About.
- Adds canonical social/contact order with recognizable icon + visible name for Instagram, Facebook, LinkedIn, GitHub and Email.
- Reconciles the About hero to the official **SOUND THAT LIFTS YOU** identity.
- Adds waveform-derived visual feedback to Global Boost and the 7-band EQ.
- Adds opt-in **Start with phone** behavior so an active boost can resume after Android finishes booting, subject to Android/manufacturer background restrictions.
- Preserves the existing 100–250% boost engine, 7-band EQ, Play/Direct split and verified Direct updater.

## Distribution
- Package: `com.michelslab.louderme`
- versionName: `0.1.7`
- versionCode: `8`
- Michel's Lab Direct uses the stable existing signing identity and supports in-place updates from v0.1.6.
- Google Play flavor remains separated from sideload-only install permission.

## Validation boundary
Repository CI validates the current commit, Play/Direct builds, branding contract, package IDs, permissions, signing and release integrity.

Audible high-gain/EQ behavior on the target Samsung and Play production delivery remain physical/store validation items. Digital target gain is not a measurement of acoustic dB SPL.
