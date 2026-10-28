#requires -Version 7.0
<#
.SYNOPSIS
  EXPERIMENTAL: install a separately signed LouderME LAB virtual render driver
  inside a disposable Windows VM, check virtual and physical PCM loopback,
  and remove only the device instance created by this run.
.DESCRIPTION
  Never run on a primary PC. Does not set the default audio endpoint or
  alter boot signing policies. Requires a previously snapshot-backed VM,
  administrator access, a locally trusted lab-signed driver package,
  Microsoft WDK devcon.exe, a built .NET bridge + native DSP DLL and a real
  physical playback endpoint visible inside the VM.
  No private keys, recordings, account credentials or network requests.
#>
param(
  [Parameter(Mandatory)][string]$PackageDir,
  [Parameter(Mandatory)][string]$DevconExe,
  [Parameter(Mandatory)][string]$BridgeDll,
  [Parameter(Mandatory)][string]$IndependentToneDll,
  [Parameter(Mandatory)][string]$PhysicalEndpointId,
  [Parameter(Mandatory)][ValidateSet('I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST')]
  [string]$LabConsent,
  [string]$EvidenceDir = (Join-Path $PWD 'louderme-physical-lab-evidence')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$hardwareId = 'Root\LouderMe_VirtualRender_Lab'
$instancePattern = 'ROOT\LOUDERME_VIRTUALRENDER_LAB\*'
$createdInstance = $null
$cleanupFailure = $null

function Get-LabDevices {
  return @(Get-PnpDevice -ErrorAction Stop |
    Where-Object { $_.InstanceId -like $script:instancePattern })
}
function Require-File([string]$file) {
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
    throw "Required lab-only file missing: $file"
  }
}
function Invoke-Bridge([string[]]$BridgeArgs, [string]$Label) {
  Write-Host "Running isolated lab phase: $Label"
  $lines = @(& dotnet $script:BridgeDll @BridgeArgs 2>&1)
  $code = $LASTEXITCODE
  $logPath = Join-Path $script:EvidenceDir "$Label.log"
  $ids = @($PhysicalEndpointId)
  if (Get-Variable -Name virtualId -Scope Script -ErrorAction SilentlyContinue) {
    $ids += $script:virtualId
  }
  $safeLines = @($lines | ForEach-Object {
    Protect-LouderMeLabLogLine -Line "$_" -KnownEndpointIds $ids -ListDevices:($Label -eq 'list-devices')
  })
  Write-LouderMeLabTextOnce -Path $logPath -Content (($safeLines -join [Environment]::NewLine) + [Environment]::NewLine)
  $safeLines | ForEach-Object { Write-Host $_ }
  if ($code -ne 0) { throw "$Label failed (exit $code); see $logPath" }
  return $lines
}

# Check the machine BEFORE accessing any privileged PnP operation.
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
  throw 'Admin required INSIDE an isolated disposable Windows LAB VM.'
}
$system = Get-CimInstance -ClassName Win32_ComputerSystem
$modelText = "$($system.Manufacturer) $($system.Model)"
if ($modelText -notmatch '(?i)Virtual Machine|VMware|VirtualBox|QEMU|KVM|Xen|Parallels') {
  throw 'Refusing driver install on a machine not recognized as a disposable VM.'
}
Write-Warning "Disposable lab VM detected: $modelText. A RECENT RESTORABLE SNAPSHOT IS REQUIRED."
Write-Warning 'This tool cannot verify a hypervisor snapshot or restore the Windows default output.'
Write-Warning 'Never run this on a personal/work PC; stop if there is no VM snapshot.'

