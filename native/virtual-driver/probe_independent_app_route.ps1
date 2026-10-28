#requires -Version 7.0
<#
Only for a disposable Windows VM with an ALREADY separately trusted lab
virtual audio driver. Launches an independent NAudio application process
after the actual bridge reports external-source readiness, then verifies
real physical WASAPI loopback and cleans up only processes started here.
NO driver installation, default-device changes, PCM files or networking.
#>
param(
  [Parameter(Mandatory)][string]$BridgeDll,
  [Parameter(Mandatory)][string]$IndependentToneDll,
  [Parameter(Mandatory)][string]$VirtualEndpointId,
  [Parameter(Mandatory)][string]$PhysicalEndpointId,
  [Parameter(Mandatory)][ValidateSet('I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST')]
  [string]$LabConsent,
  [Parameter(Mandatory)][string]$EvidenceDir,
  [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')][string]$SessionId,
  [Parameter(Mandatory)][ValidateSet(100, 150)][int]$BoostPercent,
  [switch]$ExpectNoSource,
  [ValidateSet('before', 'after')][string]$ControlPhase = 'before'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LabEvidenceDirectory.ps1')
. (Join-Path $PSScriptRoot 'LabProcessGuard.ps1')

$vm = Get-CimInstance -ClassName Win32_ComputerSystem
$who = "$($vm.Manufacturer) $($vm.Model)"
if ($who -notmatch '(?i)Virtual Machine|VMware|VirtualBox|QEMU|KVM|Xen|Parallels') {
  throw 'External-source lab refuses non-VM Windows hosts.'
}
if (-not $VirtualEndpointId -or -not $PhysicalEndpointId -or
    $VirtualEndpointId -eq $PhysicalEndpointId) {
  throw 'Independent source must have a different virtual and physical render endpoint.'
}
foreach ($file in @($BridgeDll, $IndependentToneDll)) {
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
    throw "Missing independently built lab executable: $file"
  }
}
function Get-LabIdHash([string]$id) {
  return [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($id))
  ).ToLowerInvariant()
}
$proofIdentity = [ordered]@{
  session_id = $SessionId.ToLowerInvariant()
  virtual_endpoint_sha256 = (Get-LabIdHash $VirtualEndpointId)
  physical_endpoint_sha256 = (Get-LabIdHash $PhysicalEndpointId)
  bridge_binary_sha256 = (Get-FileHash -LiteralPath $BridgeDll -Algorithm SHA256).Hash.ToLowerInvariant()
  source_binary_sha256 = (Get-FileHash -LiteralPath $IndependentToneDll -Algorithm SHA256).Hash.ToLowerInvariant()
}
if ($proofIdentity.virtual_endpoint_sha256 -eq $proofIdentity.physical_endpoint_sha256) {
  throw 'Proof cannot use the same virtual and physical render endpoint.'
}
$folder = [IO.Path]::GetFullPath($EvidenceDir)
if (-not (Test-Path -LiteralPath (Join-Path $folder 'lab-session.lock') -PathType Leaf) -or
    (Get-Content -LiteralPath (Join-Path $folder 'lab-session.lock') -Raw).Trim() -cne $SessionId.ToLowerInvariant()) {
  throw 'Independent LAB probe must use a fresh, session-locked folder prepared by the VM runner.'
}
$recordFile = if ($ExpectNoSource) {
  Join-Path $folder "no-source-control-$ControlPhase.json"
} else {
  Join-Path $folder "independent-source-result-$BoostPercent.json"
}
if (Test-Path -LiteralPath $recordFile) {
  throw "Refusing to overwrite stale LAB phase evidence: $recordFile"
}
$bridge = $null
$source = $null
$bridgeLines = [Collections.Generic.List[string]]::new()
try {
  $bridge = Start-LouderMeLabChild -Executable 'dotnet' -DeferStdOutDrain -Arguments @(
    $BridgeDll, '--probe-app-route', '--source-id', $VirtualEndpointId,
    '--output-id', $PhysicalEndpointId, '--boost', "$BoostPercent",
    '--confirm-experimental'
  )
  # These are real lines from the separately running bridge; do not infer
  # readiness from a PID or from the presence of a Windows sound icon.
  $ready = $false
  $deadline = [DateTime]::UtcNow.AddSeconds(12)
  $readyChars = 0
  while ([DateTime]::UtcNow -lt $deadline -and -not $bridge.Process.HasExited) {
    $pending = $bridge.Process.StandardOutput.ReadLineAsync()
    $remaining = [Math]::Max(1, [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds)
    if (-not $pending.Wait($remaining)) { break }
    $line = $pending.Result
    if ($null -eq $line) { break }
    $readyChars += $line.Length
    if ($bridgeLines.Count -ge 128 -or $readyChars -gt 32768) {
      throw 'Bridge readiness log exceeded bounded output; no tone will be started.'
    }
    $bridgeLines.Add($line)
    if ($line -eq 'EXTERNAL_SOURCE_READY') { $ready = $true; break }
  }
  if (-not $ready) {
    throw 'Bridge did not report actual capture+physical-monitor readiness. No source tone sent.'
  }
  # Start draining the remaining stdout IMMEDIATELY, while stderr was
  # already draining from process creation. No WaitForExit can now deadlock
  # on a full pipe while the independent process emits tone.
  $bridge.StdOutTask = $bridge.Process.StandardOutput.ReadToEndAsync()
  if ($ExpectNoSource) {
    # Control MUST run the actual full capture window with the correct VM
    # devices, but deliberately NEVER launch the independent tone process.
    $negativeOutput = Receive-LouderMeLabChildOutput -Child $bridge -TimeoutMs 9000
    $bridgeStdOut = ($bridgeLines.ToArray() -join [Environment]::NewLine) +
      [Environment]::NewLine + $negativeOutput.StdOut
    $bridgeStdErr = $negativeOutput.StdErr
    . (Join-Path $PSScriptRoot 'NoSourceControlCheck.ps1')
    $negative = Test-LouderMeNoSourceControl -ExitCode $negativeOutput.ExitCode `
      -StdOut $bridgeStdOut -StdErr $bridgeStdErr
    $negative['control_phase'] = $ControlPhase
    foreach ($key in $proofIdentity.Keys) { $negative[$key] = $proofIdentity[$key] }
    $file = Join-Path $folder "no-source-control-$ControlPhase.json"
    if (Test-Path -LiteralPath $file) {
      throw 'Stale no-source control evidence exists; choose a fresh evidence directory.'
    }
    Write-LouderMeLabJsonOnce -Path $file -Record $negative
    Write-Host "PASS: $ControlPhase negative control proved virtual and physical listeners reject missing source PCM."
    return
  }
  $source = Start-LouderMeLabChild -Executable 'dotnet' -Arguments @(
    $IndependentToneDll, '--device-id', $VirtualEndpointId, '--external-app-source',
    '--confirm-experimental'
  )
  $sourceResult = Receive-LouderMeLabChildOutput -Child $source -TimeoutMs 6000
  $sourceOutput = $sourceResult.StdOut
  $sourceError = $sourceResult.StdErr
  if ($sourceResult.ExitCode -ne 0) {
    throw 'Independent app PCM failed; do not accept bridge-only evidence.'
  }
  $bridgeResult = Receive-LouderMeLabChildOutput -Child $bridge -TimeoutMs 8000
  $remainder = $bridgeResult.StdOut
  $bridgeStdErr = $bridgeResult.StdErr
  $all = ($bridgeLines.ToArray() -join [Environment]::NewLine) +
    [Environment]::NewLine + $remainder
  # Logs contain numeric evidence only and must not store actual endpoint
  # IDs or private audio. Redact exact IDs if a library prints them.
  foreach ($endpoint in @($VirtualEndpointId, $PhysicalEndpointId)) {
    $all = $all.Replace($endpoint, '[REDACTED_ENDPOINT_ID]')
  }
  Write-LouderMeLabTextOnce -Path (Join-Path $folder "independent-source-route-$BoostPercent.log") -Content $all
  # Success banners alone are not proof. Require exactly one readiness,
  # separate-source completion, complete physical WASAPI frame counters,
  # coherent numeric 440 Hz identity, and zero dropped frames.
  . (Join-Path $PSScriptRoot 'PositiveSourceCheck.ps1')
  $checked = Test-LouderMePositiveSourceRoute -ExitCode $bridgeResult.ExitCode `
    -StdOut $all -StdErr $bridgeStdErr -SourceExitCode $sourceResult.ExitCode `
    -SourceStdOut $sourceOutput -SourceStdErr $sourceError
  $result = [ordered]@{
    status = 'PASS'
    stimulus = 'separate Windows process'
    boost_percent = $BoostPercent
    session_id = $proofIdentity.session_id
    virtual_endpoint_sha256 = $proofIdentity.virtual_endpoint_sha256
    physical_endpoint_sha256 = $proofIdentity.physical_endpoint_sha256
    bridge_binary_sha256 = $proofIdentity.bridge_binary_sha256
    source_binary_sha256 = $proofIdentity.source_binary_sha256
    physical_matched440_amplitude = $checked.physical_matched440_amplitude
    physical_coherence = $checked.physical_coherence
    physical_monitor_frames = $checked.physical_monitor_frames
    virtual_processed_frames = $checked.virtual_processed_frames
    physical_queued_frames = $checked.physical_queued_frames
    render_consumed_frames = $checked.render_consumed_frames
    render_starved_reads = $checked.render_starved_reads
    dropped_frames = $checked.dropped_frames
    independent_source_frames = $checked.independent_source_frames
    independent_source_channels = $checked.independent_source_channels
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
  Write-LouderMeLabJsonOnce -Path (Join-Path $folder "independent-source-result-$BoostPercent.json") -Record $result
  Write-Host 'PASS: independent Windows app process -> real installed virtual render -> own DSP -> physical WASAPI monitor.'
  Write-Warning 'NOT proof of production driver signing, universal third-party audio or acoustic SPL.'
} finally {
  # Kill ONLY these two child process trees, even on readiness timeout,
  # filled stdout/stderr pipes or an error in source/physical measurement.
  foreach ($child in @($source, $bridge)) {
    if ($null -ne $child) { Stop-LouderMeLabChild -Child $child }
  }
}
