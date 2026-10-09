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

The hosted CI still does not have a full `StampInf.exe` environment, so it
cannot certify an installable package. The full-WDK helper remains unverified
until executed on a provisioned driver-lab machine.

The workflow `native-virtual-device-ci.yml` prepares/checks actual
Microsoft source in Ubuntu, and attempts to compile the modified SysVAD
solution on the Windows 2025/VS2026 runner using Microsoft's WDK NuGet
layout. Source verification is distinct from successful driver compilation.

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

   A passing measurement reports a nonzero sample count, RMS and peak;
   silence, unsupported format or absent endpoint returns a failure. This
   is **not proof** that processed audio reached physical speakers.

5. Start the experimental `native/windows-bridge` with a distinct physical
   output. Confirm transformed PCM reaches actual headphones/speakers exactly
   once, with 100–250% gain and 7-band EQ.
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

**Never report “native global audio works” until physical proof is recorded.**
