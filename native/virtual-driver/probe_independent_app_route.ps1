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

function Make-Process([string]$dll, [string[]]$arguments) {
  $start = [Diagnostics.ProcessStartInfo]::new('dotnet')
  $start.ArgumentList.Add([IO.Path]::GetFullPath($dll))
  foreach ($a in $arguments) { $start.ArgumentList.Add($a) }
  $start.UseShellExecute = $false
  $start.CreateNoWindow = $true
  $start.RedirectStandardOutput = $true
  $start.RedirectStandardError = $true
  $p = [Diagnostics.Process]::new()
  $p.StartInfo = $start
  if (-not $p.Start()) { throw 'Could not launch isolated lab process.' }
  return $p
}

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
  $bridge = Make-Process $BridgeDll @(
    '--probe-app-route', '--source-id', $VirtualEndpointId,
    '--output-id', $PhysicalEndpointId, '--boost', "$BoostPercent",
    '--confirm-experimental'
  )
  # These are real lines from the separately running bridge; do not infer
  # readiness from a PID or from the presence of a Windows sound icon.
  $ready = $false
  $deadline = [DateTime]::UtcNow.AddSeconds(12)
  while ([DateTime]::UtcNow -lt $deadline -and -not $bridge.HasExited) {
    $pending = $bridge.StandardOutput.ReadLineAsync()
    $remaining = [Math]::Max(1, [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds)
    if (-not $pending.Wait($remaining)) { break }
    $line = $pending.Result
    if ($null -eq $line) { break }
    $bridgeLines.Add($line)
    if ($line -eq 'EXTERNAL_SOURCE_READY') { $ready = $true; break }
  }
  if (-not $ready) {
    throw 'Bridge did not report actual capture+physical-monitor readiness. No source tone sent.'
  }
  if ($ExpectNoSource) {
    # Control MUST run the actual full capture window with the correct VM
    # devices, but deliberately NEVER launch the independent tone process.
    if (-not $bridge.WaitForExit(9000)) {
      throw 'No-stimulus bridge capture failed to finish its complete time window.'
    }
    $bridgeStdOut = ($bridgeLines.ToArray() -join [Environment]::NewLine) +
      [Environment]::NewLine + $bridge.StandardOutput.ReadToEnd()
    $bridgeStdErr = $bridge.StandardError.ReadToEnd()
    . (Join-Path $PSScriptRoot 'NoSourceControlCheck.ps1')
    $negative = Test-LouderMeNoSourceControl -ExitCode $bridge.ExitCode `
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
  $source = Make-Process $IndependentToneDll @(
    '--device-id', $VirtualEndpointId, '--external-app-source',
    '--confirm-experimental'
  )
  if (-not $source.WaitForExit(6000)) {
    throw 'Independent source app did not finish within six seconds.'
  }
  $sourceOutput = $source.StandardOutput.ReadToEnd()
  $sourceError = $source.StandardError.ReadToEnd()
  if ($source.ExitCode -ne 0 -or
      $sourceOutput -notmatch 'EXTERNAL_APP_TONE_FINISHED') {
    throw "Independent app PCM failed: exit=$($source.ExitCode), output=$sourceOutput, error=$sourceError"
  }
  if (-not $bridge.WaitForExit(8000)) {
    throw 'Bridge did not finish physical monitor/evidence evaluation in time.'
  }
  $remainder = $bridge.StandardOutput.ReadToEnd()
  $all = ($bridgeLines.ToArray() -join [Environment]::NewLine) +
    [Environment]::NewLine + $remainder
  # Logs contain numeric evidence only and must not store actual endpoint
  # IDs or private audio. Redact exact IDs if a library prints them.
  foreach ($endpoint in @($VirtualEndpointId, $PhysicalEndpointId)) {
    $all = $all.Replace($endpoint, '[REDACTED_ENDPOINT_ID]')
  }
  Write-LouderMeLabTextOnce -Path (Join-Path $folder "independent-source-route-$BoostPercent.log") -Content $all
  if ($bridge.ExitCode -ne 0 -or
      $all -notmatch 'PASS: LAB independent source Windows process') {
    throw "External application PCM failed route gate: bridge exit=$($bridge.ExitCode)."
  }
  $metric = [regex]::Match($all, 'Physical LAB proof: [^\r\n]*matched440=(?<amplitude>[0-9]+(?:\.[0-9]+)?)\s+coherence=(?<coherence>[0-9]+(?:\.[0-9]+)?)')
  if (-not $metric.Success) {
    throw 'Bridge claimed success without parseable numeric physical 440Hz evidence.'
  }
  $amplitude = [double]::Parse($metric.Groups['amplitude'].Value, [Globalization.CultureInfo]::InvariantCulture)
  $coherence = [double]::Parse($metric.Groups['coherence'].Value, [Globalization.CultureInfo]::InvariantCulture)
  if ($amplitude -le 0 -or $coherence -lt 0.5) {
    throw 'Independent app produced invalid physical tone amplitude/coherence evidence.'
  }
  $result = [ordered]@{
    status = 'PASS'
    stimulus = 'separate Windows process'
    boost_percent = $BoostPercent
    session_id = $proofIdentity.session_id
    virtual_endpoint_sha256 = $proofIdentity.virtual_endpoint_sha256
    physical_endpoint_sha256 = $proofIdentity.physical_endpoint_sha256
    bridge_binary_sha256 = $proofIdentity.bridge_binary_sha256
    source_binary_sha256 = $proofIdentity.source_binary_sha256
    physical_matched440_amplitude = $amplitude
    physical_coherence = $coherence
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
  Write-LouderMeLabJsonOnce -Path (Join-Path $folder "independent-source-result-$BoostPercent.json") -Record $result
  Write-Host 'PASS: independent Windows app process -> real installed virtual render -> own DSP -> physical WASAPI monitor.'
  Write-Warning 'NOT proof of production driver signing, universal third-party audio or acoustic SPL.'
} finally {
  # Kill ONLY the PIDs created by THIS lab test (never every dotnet process).
  foreach ($process in @($source, $bridge)) {
    if ($null -ne $process) {
      if (-not $process.HasExited) {
        $process.Kill($true)
        [void]$process.WaitForExit(3000)
      }
      $process.Dispose()
    }
  }
}
