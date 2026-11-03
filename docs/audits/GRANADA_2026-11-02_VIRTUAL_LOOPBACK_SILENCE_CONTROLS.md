# Operación Granada — 2026-11-02: source-free virtual endpoint before/after controls

## Problem
PR #134 added a real *separate-process* Windows stimulus at the explicitly selected virtual render endpoint. Its matched 440-Hz positive record was accepted without measuring the same virtual WASAPI loopback with the source absent immediately before and after. Unrelated or lingering 440-Hz playback could therefore produce a false causal conclusion even if the bridge never synthesized its own tone.

## Implemented sequence, disposable Windows VM only

1. After installing a locally trusted LAB SysVAD package, the bridge executes `--probe-virtual-external` for its full five-second receive-only window with **no separate tone process**. This is a real virtual loopback capture; it must return failure code 3 for the missing tone, exactly one post-capture numeric report and the precise no-tone diagnostic. The source process must never start in the control.
2. `VirtualSourceSilenceCheck.ps1` accepts only the expected failure and one `EXTERNAL_VIRTUAL_READY` marker, requires finite numeric samples and matched amplitude between **0 and 0.003**. Zero reported frames is allowed for an idle Windows loopback endpoint *only* because capture had started and run to its normal post-stop report. It cannot by itself establish a usable positive stream.
3. A separate `LouderMeIndependentTone.dll` process sends its own two-second low-volume 440-Hz stimulus to the virtual render endpoint. Its own successful completion and the bridge's positive measured 440-Hz identity are checked.
4. After both child processes finish, the same five-second receive-only probe runs with **no source**. The post-control must also reject any residual 440 Hz. No source-free proof may be substituted for the required positive PCM proof.
5. Source-free `virtual-no-source-before.json`, `virtual-no-source-after.json` and `independent-virtual-source-proof.json` are separately write-once and bound to the same session GUID, virtual endpoint ID hash, bridge DLL and source application DLL hashes. The completed 100%/150% physical-route proof requires all three, plus its own no-stimulus physical controls, under `-RequireVirtualProof`.
6. Failures stop the VM lab **before** it claims complete SysVAD → native C++ DSP → physical WASAPI processing. No default audio endpoint changes, kernel installation, audio file recording, microphone, or non-LAB PnP removal are added here.

## Testing boundaries

- `test_VirtualSourceSilenceCheck.ps1`: device-free adversarial fixtures for fake readiness, duplicate/absent evidence, a matched 440-Hz residual, invalid values, wrong errors and unexpected successes.
- `test_IndependentGainCheck.ps1`: rejects pre/post virtual residual, missing after control, swapped session/binary/endpoints, source started in control, invalid numeric fields and overclaim.
- Windows CI runs both PowerShell test suites and parses the new source. Python source-boundary tests ensure ordering.
- **No actual SysVAD installed device, independent PCM transport, signed production driver or acoustic loudness is proven in CI.** A truly isolated Windows VM with a legally/locally trusted signed lab driver and physically available WASAPI output remains mandatory. Issues #46/#48 stay open.

No release was produced. Stable distributions are unchanged.
