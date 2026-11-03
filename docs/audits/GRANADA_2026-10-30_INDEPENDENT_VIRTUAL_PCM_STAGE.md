# Operación Granada — independent process → virtual render WASAPI loopback only

## Source-derived root-cause hypothesis
At pinned Microsoft SysVAD commit `2dc3fd3a0cc84a2933f2194e7ec0871584979071`, the sample's `EndpointsCommon/minwavertstream.cpp` capture stream calls `WriteBytes(ByteDisplacement)`, backed by a `ToneGenerator`. Our lab derivation disables that example's tone generator. The inspected driver source does not show a straightforward render-buffer-to-capture-buffer copy. **Important:** Windows Audio Engine may still provide an actual shared-mode loopback PCM stream. Source reading alone cannot conclude that physical Windows playback or virtual loopback is silent. A real endpoint test remains essential.

The VM lab previously had an internally generated `--probe-virtual` (same bridge process), then went to the DSP + physical sink. It lacked a separate causal stage proving **PCM from another executable** reaches the named *virtual* endpoint independently of bridge DSP/output. This created a large ambiguous failure surface.

## New functional separation

1. In a disposable Windows VM, with explicitly approved, locally trusted lab driver and after the preinstall native DSP/physical A/B safety gate, enumerate the newly installed LouderMe virtual endpoint.
2. Keep the existing internal `--probe-virtual` as a baseline.
3. Run `probe_independent_virtual_pcm.ps1`. This launches `LouderMeAudioBridge.dll --probe-virtual-external --source-id EXACT_VIRTUAL_ID --confirm-experimental`. The bridge opens **only** WASAPI loopback capture; it neither generates a tone nor constructs `WasapiOut`, a native DSP, or a physical endpoint. It prints `EXTERNAL_VIRTUAL_READY` only after capture starts.
4. A distinct `LouderMeIndependentTone.dll --device-id EXACT_VIRTUAL_ID --external-app-source --confirm-experimental` emits a low-amplitude 440-Hz tone for two seconds using shared-mode WASAPI into that named virtual endpoint.
5. Only if source exit 0, real bridge exit 0, exactly one readiness marker, one completion and one bounded 440-Hz proof with matched amplitude/coherence and positive frame count are present does the VM write a **session-bound, write-once, numeric evidence file** `independent-virtual-source-proof.json`. File contains hashed endpoint/binary identities only, no PCM or Windows endpoint names. Timeouts and child process cleanup bound each phase.
6. If that virtual-only stage fails, **do not start** the physical/bridge route tests. This isolates missing SysVAD/WASAPI virtual-loopback transport from the already independently checked DSP and physical sink.
7. Once accepted, continue with the existing full independent-app → virtual endpoint → native C++ DSP → physical WASAPI loopback A/B and before/after no-stimulus controls, then uninstall only the owned LAB PnP instance.

## Validation and non-claims

- Windows native CI builds the updated bridge, parses the full lab script and runs device-free `test_LabInputIntegrity.ps1`; virtual-driver CI runs Python source boundary assertions.
- This work does **not** install a driver on hosted CI, prove that virtual PCM exists on a real Windows device, certify audible acoustic loudness, support system-default switching, or imply all third-party applications will work.
- Owner-confirmed disposable-VM proof and driver signing remain mandatory for issues #46/#48. Never auto-install on the user's normal workstation.
- No Android or Windows stable release will be created from source-only/hardware-free evidence.
