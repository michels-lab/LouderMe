# Operación Granada — independent Windows application audio route initialization

Date: 2026-10-22. Owner: LouderMe Windows native bridge / issues #46, #48.

## Root cause: route was aborting before the external test could start

In `native/windows-bridge/Program.cs`, `sourceToneOutput` and `toneStream` were constructed **only for** `--probe-route` (the internal 440 Hz bridge-owned stimulus). Yet the shared `if (provePhysicalRoute)` monitor initialization always called `sourceToneOutput.Init(toneStream)`. For `--probe-app-route` (the independent Windows audio application) both variables were null. The original path would throw a `NullReferenceException` during setup and fail before printing `EXTERNAL_SOURCE_READY`, before the external process could supply any PCM. This also invalidated a no-source control using the same mode. Earlier device-free tests exercised PCM, C++ DSP and the separate tone generator independently, but did not exercise this **mode-selecting bridge lifecycle**.

## Fix — fail closed, never generate an internal test tone in external-source mode

- Add `PhysicalRouteStimulus.Initialize(ownTone, externalAppTone, initializer)`, used by the real production-lab setup branch. Exactly one physical evidence stimulus mode is allowed; internal tone is initialized only in `--probe-route`. External-app mode skips the internal tone and waits for real captured audio from the separately launched executable after `EXTERNAL_SOURCE_READY`.
- Integrate `PhysicalRouteStimulus.RunSelfTests()` with existing `--check-route-tone` CI (run by Windows managed/native DSP workflow). Pure C# negative fixtures reproduce the old null-internal-source error and verify external mode never calls the initializer, internal mode exactly once, and missing/mixed modes rejected.
- No driver installation, elevated Windows settings, sample playback, automatic default endpoint change, microphone, recording or product release is introduced.

## Acceptance / remaining limitations

This closes a **deterministic source-level null-initialization blocker**, not Windows OS audio integration. Build and run exact-head Windows bridge CI including the new `--check-route-tone` test; confirm all native DSP and previously installed negative proof fixtures stay green. Do not claim a source-code CI PASS is physical playback evidence.

Unverified and tracked: signed/installable virtual device, real OS app PCM received by virtual WASAPI loopback, real native gain 100→150 on physical stream, matched before/after silence controls, latency, driver rollback and device compatibility. Those require an isolated operator-controlled VM with actual endpoints. Stable Android v0.1.12 + Windows v0.1.13 unchanged.
