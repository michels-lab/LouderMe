# Operación Granada — Windows LAB evidence freshness and privacy

Date: 2026-10-21. Product: LouderMe Windows virtual audio. Status: candidate; physical VM NOT RUN.

## Root cause and scope
Previously, a reused evidence directory could retain old phase files after an interrupted session. Phase outputs and driver-probe logs used overwriting writes in several locations. WASAPI endpoint IDs from list-devices could also be copied into uploaded runner logs. This was not an audio DSP bug; it was a provenance and privacy gap in physical validation.

## A. Before driver installation
Require a previously nonexistent evidence directory, reject even old empty directories, generate GUID session lock and use a unique run-ID + run-attempt directory on manual VM workflows. Any prior run is preserved, not silently recycled.

## B. Per-phase immutable files
All session logs, metadata, 100/150% physical proof JSON, before/after no-source controls and ratio use FileMode.CreateNew. A mismatched lock or existing phase file aborts the independent process probe before making new evidence.

## C. Privacy, tests and release
Only in-memory device enumeration retains full virtual and physical endpoint IDs. Archived runner logs redact IDs. The hardware-free contract fixture tests duplicate folders, existing files, attempts to overwrite records, preservation after failure and redact-without-damaging-metrics behavior. Native Windows CI runs those fixtures and parses PowerShell scripts. Real-device proof, WDK-certified signing, visual/installer acceptance and arbitrary-app audio are NOT attested by hosted CI.

Release unchanged: Android v0.1.12 + Windows v0.1.13. No pre-release; no consumer native driver shipment. Issues #46/#48 remain open.