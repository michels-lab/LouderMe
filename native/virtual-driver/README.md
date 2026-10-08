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
- The selected virtual speaker is configured with a loopback-support flag,
  allowing the existing WASAPI bridge to investigate its actual PCM stream.
  **This flag is not evidence that virtual SysVAD supplies non-silent PCM to
  loopback. Test on real Windows hardware before claiming it works.**
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

The workflow `native-virtual-device-ci.yml` prepares/checks actual
Microsoft source in Ubuntu, and attempts to compile the modified SysVAD
solution on the Windows 2025/VS2026 runner using Microsoft's WDK NuGet
layout. Source verification is distinct from successful driver compilation.

**Do not install, publish or add the unsigned/test-signed driver to stable
LouderME installers.** Building the WDK solution may generate unsigned or
development-signed artifacts; that is not sufficient for consumer deployment.

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
4. Start the experimental `native/windows-bridge` with a distinct physical
   output. Confirm transformed PCM reaches actual headphones/speakers exactly
   once, with 100–250% gain and 7-band EQ.
5. Test crash/unplug/reboot recovery, physical device switching,
   buffer underruns, playback latency, CPU, 44.1/48/96 kHz sample rates,
   and format conversion. Restore original defaults on uninstall.
6. Obtain appropriate Windows signing/distribution authorization and perform
   an isolated production installer validation. Explicit user consent is
   mandatory for any driver install/device switch.

## Safety

No driver installation, default-device modification, registry write, audio
capture or replay is performed by the preparation script or CI. Only the
separately launched experimental Windows WASAPI bridge can attempt audio
capture/replay, with explicit source/output IDs. Normal stable LouderME
continues using the documented Equalizer APO fallback.

**Never report “native global audio works” until physical proof is recorded.**
