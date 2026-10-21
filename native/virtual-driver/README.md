# LouderME Virtual Render Endpoint — WDK lab prototype

**Status: real driver SOURCE DERIVATION + WDK build experiment, not a signed or installed product.**

This work derives a **single render-only virtual device** from Microsoft's
SysVAD reference at pinned commit `2dc3fd3a0cc84a2933f2194e7ec0871584979071`.
The upstream project is distributed under the **Microsoft Public License
(MS-PL)**. The derivation script checks that license and copies the full MS-PL
text alongside any patched source. All Microsoft copyright and attribution
notices remain intact. Reproduced/modified source must preserve MS-PL terms.

The derivation modifies the local checkout, **not** the official upstream
repository. It does not publish a signed driver or install an audio device.

## Actual changes to the Microsoft driver source

- Hardware ID becomes `Root\LouderMe_VirtualRender_Lab` (isolated from
  unmodified SysVAD and other vendors' drivers).
- Manufacturer, device, service and speaker-friendly labels identify
  **Michel's Lab / LouderME Virtual Audio (Lab)**.
- Endpoint miniport list contains only the virtual **speaker render** endpoint.
  No sample capture/microphone endpoints are installed; unrelated demo render
  miniports (headphone/HDMI/SPDIF) are excluded.
- Optional SysVAD render-to-disk saving is **permanently disabled** and its
  registry override is removed; the sample-generated test-tone feature is
  disabled so sound-proof tests cannot mistake synthetic driver tones for
  PCM originating from Windows playback clients.
- The selected virtual speaker is configured with a loopback-support flag,
  allowing the existing WASAPI bridge to investigate its actual PCM stream.
  **This flag is not evidence that virtual SysVAD supplies non-silent PCM to
  loopback. Test on real Windows hardware before claiming it works.**
- The WDK project now explicitly targets **Windows Desktop**, rather than
  the reference sample's Universal driver model. This is a deliberate
  product-scope decision: LouderME targets Windows desktop PCs, not IoT/Core
  or other Universal platforms. The required Desktop/WHQL INF validation
  uses `InfVerif /h`; package signing, runtime functionality and applicable
  Microsoft signing requirements are still mandatory.
- The sample extension INF remains in source for upstream build consistency
  but **must not be installed**: it contains Microsoft's demo APOs.

## Source preparation

Requires Python 3.11+ and an ordinary Git checkout of upstream source at the
pinned commit. The CI workflow checks out that revision automatically.

```powershell
git clone https://github.com/microsoft/Windows-driver-samples.git upstream
git -C upstream checkout 2dc3fd3a0cc84a2933f2194e7ec0871584979071
git -C upstream submodule update --init --recursive
python native/virtual-driver/prepare_sysvad.py --upstream-root upstream
```

The script refuses to operate on a non-pinned revision or a source tree
without the Microsoft Public License. It writes a machine-readable
`LOUDERME_LAB_MANIFEST.json` that explicitly reports:

```text
render_endpoints=1
capture_endpoints=0
driver_signed=false
driver_installed=false
real_audio_verified=false
```

## Building with WDK

Windows 11 x64 development environment needs matching Visual Studio
C++ build tools, Windows SDK, WDK headers/libraries, the WDK NuGet packages,
and upstream WIL submodule. SysVAD contains dependencies beyond its driver
project, so compile its solution rather than a bare .cpp file:

```powershell
nuget restore upstream/packages.config -PackagesDirectory upstream/packages
msbuild upstream/audio/sysvad/sysvad.sln /m /p:Configuration=Release /p:Platform=x64
```

**WDK qualification status:** earlier Universal-target attempts compiled
the `.sys` but failed the separate Universal `ApiValidator` stage.
For this Windows-only product we now build the explicit **Desktop** target
instead. Microsoft's Desktop classification does not require achieving
Universal API compatibility; it does require its own `InfVerif /h`,
compatible system APIs, proper INF/CAT packaging, Microsoft-trusted driver
signing and real-device testing. Universal compatibility can be investigated
later without mislabeling the consumer target.

**Updated qualification (2026-10-11):** the full WDK helper has now passed
on a provisioned GitHub Windows lab runner (example run
[38127383524](https://github.com/michels-lab/LouderMe/actions/runs/38127383524)):
compiled x64 kernel SYS; officially stamped INF; completed `InfVerif /h`
and `Inf2Cat` on the unsigned lab package. This proves packaging and
build reproducibility, **not** signing, installation, a usable audio endpoint,
non-silent PCM from other applications, or physical speaker processing.

The workflow `native-virtual-device-ci.yml` validates pinned Microsoft
source derivation in Ubuntu, x64 WDK compile and full INF/CAT validation
on Windows. Separate manually provisioned Windows lab machine/VM evidence
remains mandatory for driver installation and real audio acceptance.

**Do not install, publish or add the unsigned/test-signed driver to stable
LouderME installers.** Building the WDK solution may generate unsigned or
development-signed artifacts; that is not sufficient for consumer deployment.

## INF/CAT preparation on a complete Windows driver lab (unverified)

Microsoft's [WDK setup guidance](https://learn.microsoft.com/en-us/windows-hardware/drivers/download-the-wdk)
calls for matching SDK/WDK components. On a **separate lab machine** with
Windows 11 and Visual Studio 2026, install the official WDK if missing
(for example, `winget install Microsoft.WindowsWDK.10.0.28000`; follow
Microsoft's instructions for the matching SDK).

After the upstream source is prepared and the kernel `.sys` is compiled
in x64 Release, use the **validation-only** helper:

```powershell
pwsh native/virtual-driver/validate_lab_package.ps1 `
  -SysVadRoot upstream/audio/sysvad `
  -OutDir upstream/louderme-lab-package
```

It requires real `StampInf.exe`, `InfVerif.exe` and `Inf2Cat.exe`
and **fails closed** if the INF violates Windows requirements, references
missing files or cannot produce an unsigned CAT. It preserves the exact
LouderME hardware ID and checks the source manifest. It does not install
the driver, touch defaults or sign it for distribution.

**Not yet run on a fully configured driver lab.** This script alone does
not satisfy applicable Windows driver API requirements, Microsoft's driver signing,
installation, rollback or real PCM loopback acceptance.

## Device bring-up acceptance (pending, not yet passed)

A dedicated **Windows test VM or lab PC**, not a production workstation,
must validate these conditions with properly signed test binaries:

1. Driver package INF passes `InfVerif` / WDK package validation; service
   starts and exposes exactly one **render** device, no microphone.
2. Verify endpoint hardware-ID and label in Windows PnP, and that uninstall
   restores the original output device.
3. Emit a known sine-tone test signal **to the virtual endpoint itself**.
   Confirm it produces non-silent, correctly timestamped PCM via **WASAPI
   loopback of that same virtual render endpoint**. If SysVAD's virtual
   ring buffer returns silence, implement the missing render-to-loopback
   mechanism first; a device icon alone is not success.
4. With **only a lab virtual device installed**, use the bridge's dedicated
   **signal proof** mode. The test program synthesizes a low-amplitude 440 Hz
   signal and directs it to the virtual render endpoint selected by ID; it
   measures loopback PCM from that same endpoint without routing audio
   to any physical speaker:

   ```powershell
   dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --list-devices
   dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-virtual --source-id "<LOUDEME_VIRTUAL_ENDPOINT_ID>" --confirm-experimental
   ```

   A passing measurement now requires **440 Hz tone identity** using both
   sine/cosine projections (unknown loopback phase) and minimum matched
   amplitude/coherence. It rejects 880 Hz, DC offsets, silence, short
   captures, nonfinite/misaligned float PCM and unrelated energy.
   Device-free `--check-loopback` regression tests run in Windows native
   bridge CI. This is stronger than a generic nonzero RMS, but still **not
   proof** that processed audio from another application reached speakers.

5. **New device-lab route instrument (2026-10-11):** first pass the
   virtual 440 Hz identity probe, then use `--probe-route` with a
   **different named physical endpoint** in a disposable Windows VM,
   low physical volume and no unrelated media. It emits a quiet virtual
   stimulus, runs the actual native C++ DSP, measures the independent
   physical WASAPI loopback and reports matched-440 amplitude/coherence.
   In-memory CI for the new stimulus is mandatory, but cannot substitute
   for an installed driver, physical sink or audible routing.
   ```powershell
   dotnet native/windows-bridge/bin/Release/net10.0-windows/LouderMeAudioBridge.dll --probe-route --source-id "<VIRTUAL_ID>" --output-id "<PHYSICAL_ID>" --boost 150 --confirm-experimental
   ```
   Confirm native gain ratios, real Windows-app-origin PCM, no echo/double
   playback and physical listening using separately controlled test
   evidence; the route probe alone does not prove all of these.
6. Test crash/unplug/reboot recovery, physical device switching,
   buffer underruns, playback latency, CPU, 44.1/48/96 kHz sample rates,
   and format conversion. Restore original defaults on uninstall.
7. Obtain appropriate Windows signing/distribution authorization and perform
   an isolated production installer validation. Explicit user consent is
   mandatory for any driver install/device switch.

## Safety

No driver installation, default-device modification, registry write, audio
capture or replay is performed by the preparation script or CI. Only the
separately launched experimental Windows WASAPI bridge can attempt audio
capture/replay, with explicit source/output IDs. Normal stable LouderME
continues using the documented Equalizer APO fallback.


### Reproducible disposable VM bring-up and removal helper (new)

The repository includes `native/virtual-driver/run_disposable_lab.ps1`. The
script is deliberately **not** invoked by hosted CI. It can install the
already prepared **locally trusted lab-signed** INF package via WDK
`devcon.exe`, wait for the one new root device, run both the virtual and
physical 440 Hz WAV/PCM probes, save console-only proof (no raw audio) and
remove all newly created LAB device instances in a `finally` block.

Hard preconditions: a snapshot-backed **disposable Windows VM** with audio
pass-through, elevated PowerShell 7, actual physical playback endpoint
visible to that VM, locally trusted test-signed `LouderMeVirtualRenderLab.cat`,
`TabletAudioSample.sys`, `LouderMeVirtualRenderLab.inf`, WDK
`devcon.exe`, the published `.NET 10` lab bridge DLL and
`louderme_audio_c.dll` placed beside it. The script does **not** generate,
sign or install an unsigned package, enable Windows test mode, disable
Secure Boot, switch default devices, remove other drivers or run on hardware
it cannot identify as a VM. Administrators must establish a trusted lab
signing/test policy themselves; production release will require its own
Microsoft trust/certification path.

Example **only inside that VM** after reviewing actual local paths:

```powershell
pwsh -NoProfile -File native/virtual-driver/run_disposable_lab.ps1 `
  -PackageDir "C:\lab\louderme-lab-package" `
  -DevconExe "C:\lab\devcon.exe" `
  -BridgeDll "C:\lab\LouderMeAudioBridge.dll" `
  -PhysicalEndpointId "<PHYSICAL_RENDER_ID_FROM_LIST_DEVICES>" `
  -LabConsent I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST
```

`-PhysicalEndpointId` comes from the bridge's `--list-devices` command
executed **inside the VM**. First run `--probe-virtual`, then
`--probe-route` automatically. A real installed LAB device is required;
there is no simulated audio-pass mode. After collection, verify that Windows
still uses the intended physical default device, then restore the **VM
snapshot**. If PnP cleanup reports an error, snapshot restore is mandatory.

For remote repeatability there is a **manual-only** workflow:
`.github/workflows/native-physical-lab-manual.yml`. It deliberately runs
**exclusively** on a self-hosted Windows runner with the
`louderme-isolated-audio-lab` label assigned to a genuinely disposable
snapshot-backed Windows VM. It requires explicit consent and absolute paths
to the VM's **pre-signed** LAB driver package, WDK devcon.exe, .NET bridge
and physical endpoint ID. Nothing executes on a GitHub-hosted Windows runner;
a missing lab runner leaves the manual workflow unstarted/queued, **not
passed**. Credentials or private code-signing keys are never workflow inputs;
test audio is not uploaded.

**Execution status: NOT RUN.** Neither a signed package installed on a real
lab VM nor actual physical playback evidence is currently available through
connected tools. PowerShell *syntax* is checked in CI without executing any
privileged installer step. A failed or missing physical test is never reported
as a working driver.

**Never report “native global audio works” until physical proof is recorded.**

## Independent Windows process input acceptance (Granada, 2026-10-21)

New **candidate** lab gate requires more than an internally generated probe tone: `native/independent-tone/LouderMeIndependentTone.csproj` builds a distinct Windows executable with no LouderMe DSP and no driver-management code. After the bridge emits `EXTERNAL_SOURCE_READY` from its active virtual capture and separate physical render monitor, the isolated lab runner plays exactly two seconds of low-volume PCM to the selected virtual device. The bridge itself synthesizes **no tone** in `--probe-app-route`, so a silent/incorrect SysVAD virtual render loopback or broken end-to-end signal transfer fails rather than producing a misleading PASS. Numeric output is checked by the physical WASAPI loopback; logs don't store raw audio.

Run only through `run_disposable_lab.ps1` in a trusted, snapshot-backed VM, supplying the additional `-IndependentToneDll` parameter alongside `-BridgeDll`, signed driver package and exact selected physical endpoint ID. The manual self-hosted VM workflow now requires `independent_tone_dll`; hosted CI cannot create a signer or install a driver. Source CI builds and validates the external executable's **hardware-free** PCM, checks the PowerShell lab scripts' syntax and runs fail-closed tests to ensure the external probe does not accidentally switch to bridge-generated tone. **This is NOT yet hardware passed, and not yet third-party generic app audio acceptance.**

**Stronger two-pass native gain acceptance:** The disposable VM now runs the independent source process at both **100%** and **150%** Boost against the exact same virtual/physical endpoint IDs. The bridge's real physical 440 Hz matched amplitude is independently recorded for each run as `independent-source-result-100.json` and `independent-source-result-150.json` (no PCM waveform retention). The lab requires their physical amplitude ratio to fall between **1.30 and 1.70**, otherwise it FAILS even if both tones were individually detected. That is a conservative controlled-route gain check, not a calibrated acoustic SPL measurement or universal application compatibility test. The lab still cannot run on hosted CI or without a locally trusted installed driver.

## Granada independent application physical gain evidence validator (2026-10-21)

`native/virtual-driver/IndependentGainCheck.ps1` is the **pure numeric proof gate** reused by the snapshot-backed VM run. It parses `independent-source-result-100.json` and `independent-source-result-150.json` from independent Windows app→virtual WASAPI→native DSP→physical loopback measurements and fails closed if one is missing, corrupt, falsely labeled, nonfinite, physically silent, low-coherence, incorrectly scoped or has a ratio outside 1.30–1.70. It never reads raw PCM or opens an endpoint. The corresponding `test_IndependentGainCheck.ps1` uses adversarial JSON fixtures in normal Windows native CI, **no signed driver or physical sound required**. The VM script writes `independent-app-boost-ratio.txt` only after the verifier approves the actual pair. Passing numeric CI fixtures does not establish installed-driver sound, calibrated loudness, arbitrary third-party apps, headphone limits, or production release.

An evidence-based **one-assistant, multi-lens** Perspective Decision Record lives at `docs/product-reviews/2026-10-21-native-windows-audio-evidence.md` with strongest counterarguments, user journey implications, owned APO versus SysVAD decision options, measurable test gates and explicit deferred AI features. This is a review, not autonomous agents, installed QA, or a shipping decision.

## Granada 2026-10-21 — no-stimulus controls bracket real independent-source PCM acceptance

A detected 440 Hz tone alone is **not causal evidence** that the separate Windows application produced it. Another stream, persistent buffer, or a compromised/stale lab trace could create a false-positive. The isolated, explicitly consented VM runner now executes the SAME `--probe-app-route` with **no source process launched** before and after the 100%/150% external app measurements. It requires the bridge to announce `EXTERNAL_SOURCE_READY` (actual virtual-capture and physical-monitor readiness), complete its real measurement window, output one fully parsed physical-loopback measurement with nonzero listener frames and no dropped samples, and return specifically its documented **missing tone** rejection. Background or residual 440 Hz matched amplitude over `0.003` forces the whole VM lab to FAIL.

Each successful negative control stores a small `no-source-control-before.json` or `no-source-control-after.json` declaration (no audio recording or endpoint IDs). A missing driver, disconnected device, an interrupted/aborted process, no monitor samples, nonfinite data, an unexpectedly successful route or pre-existing/stale control file **cannot count** as a negative-control PASS. The disposable VM's original device-instance-scoped cleanup still runs even when controls fail.

This is a **source-level and VM acceptance procedure**, not a measured success on hardware: hosted GitHub CI only parses the scripts and runs the 11 hardware-free adversarial fixtures in `test_NoSourceControlCheck.ps1`. A real signed virtual driver, VM with a reachable physical playback endpoint and owner-approved execution remain prerequisites. Silence-control tests do not certify acoustic SPL or processing from arbitrary apps.

## Granada 2026-10-21 — session-bound, cross-phase evidence acceptance

The isolated VM now generates a **fresh nonempty GUID** for each driver bring-up. The same `-SessionId` travels through the *before-silence*, external-app **100%**, external-app **150%** and *after-silence* probes. Every evidence JSON includes the session GUID and SHA-256 fingerprints of the virtual endpoint ID, physical endpoint ID, bridge DLL and independent-app DLL. **Endpoint IDs themselves are not written** to these evidence JSON files. Changes to devices, binaries or session between phases fail the final validation.

`Test-LouderMeIndependentGain -EvidenceDir <LAB_EVIDENCE_DIR> -ExpectedSessionId <LAB_SESSION_GUID>` now requires **all four distinct records** and validates both negative-control scope, matched 440 Hz silence (maximum 0.003), nonzero listener frames and the original 100%→150% physical-amplitude ratio (1.30–1.70). It cannot accept just two disconnected `PASS` result files. The GUID is recorded in `lab-metadata.txt`; it is a provenance cross-check, **not an unforgeable attestation**, and SHA-256 fingerprints do not independently certify that the driver is installed. Hardware-free CI fixtures test corrupted phases, mixed GUIDs, endpoint/binary changes, missing silence evidence and falsely inflated evidence claims.

Do **not** run `probe_independent_app_route.ps1` outside the disposable VM, use a reused evidence directory, infer human-audible SPL from PCM loopback, or publish a native driver from these tests. The actual signed-driver VM experiment and owner-approved physical acceptance remain **NOT RUN**.
