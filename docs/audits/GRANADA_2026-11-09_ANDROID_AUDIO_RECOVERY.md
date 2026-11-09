# Operación Granada — Android audio control, restart and EQ recovery (2026-11-09)

## Root causes identified in the source

1. `SessionZeroAudioEngine.enable()` treated any successfully created LoudnessEnhancer as the final choice even when `setEnabled()`, `enabled` or `hasControl()` did not prove a controllable effect. The DynamicsProcessing fallback was never tried for this common degraded result.
2. Persisted seven-band EQ parsed values with `mapNotNull()`, silently shifting subsequent bands left when one token was invalid. NaN/Infinity also bypassed safe `coerceIn()` handling.
3. The recreated foreground service could have saved `ATTACHED` state but no live `AudioEngine`; an EQ command then attempted the EQ without restoring Boost. The notification's preset label did not update after EQ-only changes, and a graceful stop could leave stale attached state persisted.

## Implemented (source branch; not yet released)

- Only keep LoudnessEnhancer when it explicitly reports a successful enable, enabled state and control. Otherwise disable/release it, attempt DynamicsProcessing, and similarly require all attachment signals before continuing. If neither can be controlled, release both effects and report `UNSUPPORTED`; never show a false active Boost.
- Share the attachment-evidence rule between the Boost engine and SessionZeroEqualizer. Preserve the explicit compatibility limitation: session 0 attachment is NOT proof of global application processing.
- Parse stored EQ band values positionally; malformed tokens become 0 dB at their original frequency. Bound gains to [-10,+10] dB and replace all non-finite values with 0 dB. Retain selected preset when no custom stored value exists.
- On `ACTION_SET_EQ`, reattach Boost if the system recreated the foreground service without a live engine; prevent an EQ-only false recovery. Refresh the ongoing notification whenever the EQ changes. Persist OFF after a graceful, running-service teardown.
- New JUnit regression tests for three-way attachment confirmation, malformed-band positional integrity, non-finite inputs, preset restoration and bounds.

## Verification/evidence limits

- Automated Android Gradle unit/build checks are executed by the repository's existing `android-ci-release.yml` pull request workflow; check its exact branch SHA and conclusion before merging. Local physical-device service and EQ behavior have NOT been tested in this session.
- Validate on a physical Android device: deny/revoke effect ownership while enabling Boost, verify fallback to DynamicsProcessing or explicit UNSUPPORTED; kill/recreate the service and change the EQ preset; verify notification and persisted status; confirm external playback truly changes (only on supported devices).
- No changes to Android app versionCode/versionName, signer, updater, Windows audio driver, Play/Direct release feeds or published binaries.
- This PR intentionally does not modify `PROJECT_LOG.md` while the separate Windows speaker-map PR #156 edits it. Keep the pending Windows independent source→physical loopback and signed driver gates tracked under issue #48.

## Reentry

Review the Android CI checks of this PR. If full build passes, merge only when source and workflow gates are green; then arrange physical-device validation before any stable release. Review issue #48 and PR #156 independently.
