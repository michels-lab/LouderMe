# Granada 2026-11-09: maintain identical Windows source format throughout 100/150 Boost A/B

## Functional defect
Native DSP's experimental physical test compares real separate-app virtual-to-physical audio amplitudes at 100 and 150 percent. Previously it accepted the same endpoint and executable across both runs, but did NOT compare the PCM source channel layout or source frame count. A 2-channel vs. 5.1/7.1 source can naturally produce different physical stereo amplitudes from the downmix without a change in DSP amplification. Such a mismatch cannot establish the required 1.30–1.70 ratio.

## Code changes
- Both physical positive runs must use the same source channel count and source frame count before amplitude ratio is computed; restrict to actual supported 1/2/6/8-channel source formats.
- When the real disposable Windows VM requires earlier independent virtual-only PCM proof, verify source channels and two-second frame count are finite, whole numbers and consistent with the real independent source's 8–192 kHz constraints; BOTH physical positive runs must match that virtual source format.
- Preserve all existing checks: session ID, endpoint and binary identities, no-source before/after controls, physical render consumption, dropped packet rejection, matching 440 Hz coherence and amplitude ratio.

## CI
Hardware-free PowerShell self-tests now prove positive same-source 5.1 and 7.1 cases, and reject 2ch/6ch or 48/44.1 kHz differences, invalid 3/4/5/7-channel formats, missing/forged virtual frames and channel counts, and virtual 5.1 proof incorrectly tied to stereo physical runs.

## Evidence limitations
This is a real fail-closed execution gate for future signed-driver tests; hosted CI exercises software fixtures and does NOT install a driver, access loudspeakers or demonstrate global amplification. Issue #48 remains open. No release or RC.