$package = [IO.Path]::GetFullPath($PackageDir)
$inf = Join-Path $package 'LouderMeVirtualRenderLab.inf'
$cat = Join-Path $package 'LouderMeVirtualRenderLab.cat'
$sys = Join-Path $package 'TabletAudioSample.sys'
foreach ($file in @($inf, $cat, $sys, $DevconExe, $BridgeDll, $IndependentToneDll)) { Require-File $file }
$dspDll = Join-Path (Split-Path -Parent $BridgeDll) 'louderme_audio_c.dll'
Require-File $dspDll
if ([string]::IsNullOrWhiteSpace($PhysicalEndpointId)) {
  throw 'A manually inspected physical endpoint ID is mandatory.'
}
if ((Get-LabDevices).Count -ne 0) {
  throw 'LouderME LAB root device already exists; refuse to modify an unowned instance.'
}
$signature = Get-AuthenticodeSignature -FilePath $cat
if ($signature.Status -ne 'Valid') {
  throw "CAT signature must be trusted inside this isolated VM (status: $($signature.Status))."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  throw 'dotnet runtime/SDK is missing; cannot execute native bridge.'
}
# Installing a new render device may cause Windows to pick it as the
# default even though the app NEVER asks to change that setting. Snapshot
# the current Multimedia default BEFORE any privileged PnP operation.
. (Join-Path $PSScriptRoot 'DefaultRenderGuard.ps1')
. (Join-Path $PSScriptRoot 'LabInputIntegrity.ps1')
$inputSnapshot = New-LouderMeLabInputSnapshot -Files ([ordered]@{
  INF = $inf
  CAT = $cat
  SYS = $sys
  BRIDGE = $BridgeDll
  NATIVE_DSP = $dspDll
  INDEPENDENT_TONE = $IndependentToneDll
  DEVCON = $DevconExe
})
Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'before first driver operation'
$defaultBefore = Get-LouderMeDefaultRenderFingerprint -BridgeDll $BridgeDll
Write-Host 'Preflight: existing Windows default multimedia playback identity captured (SHA256 only).'

. (Join-Path $PSScriptRoot 'LabEvidenceDirectory.ps1')
$sessionId = [guid]::NewGuid().ToString('D')
# Fail before the first privileged PnP operation for any reused folder,
# including one containing only metadata from a failed previous attempt.
$evidence = New-LouderMeLabEvidenceDirectory -Path $EvidenceDir -SessionId $sessionId
$script:EvidenceDir = $evidence
$script:BridgeDll = [IO.Path]::GetFullPath($BridgeDll)
$metadata = @(
  'LAB_VIRTUAL_RENDER_CI=NOT_APPLICABLE',
  "VM=$modelText",
  "LAB_SESSION_ID=$sessionId",
  'DRIVER_PACKAGE_SIGNATURE=VALID_LOCAL_VM',
  "CAT_SHA256=$((Get-FileHash -LiteralPath $cat -Algorithm SHA256).Hash.ToLowerInvariant())",
  "SYS_SHA256=$((Get-FileHash -LiteralPath $sys -Algorithm SHA256).Hash.ToLowerInvariant())",
  'SPEAKER_AUDIO_MEASUREMENT=NOT_PERFORMED',
  'PRODUCTION_SIGNING=NOT_VERIFIED',
  "DEFAULT_RENDER_SHA256_BEFORE=$defaultBefore",
  'WINDOWS_DEFAULT_DEVICE_CHANGE=NOT_REQUESTED'
) + @(Get-LouderMeLabInputMetadata -Snapshot $inputSnapshot)
Write-LouderMeLabTextOnce -Path (Join-Path $evidence 'lab-metadata.txt') -Content (($metadata -join [Environment]::NewLine) + [Environment]::NewLine)

