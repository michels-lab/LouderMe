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
  $lines | Out-File -LiteralPath $logPath -Encoding utf8
  $lines | ForEach-Object { Write-Host $_ }
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

$evidence = [IO.Path]::GetFullPath($EvidenceDir)
if (-not (Test-Path -LiteralPath $evidence)) {
  [void](New-Item -ItemType Directory -Path $evidence -Force)
}
$script:EvidenceDir = $evidence
$script:BridgeDll = [IO.Path]::GetFullPath($BridgeDll)
@(
  'LAB_VIRTUAL_RENDER_CI=NOT_APPLICABLE',
  "VM=$modelText",
  'DRIVER_PACKAGE_SIGNATURE=VALID_LOCAL_VM',
  "CAT_SHA256=$((Get-FileHash -LiteralPath $cat -Algorithm SHA256).Hash.ToLowerInvariant())",
  "SYS_SHA256=$((Get-FileHash -LiteralPath $sys -Algorithm SHA256).Hash.ToLowerInvariant())",
  'SPEAKER_AUDIO_MEASUREMENT=NOT_PERFORMED',
  'PRODUCTION_SIGNING=NOT_VERIFIED'
) | Out-File -LiteralPath (Join-Path $evidence 'lab-metadata.txt') -Encoding utf8

try {
  Write-Host "Installing LAB root device '$hardwareId' in disposable VM only."
  & $DevconExe install $inf $hardwareId
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
  Write-Host "Installed only new virtual LAB instance: $createdInstance"

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
  [void](Invoke-Bridge -BridgeArgs @('--probe-virtual', '--source-id',
    $virtualId, '--confirm-experimental') -Label 'virtual-440hz')
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
  foreach ($percent in @(100, 150)) {
    pwsh -NoProfile -File $separateProbe -BridgeDll $BridgeDll -IndependentToneDll $IndependentToneDll `
      -VirtualEndpointId $virtualId -PhysicalEndpointId $PhysicalEndpointId `
      -LabConsent $LabConsent -EvidenceDir $evidence -BoostPercent $percent
    if ($LASTEXITCODE -ne 0 -or
        -not (Test-Path (Join-Path $evidence "independent-source-result-$percent.json"))) {
      throw "Independent APP PCM at $percent% did not reach the virtual→native DSP→physical monitor."
    }
  }
  $baseMeasure = Get-Content (Join-Path $evidence 'independent-source-result-100.json') -Raw | ConvertFrom-Json
  $highMeasure = Get-Content (Join-Path $evidence 'independent-source-result-150.json') -Raw | ConvertFrom-Json
  $reference = [double]$baseMeasure.physical_matched440_amplitude
  $amplified = [double]$highMeasure.physical_matched440_amplitude
  if ($baseMeasure.status -ne 'PASS' -or $highMeasure.status -ne 'PASS' -or
      [int]$baseMeasure.boost_percent -ne 100 -or [int]$highMeasure.boost_percent -ne 150 -or
      $baseMeasure.stimulus -ne 'separate Windows process' -or
      $highMeasure.stimulus -ne 'separate Windows process' -or
      $reference -le 0 -or $amplified -le 0) {
    throw 'Missing or invalid positive physical 440 Hz gain baselines.'
  }
  # Expected 1.5x linear output is only a lab acceptance RANGE. If a
  # Windows enhancement, limiter, or bad virtual render path changes gain,
  # FAIL instead of calling synthetic PCM tests an audible pass.
  $ratio = $amplified / $reference
  $ratio.ToString('F3', [Globalization.CultureInfo]::InvariantCulture) |
    Out-File -LiteralPath (Join-Path $evidence 'independent-app-boost-ratio.txt') -Encoding utf8
  if ($ratio -lt 1.30 -or $ratio -gt 1.70) {
    throw "Physical 100%→150% native boost ratio outside conservative acceptance band: $ratio."
  }
  Write-Host (("LAB_ROUTE_INDEPENDENT_APP_100_150_GAIN=PASS ratio={0:F3}; " +
    "NOT consumer sound, physical SPL or general app compatibility.") -f $ratio)
} finally {
  # Even a failed probe must clean up the exact disposable test device.
  # If a partial devcon install returned an error, detect that new instance.
  $toRemove = @(Get-LabDevices)
  foreach ($device in $toRemove) {
    $id = $device.InstanceId
    Write-Warning "Removing ONLY LouderME LAB device instance: $id"
    & pnputil /remove-device "$id"
    if ($LASTEXITCODE -ne 0) {
      $cleanupFailure = "Failed to remove LAB device $id; revert VM snapshot immediately."
    }
  }
  Start-Sleep -Seconds 1
  if ((Get-LabDevices).Count -ne 0) {
    $cleanupFailure = 'LAB device still present after cleanup; revert VM snapshot immediately.'
  }
  if ($cleanupFailure) { throw $cleanupFailure }
  Write-Host 'LAB root device removed. Check Windows default output; restore VM snapshot.'
}
