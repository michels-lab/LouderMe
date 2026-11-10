# Operación Granada — responsive Windows seven-band EQ editing

Date: 2026-11-10. Scope: Windows desktop equalizer UI and its existing Equalizer APO compatibility backend. No signed driver or audio recording.

## Problem

The interactive EQ curve previously forwarded every small mouse movement into a `NumericUpDown.ValueChanged` handler, which synchronously serialized the entire desktop settings JSON, read/wrote the Equalizer APO config and refreshed its status **on each step**. This causes potential mouse-pointer hitches, repeated disk writes, flicker, and interrupted partial state while dragging.

## Implemented behavior

- `EqCurveControl` now exposes `IsDragging` and emits `BandEditCompleted` once when a changed drag ends on MouseUp or when mouse capture is lost. It still visually updates the curve and numeric bands on every movement. No extra Windows shell dependencies.
- The desktop UI schedules numeric-band edits through a 180 ms WinForms timer. Multiple edits consolidate into one settings/APO apply. If a graph drag is active when the timer fires, it does not apply a partial graph snapshot; the finish event commits the final state.
- Numeric `Leave` immediately flushes pending keyboard/step edits. `FormClosing` force-flushes the last edit even with a captured pointer. The timer is disposed with the form. A new Boost button/preset/effect toggle still applies immediately, cancelling any queued prior edit without losing the current in-memory values.
- `EqualizerCommitPolicy.RunSelfTests()` executes under the existing Windows `--check-apo-contract` build gate, covering suppression of writes during graph drags, final flushes and no-op when there is no pending edit.
- No user-selected EQ values, presets, band count, dB bounds, version, installer, update feed or native driver code is changed.

## Validation

Run exact-head Windows CI: compile and install LouderMe, `--check-apo-contract`, settings and layout smoke tests and installer provenance. Manual Windows acceptance: drag a 60 Hz through 12 kHz band continuously, release outside the graph, edit by keyboard and change presets quickly; verify the last values survive restart and match the applied APO file; ensure there are no intermediate writes during held drags.

**Limits:** Windows CI cannot prove actual acoustic/system-wide processing. APO presence/config sync is not physical evidence. The independent virtual-driver/audio proof remains tracked as issue #48. No RC, signed package or stable release authorized.
