#requires -Version 7.0
# Device-free adversarial tests; never launch Devcon, WASAPI, PnP or driver.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LabInputIntegrity.ps1')

$root = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-LabIntegrity-$([guid]::NewGuid().ToString('D'))"
[void][IO.Directory]::CreateDirectory($root)
function Assert-Fails([string]$Label, [scriptblock]$Action) {
  $rejected = $false
  try { & $Action | Out-Null } catch { $rejected = $true }
  if (-not $rejected) { throw "Lab input identity guard incorrectly accepted: $Label" }
}
try {
  $inputs = [ordered]@{}
  foreach ($label in @('INF','CAT','SYS','BRIDGE','NATIVE_DSP','INDEPENDENT_TONE','DEVCON')) {
    $file = Join-Path $root "$label.bin"
    $bytes = [Text.Encoding]::UTF8.GetBytes("LouderMe lab artifact role=$label; synthetic bytes only.")
    [IO.File]::WriteAllBytes($file, $bytes)
    $inputs[$label] = $file
  }
  $frozen = New-LouderMeLabInputSnapshot -Files $inputs
  Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'baseline'
  $metadata = @(Get-LouderMeLabInputMetadata -Snapshot $frozen)
  if ($metadata.Count -ne 7) { throw 'Seven independent input hashes required.' }
  foreach ($label in $inputs.Keys) {
    if (@($metadata | Where-Object { $_ -match "^INPUT_$($label)_SHA256=[a-f0-9]{64}$" }).Count -ne 1) {
      throw "Missing stable fingerprint for $label."
    }
  }
  if (($metadata -join [Environment]::NewLine).Contains($root) -or
      ($metadata -join [Environment]::NewLine).Contains('LouderMe lab artifact')) {
    throw 'Private local file paths or contents escaped into lab metadata.'
  }

  $original = [IO.File]::ReadAllBytes($inputs['BRIDGE'])
  # A content swap of equal length MUST be rejected. File size checks alone
  # do not bind the actual code that ran.
  $changed = [byte[]]$original.Clone()
  $changed[5] = $changed[5] -bxor 1
  [IO.File]::WriteAllBytes($inputs['BRIDGE'], $changed)
  Assert-Fails 'same-size bridge executable replacement' {
    Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'after tone'
  }
  [IO.File]::WriteAllBytes($inputs['BRIDGE'], $original)
  Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'after restoration'

  $oldNative = [IO.File]::ReadAllBytes($inputs['NATIVE_DSP'])
  [IO.File]::WriteAllBytes($inputs['NATIVE_DSP'], [Text.Encoding]::UTF8.GetBytes('short'))
  Assert-Fails 'native C++ DSP shortened after probe' {
    Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'after source'
  }
  [IO.File]::WriteAllBytes($inputs['NATIVE_DSP'], $oldNative)

  $oldDriver = [IO.File]::ReadAllBytes($inputs['SYS'])
  [IO.File]::Delete($inputs['SYS'])
  Assert-Fails 'kernel driver removed after signature check' {
    Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'after PnP cleanup'
  }
  [IO.File]::WriteAllBytes($inputs['SYS'], $oldDriver)
  Assert-LouderMeLabInputsUnchanged -Snapshot $frozen -Phase 'after cleanup'

  $badInputs = [ordered]@{}
  foreach ($label in $inputs.Keys) { $badInputs[$label] = $inputs[$label] }
  $badInputs['INDEPENDENT_TONE'] = $inputs['BRIDGE']
  Assert-Fails 'separate Windows app and bridge point to one executable' {
    New-LouderMeLabInputSnapshot -Files $badInputs
  }
  $badInputs.Remove('INDEPENDENT_TONE')
  Assert-Fails 'missing independent application input' {
    New-LouderMeLabInputSnapshot -Files $badInputs
  }
  $badInputs['INDEPENDENT_TONE'] = $inputs['INDEPENDENT_TONE']
  $badInputs['UNEXPECTED'] = $inputs['DEVCON']
  Assert-Fails 'unexpected eight-input alias bypass' {
    New-LouderMeLabInputSnapshot -Files $badInputs
  }

  [IO.File]::WriteAllBytes($inputs['CAT'], [byte[]]::new(0))
  Assert-Fails 'empty signed catalog input' {
    New-LouderMeLabInputSnapshot -Files $inputs
  }

  # Inspect real VM caller integration without executing privileged audio.
  $runner = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'run_disposable_lab.ps1') -Raw
  foreach ($marker in @(
    'New-LouderMeLabInputSnapshot',
    'Get-LouderMeLabInputMetadata',
    'before first driver operation',
    'immediately before driver install',
    'before WASAPI endpoint discovery',
    'before virtual tone',
    'before physical tone',
    'before silence control',
    'before independent app $percent-percent probe',
    'after both independent source probes',
    'after final silence control',
    'after driver cleanup',
    'INPUTS_UNCHANGED=PASS'
  )) {
    if (-not $runner.Contains($marker)) {
      throw "Real disposable VM runner missing mandatory fingerprint check: $marker"
    }
  }
  if ($runner.Contains('LAB instance: $createdInstance') -or
      $runner.Contains('device instance: $id') -or
      $runner.Contains('LAB device $id') -or
      -not $runner.Contains('Invoke-LouderMeLabFiniteProcess -Executable $DevconExe') -or
      -not $runner.Contains("Invoke-LouderMeLabFiniteProcess -Executable 'pnputil.exe'") -or
      $runner.Contains('& $DevconExe install $inf $hardwareId') -or
      $runner.Contains('& pnputil /remove-device "$id"')) {
    throw 'Private PnP instance IDs may escape through script or native driver installer output.'
  }
  Write-Host 'PASS: seven lab inputs pinned; same-size tamper, deleted driver, missing/alias/empty files rejected; every real VM phase and cleanup gated (no devices).'
} finally {
  Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
