# Operación Granada — Live Windows default-render route fence

Date: 2026-11-10.

## Functional change

LouderMe's opt-in `native/windows-bridge` experiment selects **one exact virtual WASAPI capture endpoint** and **one exact physical render endpoint**. Even when both remain Active, Windows can change any of its three independent *default output* roles (Console, Multimedia or Communications) while this experiment runs. Without detection, new application streams could bypass the virtual device, and mixing physical direct + bridge output could be dangerous or confusing.

`DefaultRenderRouteFence` now snapshots the immutable **endpoint IDs** for all three roles before WASAPI streams open, rechecks every second on the management thread during active routing and fails closed if any role changes or becomes unreadable. Friendly names are never accepted as identities. The safeguard requires no admin privileges, records no raw endpoint IDs or audio and never changes Windows' default routing itself.

If Windows changes a role, the bridge's existing `finally` stops its owned capture and render devices and releases the DSP. The failure is terminal **exit 76** (distinct from bounded recoverable exit 75), so `--auto-reconnect` cannot resume DSP after a user/system-selected new default. For ordinary recoverable disconnects (exit 75), the route fence remains pinned across all three allowed attempts. A fresh explicit launch is required after a default change.

## Coverage and limits

`--check-route-preflight` now runs device-free role snapshot tests: stable roles, drift of each of the three roles independently, changed endpoint ID under the same friendly name, missing/empty roles, and retained baseline across simulated reconnect. The Windows managed-native bridge CI already invokes this command, so its published PASS must be from the exact source SHA.

Physical QA still required: run only in an owner-consented disposable Windows VM with a separately trusted LAB driver; under a real independent Windows app playing to the virtual endpoint, change any default role during playback and confirm prompt terminal 76, silence of the bridge-owned output, and no automatic restart or default reassignment. Restore VM snapshot afterward.

**This does not create a production signed audio driver, prove sound-pressure change, force apps into the virtual endpoint, modify Windows default playback or replace the physical proof in issue #48.** A role changed and restored entirely between management polls might not be observed. No RC or release.
