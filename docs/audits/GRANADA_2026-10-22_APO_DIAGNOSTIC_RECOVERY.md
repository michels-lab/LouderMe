# Operación Granada — Windows -18 dB listening-test crash recovery

Date: 2026-10-22. Repo: michels-lab/LouderMe. Scope: Windows Equalizer APO legacy fallback only, source+contract QA.

## Root cause
LouderMe Desktop applies a temporary -18 dB attenuation by appending a second Preamp command to its managed Equalizer APO config. A normal async four-second listening test restores the old settings in a C# finally block, but a forced close, OS crash, or killed process does **not** execute finally. An interrupted test can leave playback permanently attenuated after restart. Previous contract tests only exercised a normal finally path, never next-start recovery or conflicts with external edits.

## Linked corrections
1. Apply a distinctive LouderMe-specific temporary probe marker and recover the exact generated marker plus attenuation only. MainForm runs recovery before loading its initial visible status; successful recovery reports that configuration was restored, not that physical audio is verified.
2. Preserve externally edited APO files. If the tag exists but content no longer exactly matches the expected diagnostic, do not overwrite on startup, test exit or later normal Apply. Surface an actionable conflict instead. The -18 dB file is not evidence of global processing, only a consented user-listening probe.
3. Backward compatibility: recognize a v0.1.13 **unmarked** trailing -18.0 dB command only if the entire file matches a generated LouderMe settings configuration plus exactly that known suffix. Never remove an unrelated third-party attenuator.
4. Extend the existing runnable --check-apo-contract with forced-crash-style interrupted probe, one-time restoration, conflicting manual edits, normal Apply refusal, repaired test, legacy exact-match recovery, legacy foreign-edit refusal, and no false-positive recovery on normal files.

## Gates and limits
- Exact-head Windows desktop CI must pass .NET build, existing --check-apo-contract, public/Developer runtime, layout, packaged installer smoke and any source-related workflow checks before integration.
- No driver installed; no APO configuration modified on a real owner computer by CI. Synthetic/temp files only. No microphone/audio data/cloud service.
- The *shipping* public Windows v0.1.13 and Android v0.1.12 remain unchanged until an independently authorized stable release. A source merge alone is **not** an installer/update or physical sound fix.
- A file that differs from the exact expected diagnostic is intentionally left untouched; users must inspect Equalizer APO's own config if recovery is blocked. When the app is not launched again, it cannot restore any previous temporary effect.

## Concurrency guard

The four-second probe captures a private clone of all seven EQ gains and Boost/EQ settings before applying the temporary attenuation. The UI disables all Boost and EQ interaction while measuring. The final restore uses the captured immutable snapshot; a deliberately changed settings object in contract tests must not cause rollback of an unmatched test. The test verifies the original matching snapshot restores correctly. UI events cannot silently convert temporary old gain into a new preset while the test is active.
