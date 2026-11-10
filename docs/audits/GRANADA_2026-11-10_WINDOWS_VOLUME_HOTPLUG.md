# Operación Granada — Windows output-bound volume and mute controls

Date: 2026-11-10.

## Functional change

The Desktop UI refreshed the multimedia default output only every 1.5 seconds. A volume slider/mute click occurring between an unplug, Bluetooth reconnection, or change of default Windows output and the next timer tick could write through an old `MMDevice` COM reference, or use its stale volume state.

The existing AudioDeviceController now refreshes and verifies the exact endpoint ID **on every user-initiated volume or mute write**, rejecting a write if the currently routed Windows multimedia default is not the device shown to the user. The UI then forces an immediate volume/mute/output refresh and invalidates prior audible proof through its existing route revision tracking. A failed default-endpoint lookup discards the old COM device reference instead of keeping a disconnected endpoint cached.

For a continuous TrackBar **mouse drag**, the starting endpoint ID is captured at MouseDown. If the route changes mid-gesture, LouderMe suppresses subsequent slider events until MouseUp or mouse capture loss, even after the UI discovers the new endpoint. Keyboard adjustments remain independently bound to the last displayed output. This prevents a handoff from transferring the remainder of an already-in-progress drag onto a different device.

If Windows has no current multimedia output, LouderMe shows an unavailable state and **disables the volume slider and Mute checkbox**; the timer retries and re-enables both controls after an output returns. The UI never changes Windows' default device; it only controls the current default multimedia endpoint. These controls change master volume 0–100%, **not** native app PCM/DSP.

The device-free `OutputRouteEvidencePolicy.RunSelfTests()` now includes explicit same-output binding, output mismatch, null/empty IDs and case-sensitive endpoint identities. The contract executes through existing `--check-apo-contract` Windows desktop CI. The code never reports an audio-path test as proof for a different output.

## QA and limitations

- Build Windows desktop and run `--check-apo-contract`, `--check-settings-contract`, layout and visual smoke tests on an installed/CI candidate before merge.
- Manual physical test: while **holding** a volume drag, change the default output from A to B, continue moving the mouse, then release; confirm B receives no adjustment from the original A gesture. Also unplug headphones, change Bluetooth endpoints, try keyboard adjustments, and restore a renderer. Verify no stale output write, no wrong-target mute and slider re-enabling after reconnection.
- COM device changes can still race after an exact ID check and before Windows processes a write; Windows is the final authority for hot-unplug. No automatic physical output selection/reassignment or driver install is performed.
- Native SysVAD/independent-app external PCM amplification remains tracked under #48. This PR changes no signer, update channel, release, Android code or version. No RC/stable release.
