# Granada 2026-11-08 — independent Windows 5.1/7.1 source lab proof

## Real functionality
The independent Windows executable (a separate process with no DSP) previously refused virtual endpoint formats above two channels. The native bridge also rejected all multichannel source formats in the external-app physical route test, even though a 5.1/7.1 to stereo PCM adapter is now implemented.

## Implementation
- Accept real PCM16/24/24-in-32/float32 5.1 back/side and 7.1 extended speaker layouts in the independent 440Hz virtual endpoint app, while retaining a bounded low 0.025 amplitude and exact endpoint selection.
- Run 44.1/48/96k x 1/2/6/8 x PCM format vectors and per-lane byte equivalence with negative unknown/legacy surround masks in the real Windows --check-tone selftest.
- Allow 6/8 source channels ONLY for the external-app proof mode through the owned C++ DSP and physical stereo downmix. Internal physical proof keeps its original mono/stereo limitation.
- Update virtual-only separate-process lab proof success-marker validation for 6/8 channels. No new driver install, microphone, cloud or sound recording.

## Not proven
CI only validates code and source data. A real installed SysVAD endpoint must advertise 5.1/7.1 before an operator can run a signed-driver disposable-VM loopback/physical test. Do not confuse lab test capability with passing live sound acceptance or shipping product. #48 remains open. No RC/release.