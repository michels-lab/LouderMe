# Operación Granada — Android media route continuity and audio-effect recovery

Date: 2026-11-09. Scope: LouderMe Android session-0 compatibility engine. This file describes a source-code change, not an acoustic measurement or release.

## Problem

PR #158 began responding to Android output-device callbacks but only changed the displayed route label. That did not detect one Bluetooth headset being replaced by another Bluetooth headset with the same human label, nor check whether Android revoked control of the running LoudnessEnhancer or Equalizer after a USB/Bluetooth handoff. A short-lived empty predicted MEDIA device list could also falsely suggest that the route had changed.

## Functional changes

1. New pure Kotlin `MediaRouteContinuity` keeps a canonical list of (audio device type, Android runtime ID) pairs **only in the running service's memory**. It sorts/deduplicates devices, detects same-category hardware swaps and avoids repetitive rechecks when callbacks carry the same device identities. Device identities are never persisted, broadcast, logged or presented in About.
2. Coalesces fast callbacks using a single main-thread Handler callback (300 ms), and retries one transient empty MEDIA snapshot once after 700 ms when there had been a concrete route. It cancels pending callbacks on Stop/service destruction and never restarts a stopped Boost from a device notification.
3. `SessionZeroAudioEngine.hasActiveEffect()` and `SessionZeroEqualizer.hasActiveEffect()` inspect live effect enabled/ownership, returning false safely if the vendor implementation throws while tearing down devices. After a real route handoff the service attempts to reattach Boost **only when it has lost ownership**; on a still-controlled Boost, the EQ can be independently reapplied only if its ownership disappeared. The existing audio engine retains its reported `ATTACHED` vs `UNSUPPORTED` semantics: this is not global acoustic proof.
4. When a newly activated Boost is unsupported or errors out, the service releases the stale engine before stopping. Reconnection preserves the selected Boost percentage, updates route broadcasts/foreground notification and reapplies saved EQ when Boost successfully attaches.
5. Pure JUnit regression tests cover runtime ID replacement with an identical label, canonical identity despite order/duplicates, duplicate-callback no-ops, single bounded empty snapshot retry and attachment-loss recovery decisions.

## Opt-in restart at BOOT_COMPLETED

- Android 13+ does not require the `POST_NOTIFICATIONS` runtime permission to **start** a foreground service, according to Android's official notification runtime-permission documentation. LouderMe previously blocked its opt-in boot restore if the user had denied that permission. The boot receiver now honors the saved opt-in + previously running state without imposing a redundant notification-permission gate (the system may still hide the user-facing notification).
- `BootResumePolicy` centralizes the opt-in/status/percentage decision and has JVM tests for disabled settings, stopped/failed sessions and clamped Boost. The receiver uses a guarded `startForegroundService()` call; a platform/OEM background-start rejection no longer crashes the broadcast or leaves a saved ATTACHED state. Instead, the app saves an ERROR with instructions to manually restart.
- Some Android versions/OEMs enforce additional background foreground-service launch restrictions. Permission-free boot restore is not a promise that every Android build permits the launch, and must be exercised on a real device.

## Verification and limits

- Android GitHub CI must compile/test Play, Direct and Developer flavors and run actual emulator Home/About/Developer validation before merge. No private signing material is accessed in this branch.
- Android's `AudioEffect` session 0 is an experimental compatibility path, not reliable system-wide interception. Reattaching a reported owned effect does not establish that playback from every external app was amplified. Physical owner acceptance requires Android device reproduction using real Bluetooth/USB route changes while third-party playback is active, comparing actual output and checking for audible glitches.
- No Windows driver, signed APK, updater, public versions, app package identity or distributions modified.
- PR #156 remains the owner of Windows 5.1/7.1 speaker-map code and the shared `PROJECT_LOG.md`; this patch deliberately does not touch either.

## Continuation point

Integrate only after exact-head CI is green; keep physical route/audio tests and the separate Windows driver gates open. Do not publish an RC or stable release based solely on automated tests.
