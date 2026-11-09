# LouderMe Granada — 2026-11-08: sound-output changes invalidate stale listening evidence

## Functional problem
The desktop previously kept the user's last four-second -18 dB listening answer after Windows changed its default output (e.g. speakers to Bluetooth). It could then appear in the diagnostic report as positive audio evidence for an untested device.

## Implementation
- Expose the selected Windows multimedia endpoint ID only in memory. Never persist or export it.
- Polling now invalidates the previous listening result and updates the UI when the selected output changes or becomes unavailable.
- The four-second attenuation experiment snapshots the endpoint ID and a monotonic local selection revision; after the user's answer it verifies the same identity and revision before recording a yes/no answer. A detected A->B->A swap also invalidates the answer.
- Output-device refresh obtains a replacement before disposing the previous endpoint, reducing transient hot-unplug failures.
- Device-free policy selftests exercise unchanged endpoint, changed endpoint, disappearance, missing identity and A->B->A. They run under the existing --check-apo-contract in Windows build and release CI.

## Limitations
The 1.5-second polling cannot detect an entire A->B->A switch between ticks; no undocumented hardware test is claimed. Configuration on disk does not certify attachment to the new playback endpoint. The experimental native route still requires VM/driver/physical output proof under issue #48. No stable release or RC.
