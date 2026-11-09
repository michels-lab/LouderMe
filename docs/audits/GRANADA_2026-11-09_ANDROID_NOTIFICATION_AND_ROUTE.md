# LouderMe — Operación Granada: Android notification controls and route updates (2026-11-09)

## Delivered functional block

1. User-facing foreground notification now exposes **Boost −**, **Boost +**, and **Stop**. Each action has a distinct, immutable, explicitly scoped `PendingIntent.getService()` targeting `AudioBoostService`, so no activity must be launched from background. The Boost shortcuts select the adjacent supported 100/125/150/175/200/225/250% level; they preserve clamp bounds when the slider previously chose a non-preset value.
2. If an old notification action arrives after Boost was stopped, increase/decrease **must not** start an abandoned session. The service checks `lastState.isRunning` and stops itself on stale actions. Stop remains usable independently and persists OFF via the existing service path.
3. Foreground service registers `AudioDeviceCallback` for outputs connecting/disconnecting; it only publishes a fresh route when an engine exists, the service is active and the route actually changes. This updates persistence, Home state broadcast and notification. Unregisters the callback during teardown to avoid leaking the service.
4. Android API 33+ now uses `AudioManager.getAudioDevicesForAttributes(AudioAttributes.USAGE_MEDIA)` to report the **anticipated media path**, rather than misrepresenting every merely connected output as currently selected. Older versions retain the existing best-effort device-enumeration fallback. Neither path is acoustic proof that another app's PCM was boosted.
5. Pure JVM test suites cover adjacent-preset traversal, arbitrary slider percentages, min/max boundaries, unknown routes and device-type priority. The notification/service and hot-plug runtime require instrumented/physical Android verification.

## QA and release separation

- This is a code-only PR. Existing GitHub Android CI must compile the public Play/Direct and separate Developer flavors, run unit tests, install emulator candidates and capture actual screens before merge.
- Human validation still needed: expanded notification on Android 13+ and physical Samsung; action order/survival on lock screen, Bluetooth hot-plug, media-route switching without a reconnect, and audio behavior when external apps are playing.
- Notification actions are an interface to LouderMe's **existing** session-0 compatibility engine, not evidence of system-wide gain. Real-world support varies by device.
- No version increment, signer change, updater change, Windows driver change, binary distribution or release. PR #156 owns experimental Windows 5.1/7.1 speaker-position mapping; issue #48 retains physical driver gates.
- Because PR #156 edits `PROJECT_LOG.md`, this PR preserves that ownership and documents its exact work here. A subsequent independent integration can update the aggregated log after PR #156 is reconciled.

## Reentry

Review Android CI check conclusions at exact PR SHA. Integrate only green CI into main. A stable release additionally needs signed final candidate validation, updater provenance and user testing of the actual package, not merely source compilation.
