#requires -Version 7.0
<#
Only in owner-consented snapshot-backed Windows VM after the signed LAB
device was installed. Proves a SEPARATE Windows process's 440 Hz PCM is
observed on the VIRTUAL endpoint itself, before any DSP or physical output.
Does not install drivers, switch defaults, use microphones or record PCM.
#>
param(
  [Parameter(Mandatory)][string]$BridgeDll,
  [Parameter(Mandatory)][string]$IndependentToneDll,
  [Parameter(Mandatory)][string]$VirtualEndpointId,
  [Parameter(Mandatory)][ValidateSet('I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST')]
  [string]$LabConsent,
  [Parameter(Mandatory)][string]$EvidenceDir,
  [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F-]{36}$')][string]$SessionId
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LabEvidenceDirectory.ps1')
. (Join-Path $PSScriptRoot 'LabProcessGuard.ps1')
$vm = Get-CimInstance -ClassName Win32_ComputerSystem
if ("$($vm.Manufacturer) $($vm.Model)" -notmatch '(?i)Virtual Machine|VMware|VirtualBox|QEMU|KVM|Xen|Parallels') {
  throw 'Isolated virtual PCM check requires an actual disposable Windows VM.'
}
$session = [guid]::Empty
if (-not [guid]::TryParse($SessionId, [ref]$session) -or $session -eq [guid]::Empty) {
  throw 'Fresh nonempty lab session GUID required.'
}
$sessionIdCanonical = $session.ToString('D')
$folder = [IO.Path]::GetFullPath($EvidenceDir)
if ((Get-Content -LiteralPath (Join-Path $folder 'lab-session.lock') -Raw).Trim() -cne $sessionIdCanonical) {
  throw 'Virtual PCM proof requires the current immutable lab evidence session.'
}
if ([string]::IsNullOrWhiteSpace($VirtualEndpointId)) {
  throw 'Selected exact virtual render endpoint is mandatory.'
}
foreach ($input in @($BridgeDll, $IndependentToneDll)) {
  if (-not (Test-Path -LiteralPath $input -PathType Leaf)) {
    throw 'Built and independently pinned bridge/tone applications are required.'
  }
}
$evidencePath = Join-Path $folder 'independent-virtual-source-proof.json'
$logPath = Join-Path $folder 'independent-virtual-source-proof.log'
if ((Test-Path -LiteralPath $evidencePath) -or (Test-Path -LiteralPath $logPath)) {
  throw 'Existing virtual PCM evidence is never reused or overwritten.'
}
$bridge = $null
$source = $null
$readyLines = [Collections.Generic.List[string]]::new()
try {
  $bridge = Start-LouderMeLabChild -Executable 'dotnet' -DeferStdOutDrain -Arguments @(
    $BridgeDll, '--probe-virtual-external', '--source-id', $VirtualEndpointId,
    '--confirm-experimental'
  )
  $deadline = [DateTime]::UtcNow.AddSeconds(12)
  $readinessChars = 0
  $ready = $false
  while ([DateTime]::UtcNow -lt $deadline -and -not $bridge.Process.HasExited) {
    $pending = $bridge.Process.StandardOutput.ReadLineAsync()
    $milliseconds = [Math]::Max(1, [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds)
    if (-not $pending.Wait($milliseconds)) { break }
    $line = $pending.Result
    if ($null -eq $line) { break }
    $readyLines.Add($line)
    $readinessChars += $line.Length
    if ($readyLines.Count -gt 128 -or $readinessChars -gt 32768) {
      throw 'Unbounded virtual PCM readiness output rejected.'
    }
    if ($line -ceq 'EXTERNAL_VIRTUAL_READY') { $ready = $true; break }
  }
  if (-not $ready) {
    throw 'Virtual WASAPI capture did not report readiness; no independent source will start.'
  }
  $bridge.StdOutTask = $bridge.Process.StandardOutput.ReadToEndAsync()
  $source = Start-LouderMeLabChild -Executable 'dotnet' -Arguments @(
    $IndependentToneDll, '--device-id', $VirtualEndpointId,
    '--external-app-source', '--confirm-experimental'
  )
  $resultSource = Receive-LouderMeLabChildOutput -Child $source -TimeoutMs 6000
  if ($resultSource.ExitCode -ne 0 -or
      @($resultSource.StdOut -split '\r?\n' | Where-Object {
        $_ -match '^EXTERNAL_APP_TONE_FINISHED 440Hz frames=[0-9]+ channels=[12];'
      }).Count -ne 1) {
    throw 'Separate app failed to complete its exact 440 Hz PCM into the selected virtual endpoint.'
  }
  $resultBridge = Receive-LouderMeLabChildOutput -Child $bridge -TimeoutMs 8000
  $stdout = ($readyLines.ToArray() -join [Environment]::NewLine) +
    [Environment]::NewLine + $resultBridge.StdOut
  if ($resultBridge.ExitCode -ne 0 -or
      -not [string]::IsNullOrWhiteSpace($resultBridge.StdErr)) {
    throw 'External virtual PCM bridge returned failure or unexpected error output.'
  }
  $lines = @($stdout -split '\r?\n')
  if (@($lines | Where-Object { $_ -ceq 'EXTERNAL_VIRTUAL_READY' }).Count -ne 1 -or
      @($lines | Where-Object {
        $_ -ceq 'PASS: independent Windows process -> LouderMe virtual WASAPI loopback 440 Hz identity. No physical output or DSP was opened.'
      }).Count -ne 1) {
    throw 'Missing exactly one source-ready and independent virtual-PCM PASS marker.'
  }
  $matches = @($lines | Where-Object {
    $_ -match '^VIRTUAL_EXTERNAL_PROOF frames=[0-9]+ matched440=[0-9]+\.[0-9]{6} coherence=[0-9]+\.[0-9]{4}$'
  })
  if ($matches.Count -ne 1) { throw 'Unique numeric virtual loopback evidence missing.' }
  $proof = [regex]::Match($matches[0],
    '^VIRTUAL_EXTERNAL_PROOF frames=(?<frames>[0-9]+) matched440=(?<amplitude>[0-9]+\.[0-9]{6}) coherence=(?<coherence>[0-9]+\.[0-9]{4})$')
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $frames = [long]::Parse($proof.Groups['frames'].Value, $culture)
  $amplitude = [double]::Parse($proof.Groups['amplitude'].Value, $culture)
  $coherence = [double]::Parse($proof.Groups['coherence'].Value, $culture)
  if ($frames -lt 4000 -or -not [double]::IsFinite($amplitude) -or
      -not [double]::IsFinite($coherence) -or
      $amplitude -lt 0.007 -or $amplitude -gt 0.15 -or
      $coherence -lt 0.50 -or $coherence -gt 1.0) {
    throw 'Virtual loopback lacks real numeric 440 Hz PCM evidence from the separate app.'
  }
  $redacted = $stdout.Replace($VirtualEndpointId, '[REDACTED_ENDPOINT_ID]')
  Write-LouderMeLabTextOnce -Path $logPath -Content $redacted
  $record = [ordered]@{
    status = 'PASS'
    stimulus = 'separate Windows process'
    session_id = $sessionIdCanonical
    virtual_endpoint_sha256 = [Convert]::ToHexString(
      [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($VirtualEndpointId))).ToLowerInvariant()
    bridge_binary_sha256 = (Get-FileHash -LiteralPath $BridgeDll -Algorithm SHA256).Hash.ToLowerInvariant()
    source_binary_sha256 = (Get-FileHash -LiteralPath $IndependentToneDll -Algorithm SHA256).Hash.ToLowerInvariant()
    virtual_monitor_frames = $frames
    virtual_matched440_amplitude = $amplitude
    virtual_coherence = $coherence
    physical_output_verified = $false
    global_windows_audio_verified = $false
    acoustic_spl_verified = $false
  }
  Write-LouderMeLabJsonOnce -Path $evidencePath -Record $record
  Write-Host 'PASS: separate Windows application PCM reached the named virtual WASAPI loopback. No physical output or DSP was opened.'
} finally {
  foreach ($child in @($source, $bridge)) {
    if ($null -ne $child) { Stop-LouderMeLabChild -Child $child }
  }
}
