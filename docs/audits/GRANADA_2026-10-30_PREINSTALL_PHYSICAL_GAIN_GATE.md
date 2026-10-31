# Granada — Pre-install real physical gain gate in isolated SysVAD VM (2026-10-30)

## Root cause isolation
The lab already has C++ DSP, a bounded physical-WASAPI 100%→150% comparison, a SysVAD-derived virtual render endpoint, and a full independent Windows source → virtual capture → DSP → physical output sequence. However, driver installation previously preceded ANY direct physical gain proof. A silent/incompatible physical output could therefore be confused with a virtual-driver/PCM transport problem after privileged installation. The virtual endpoint's actual non-silent loopback remains **unverified**.

## New fail-closed VM sequence
Only inside the existing self-hosted, snapshot-backed disposable Windows VM with explicit `I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST` consent:

1. Validate administrator and hypervisor, empty owned LAB devices, trusted local CAT, seven immutable SHA-256 inputs, original Windows default render identities, and unique new evidence directory.
2. **Before any Devcon driver installation:** run `--probe-direct-gain --output-id <EXACT_PHYSICAL_ID> --confirm-experimental` through the bounded, sanitized subprocess helper. It uses the real C++ DSP and **same selected physical WASAPI loopback** for two quiet 2-second 440 Hz phases with Boost 100% then 150%.
3. Require both the bridge's actual process exit 0 and exactly one final `status=PHYSICAL_LOOPBACK_NATIVE_GAIN_AB_PASS` evidence line. Anything else stops the experiment without installing SysVAD. Retain a privacy-sanitized `direct-physical-gain-before-driver.log`, no PCM recording.
4. Re-hash the seven immutable lab inputs before/after and reconfirm all Windows default endpoint identities remain unchanged. The two-pass subprocess has its own bounded **40-second deadline**.
5. Only after these physical-sink prerequisites pass may the VM attempt to install the locally trusted LAB driver, prove virtual loopback, prove independent process PCM and the A/B routed-gain control with before/after source-free negatives, and remove only its own PnP instance in `finally`.

## Interpretation
- Pre-driver physical A/B pass isolates that the owned DSP and selected output can reproduce a measurable 440 Hz **software loopback** gain comparison, which prevents later misdiagnosing *that* subsystem as SysVAD.
- **Still not proven:** loudspeaker acoustic output, signed consumer driver, non-silent SysVAD render loopback, arbitrary application interception, zero echo, system-wide Boost/EQ, or release readiness.
- Hardware validation is **not executed by hosted CI**. Windows CI only parses scripts and performs device-free safety-contract tests.
- If a pre-driver gain check fails, inspect the physical output, routing, Windows mixer/format, DSP DLL and other playing audio *before* investigating the virtual source. Do not automatically install or switch devices.
- Requires a VM with real mapped physical WASAPI playback and quiet volume; do not run this on the user's primary machine. No release created; issues #46/#48 stay open.
