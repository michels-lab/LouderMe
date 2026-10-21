#requires -Version 7.0
# Hardware-free regression: no stale LAB directory or single-use artifact may
# be silently reused, overwritten, or deleted by preflight.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LabEvidenceDirectory.ps1')

$root = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-LabEvidence-$([guid]::NewGuid())"
[void](New-Item -ItemType Directory -Path $root -ErrorAction Stop)
function Assert-Reject([string]$label, [scriptblock]$action) {
  $rejected = $false
  try { & $action | Out-Null } catch { $rejected = $true }
  if (-not $rejected) { throw "FAIL: stale or unsafe LAB evidence accepted: $label" }
}
try {
  $session = [guid]::NewGuid().ToString('D')
  $fresh = Join-Path $root 'fresh'
  $created = New-LouderMeLabEvidenceDirectory -Path $fresh -SessionId $session
  if ($created -cne [IO.Path]::GetFullPath($fresh)) { throw 'Wrong evidence directory returned.' }
  $lock = Join-Path $fresh 'lab-session.lock'
  if ((Get-Content -LiteralPath $lock -Raw).Trim() -cne $session) {
    throw 'Evidence session lock missing or incorrect.'
  }
  $positive = Join-Path $fresh 'independent-source-result-100.json'
  Write-LouderMeLabJsonOnce -Path $positive -Record @{ status = 'PASS'; session_id = $session }
  $original = Get-Content -LiteralPath $positive -Raw
  Assert-Reject 'duplicate evidence record' {
    Write-LouderMeLabJsonOnce -Path $positive -Record @{ status = 'FAKE'; session_id = $session }
  }
  if ((Get-Content -LiteralPath $positive -Raw) -cne $original) {
    throw 'Write-once JSON artifact changed after duplicate attempt.'
  }
  Assert-Reject 'reuse populated directory' {
    New-LouderMeLabEvidenceDirectory -Path $fresh -SessionId $session
  }
  if ((Get-Content -LiteralPath $positive -Raw) -cne $original) {
    throw 'Existing source evidence was changed during preflight.'
  }
  $oldEmpty = Join-Path $root 'old-empty'
  [void](New-Item -ItemType Directory -Path $oldEmpty -ErrorAction Stop)
  Assert-Reject 'reuse empty directory' {
    New-LouderMeLabEvidenceDirectory -Path $oldEmpty -SessionId $session
  }
  $filePath = Join-Path $root 'not-a-dir'
  Set-Content -LiteralPath $filePath -Value 'preserve me'
  Assert-Reject 'file collision' {
    New-LouderMeLabEvidenceDirectory -Path $filePath -SessionId $session
  }
  if ((Get-Content -LiteralPath $filePath -Raw).Trim() -cne 'preserve me') {
    throw 'Existing file was altered.'
  }
  Assert-Reject 'missing parent' {
    New-LouderMeLabEvidenceDirectory -Path (Join-Path $root 'missing/subdir') -SessionId $session
  }
  Assert-Reject 'invalid empty GUID' {
    New-LouderMeLabEvidenceDirectory -Path (Join-Path $root 'invalid') -SessionId ([guid]::Empty.ToString())
  }
  Assert-Reject 'duplicate log path' {
    Write-LouderMeLabTextOnce -Path $lock -Content 'replacement'
  }
  if ((Get-Content -LiteralPath $lock -Raw).Trim() -cne $session) {
    throw 'Write-once session marker was overwritten.'
  }
  $privatePhysical = '{0.0.0.00000000}.{PRIVATE_PHYSICAL_ENDPOINT}'
  $privateVirtual = '{0.0.0.00000000}.{PRIVATE_VIRTUAL_ENDPOINT}'
  $listLine = Protect-LouderMeLabLogLine -Line "[Speakers] $privatePhysical" -ListDevices
  if ($listLine -cne '[Speakers] [REDACTED_ENDPOINT_ID]') {
    throw 'WASAPI device list disclosure was not redacted.'
  }
  $routeLine = Protect-LouderMeLabLogLine -Line "from=$privateVirtual to=$privatePhysical" -KnownEndpointIds @($privatePhysical, $privateVirtual)
  if ($routeLine.Contains($privatePhysical) -or $routeLine.Contains($privateVirtual) -or
      $routeLine -cne 'from=[REDACTED_ENDPOINT_ID] to=[REDACTED_ENDPOINT_ID]') {
    throw 'Route trace persisted raw Windows endpoint IDs.'
  }
  $safeMarker = Protect-LouderMeLabLogLine -Line 'PASS: physical matched440=0.020 coherence=0.81' -KnownEndpointIds @($privatePhysical)
  if ($safeMarker -cne 'PASS: physical matched440=0.020 coherence=0.81') {
    throw 'Numeric nonprivate audio evidence was changed by redaction.'
  }
  Write-Host 'PASS: fresh LAB evidence ownership, write-once files, collision refusal and redacted endpoint logs (device-free).'
} finally {
  Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
