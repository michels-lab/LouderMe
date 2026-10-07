# LouderMe v0.1.8

Stable Android hotfix by **Michel's Lab**.

## Fixed
- Replaces the rejected/corrupted interim Michel Duarte About portrait with the exact canonical user-provided 1440×1920 JPEG from Michel-Software-Standards.
- Keeps the canonical portrait byte-for-byte unchanged and reduces only its on-screen footprint so the author block fits the LouderMe About hierarchy cleanly.
- Enforces the canonical portrait Git blob in Android CI so an old or modified copy cannot silently return.

## Preserved
- Official LouderMe waveform identity and Michel's Lab parent-brand lockup.
- Instagram, Facebook, LinkedIn, GitHub and Email About rows.
- 100–250% boost engine, 7-band EQ and presets.
- Play / Michel's Lab Direct split.
- Stable Direct signing identity, in-place updater and Start with phone.

## Distribution
- Package: `com.michelslab.louderme`
- versionName: `0.1.8`
- versionCode: `9`
- Michel's Lab Direct remains signed with the existing stable identity and supports in-place updates.
- Google Play flavor remains separated from sideload-only install permission.

## Validation boundary
Repository CI validates the canonical portrait, Play/Direct builds, package IDs, permissions and signing.

The previously reported Samsung About crash is not considered conclusively closed until this v0.1.8 build is opened on the target device. The corrupted portrait was a confirmed defect and a plausible contributor, but runtime validation remains the final gate.
