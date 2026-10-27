# LouderMe GRANADA — preventing implicit Windows default-device reassignment

**Date:** 2026-10-27
**Scope:** isolated, consent-only virtual-audio LAB scripts and read-only bridge metadata. No public audio driver or release.

## Root cause and why it matters

The disposable VM helper explicitly never calls a Windows `SetDefaultEndpoint` API. That alone does **not** establish that the default audio device stayed unchanged: when a new render device is installed, Windows audio policy/PnP may change which render endpoint is the default, independently of the program's own actions. The old LAB helper installed SysVAD and only advised the operator to check the default manually **after** cleanup. It could run the physical tone probes before anyone noticed a changed default.

A metadata-only guard now captures a SHA-256 of the ordered triple of **Windows render defaults for Console, Multimedia and Communications roles** before the first privileged driver action. The bridge obtains these via real Core Audio MMDeviceEnumerator; each record includes role and length-prefix to avoid concatenation ambiguity. The raw endpoint IDs and friendly names never appear in the CLI output or evidence files. This hashes the identity metadata, not any audio or credentials.

## Functional deliverables

**A — read-only probe:** `dotnet LouderMeAudioBridge.dll --default-render-fingerprint` now returns precisely one `DEFAULT_RENDER_SHA256=<lowercase sha256>` line on success, with no other arguments permitted. It returns nonzero on missing/inactive default output for any role. This is an actual host OS metadata query; hosted CI **does not** fake a physical device test.

**B — zero-silent-change VM lifecycle:** `run_disposable_lab.ps1` refuses to install if default identity is unavailable; compares after successful driver installation **before emitting sound** and after audio measurements. Its cleanup compares again after removing only the LAB instance created by the current invocation; if the hash differs or cannot be read, it fails closed and instructs the operator to restore the VM snapshot. It writes metadata-only before/after SHA fingerprints to its new one-time session evidence. A mid-run auto-switch that was reverted before measurement can only be detected at sampled checkpoints, not guaranteed continuously.

**C — cleanup blast-radius reduction:** if the run knows its exact created PnP ID, it removes only that instance. If a partial install fails, it may remove a single newly detected LAB instance only because preflight proved none existed before installation. If multiple ambiguous new root instances appear, refuse a blanket removal and require restoring the disposable VM snapshot. The script never installs or removes devices on a real personal computer.

**D — regression gate:** pure PowerShell parser/validator rejects 14 adversarial variants (failed read, missing/default-changed identity, extra/duplicate/raw data and malformed fingerprints). CI executes it without opening a real audio endpoint and syntax-parses all changed helper files. SysVAD source derivation also performs static contract checks that default render identity is captured before `devcon`, asserted before sound, and checked again during cleanup. All native DSP, Android Home/About/Developer and WDK compile checks remain required before merge.

## Boundaries and reentry

This does **not** establish that any driver has been signed, installed, routed to a third-party program, or played audible enhanced sound. It does not change the user's Windows default playback, attempt automatic reset, or remove arbitrary PnP devices. If the default device differs even after LAB cleanup, the only approved recovery is the disposable VM snapshot or explicit human device configuration, not a silent system mutation.

After exact-head CI, the next physical milestone for issues #46/#48 is a snapshot-backed VM with a locally trusted test-signed LAB driver, real virtualization/physical WASAPI endpoint, 100%/150% independent-source gain with quiet pre/post controls, precise rollback and owner review. For production release, Microsoft trust and broader device/latency/volume safety validation remain mandatory.