try {
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'immediately before driver install'
  Write-Host "Installing LAB root device '$hardwareId' in disposable VM only."
  # WDK tools can echo private generated PnP instance IDs in stdout/stderr.
  # Keep only exit status; never upload their raw text to shared Actions logs.
  & $DevconExe install $inf $hardwareId *> $null
  if ($LASTEXITCODE -ne 0) {
    throw 'Devcon root-device installation failed; inspect package signing / VM test policy.'
  }
  for ($tries = 0; $tries -lt 30; $tries++) {
    $newDevices = @(Get-LabDevices)
    if ($newDevices.Count -eq 1 -and $newDevices[0].Status -eq 'OK') {
      $createdInstance = $newDevices[0].InstanceId
      break
    }
    Start-Sleep -Seconds 1
  }
  if (-not $createdInstance) {
    throw 'Virtual LAB PnP device did not become healthy within 30 seconds.'
  }
  Write-Host 'Installed exactly one new virtual LAB instance (private Windows PnP ID withheld).'
  # Abort BEFORE opening capture/physical speaker if PnP auto-switched the
  # user's default render endpoint during driver installation.
  $defaultAfterInstall = Get-LouderMeDefaultRenderFingerprint -BridgeDll $BridgeDll
  Assert-LouderMeDefaultRenderUnchanged -Expected $defaultBefore -Actual $defaultAfterInstall -Phase 'driver installation'

  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'before WASAPI endpoint discovery'
  $list = @(Invoke-Bridge -BridgeArgs @('--list-devices') -Label 'list-devices')
  $virtualIds = @()
  $physicalFound = $false
  foreach ($line in $list) {
    if ("$line" -match '^\[(?<friendly>.+)\] (?<deviceid>.+)$') {
      $friendly = $Matches.friendly
      $deviceId = $Matches.deviceid.Trim()
      if ($friendly -match '(?i)LouderMe Virtual') {
        $virtualIds += $deviceId
      } elseif ($deviceId -eq $PhysicalEndpointId) {
        $physicalFound = $true
      }
    }
  }
  if ($virtualIds.Count -ne 1) {
    throw "Expected exactly one active LouderME LAB virtual render endpoint, found $($virtualIds.Count)."
  }
  if (-not $physicalFound -or $virtualIds[0] -eq $PhysicalEndpointId) {
    throw 'Selected separate PHYSICAL playback endpoint was not listed by WASAPI.'
  }
  $virtualId = $virtualIds[0]
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'before virtual tone'
  [void](Invoke-Bridge -BridgeArgs @('--probe-virtual', '--source-id',
    $virtualId, '--confirm-experimental') -Label 'virtual-440hz')
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'before physical tone'
  [void](Invoke-Bridge -BridgeArgs @('--probe-route', '--source-id',
    $virtualId, '--output-id', $PhysicalEndpointId, '--boost', '150',
    '--confirm-experimental') -Label 'physical-route-440hz')
  # Exact same installed driver/endpoint is now exercised by a separate
  # Windows process sending PCM; the bridge creates no stimulus at all.
  $separateProbe = Join-Path $PSScriptRoot 'probe_independent_app_route.ps1'
  Require-File $separateProbe
  # Never accept a stale PASS left behind by an earlier unsuccessful VM
  # run. The user must provide a new evidence directory each run.
  foreach ($percent in @(100, 150)) {
    if (Test-Path (Join-Path $evidence "independent-source-result-$percent.json")) {
      throw "Stale independent source evidence for $percent% exists; choose a fresh evidence directory."
    }
  }
  # Real no-stimulus negative proof must pass BEFORE either renderer runs.
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'before silence control'
  pwsh -NoProfile -File $separateProbe -BridgeDll $BridgeDll -IndependentToneDll $IndependentToneDll `
    -VirtualEndpointId $virtualId -PhysicalEndpointId $PhysicalEndpointId `
    -LabConsent $LabConsent -EvidenceDir $evidence -SessionId $sessionId -BoostPercent 100 `
    -ExpectNoSource -ControlPhase before
  if ($LASTEXITCODE -ne 0 -or
      -not (Test-Path -LiteralPath (Join-Path $evidence 'no-source-control-before.json'))) {
    throw 'Silence baseline failed; external source gain measurement forbidden.'
  }
  foreach ($percent in @(100, 150)) {
    Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase "before independent app $percent-percent probe"
    pwsh -NoProfile -File $separateProbe -BridgeDll $BridgeDll -IndependentToneDll $IndependentToneDll `
      -VirtualEndpointId $virtualId -PhysicalEndpointId $PhysicalEndpointId `
      -LabConsent $LabConsent -EvidenceDir $evidence -SessionId $sessionId -BoostPercent $percent
    if ($LASTEXITCODE -ne 0 -or
        -not (Test-Path (Join-Path $evidence "independent-source-result-$percent.json"))) {
      throw "Independent APP PCM at $percent% did not reach the virtual→native DSP→physical monitor."
    }
  }
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'after both independent source probes'
  # The SAME real virtual and physical devices must reject silence again
  # after the sources exit, or 440Hz contamination makes the ratio suspect.
  pwsh -NoProfile -File $separateProbe -BridgeDll $BridgeDll -IndependentToneDll $IndependentToneDll `
    -VirtualEndpointId $virtualId -PhysicalEndpointId $PhysicalEndpointId `
    -LabConsent $LabConsent -EvidenceDir $evidence -SessionId $sessionId -BoostPercent 100 `
    -ExpectNoSource -ControlPhase after
  if ($LASTEXITCODE -ne 0 -or
      -not (Test-Path -LiteralPath (Join-Path $evidence 'no-source-control-after.json'))) {
    throw 'Post-source silence control failed; previous signal may be lingering.'
  }
  $validator = Join-Path $PSScriptRoot 'IndependentGainCheck.ps1'
  Require-File $validator
  . $validator
  # Real measured source evidence must pass the same data guard exercised in
  # native CI adversarial fixtures. Only accepted comparisons can leave a PASS.
  Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'after final silence control'
  $ratio = Test-LouderMeIndependentGain -EvidenceDir $evidence -ExpectedSessionId $sessionId
  Write-LouderMeLabTextOnce -Path (Join-Path $evidence 'independent-app-boost-ratio.txt') -Content ($ratio.ToString('F3', [Globalization.CultureInfo]::InvariantCulture) + [Environment]::NewLine)
  # Even after a successful measurement, a Windows audio policy change by
  # another component invalidates the claim that this LAB never displaced
  # the pre-existing multimedia playback device.
  $defaultAfterProbe = Get-LouderMeDefaultRenderFingerprint -BridgeDll $BridgeDll
  Assert-LouderMeDefaultRenderUnchanged -Expected $defaultBefore -Actual $defaultAfterProbe -Phase 'completed audio probes'
  Write-Host (("LAB_ROUTE_INDEPENDENT_APP_100_150_GAIN=PASS ratio={0:F3}; " +
    "NOT consumer sound, physical SPL or general app compatibility.") -f $ratio)
} finally {
  # Even a failed probe must clean up the exact disposable test device.
  # If a partial devcon install returned an error, detect that new instance.
  # Prefer the exact instance created by this run; never remove an
  # unrelated PnP instance discovered through a broad hardware-ID query.
  $allLabDevices = @(Get-LabDevices)
  if ($createdInstance) {
    $toRemove = @($allLabDevices | Where-Object { $_.InstanceId -ceq $createdInstance })
    if ($allLabDevices.Count -ne $toRemove.Count) {
      $cleanupFailure = 'Unexpected additional LAB devices detected; leave unrelated instances alone and restore VM snapshot.'
    }
  } elseif ($allLabDevices.Count -le 1) {
    # A failed devcon install can leave precisely one newly created
    # instance. Preflight proved none existed before this run.
    $toRemove = $allLabDevices
  } else {
    $toRemove = @()
    $cleanupFailure = 'Ambiguous partial LAB installs detected; refusing broad device removal; restore VM snapshot.'
  }
  foreach ($device in $toRemove) {
    $id = $device.InstanceId
    Write-Warning 'Removing only LouderME LAB-owned device instance (private ID withheld).'
    # pnputil prints the private instance ID by default. Suppress native
    # output while preserving exit-code based rollback acceptance.
    & pnputil /remove-device "$id" *> $null
    if ($LASTEXITCODE -ne 0) {
      $cleanupFailure = 'Failed to remove owned LAB device; revert VM snapshot immediately.'
    }
  }
  Start-Sleep -Seconds 1
  if ((Get-LabDevices).Count -ne 0) {
    $cleanupFailure = 'LAB device still present after cleanup; revert VM snapshot immediately.'
  }
  # Device removal can also change the Windows default. Never report
  # cleanup SUCCESS without checking it again. Do NOT set defaults here:
  # changing the user's device behind their back is unacceptable.
  try {
    $defaultAfterCleanup = Get-LouderMeDefaultRenderFingerprint -BridgeDll $BridgeDll
    Assert-LouderMeDefaultRenderUnchanged -Expected $defaultBefore -Actual $defaultAfterCleanup -Phase 'driver cleanup'
    Write-LouderMeLabTextOnce -Path (Join-Path $evidence 'default-render-identity-after-cleanup.txt') -Content ("DEFAULT_RENDER_UNCHANGED=PASS" + [Environment]::NewLine + "SHA256=$defaultAfterCleanup" + [Environment]::NewLine)
  } catch {
    $cleanupFailure = "Windows default playback was changed or cannot be verified after cleanup: $($_.Exception.Message). Restore VM snapshot."
  }
  # Even a PASS from the DSP/gain verifier is invalid if any shipped test
  # input was replaced before teardown. Check after cleaning the device so
  # this integrity failure never leaves a kernel instance installed.
  try {
    Assert-LouderMeLabInputsUnchanged -Snapshot $inputSnapshot -Phase 'after driver cleanup'
    Write-LouderMeLabTextOnce -Path (Join-Path $evidence 'lab-input-integrity-after-cleanup.txt') -Content ('INPUTS_UNCHANGED=PASS' + [Environment]::NewLine)
  } catch {
    $cleanupFailure = 'A critical lab binary changed or became unobservable during session; restore VM snapshot.'
  }
  if ($cleanupFailure) { throw $cleanupFailure }
  Write-Host 'LAB root device removed; original default multimedia playback SHA256 retained. Restore VM snapshot.'
}
