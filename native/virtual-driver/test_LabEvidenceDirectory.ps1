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
  Write-Host 'PASS: fresh LAB evidence ownership, immutable JSON/log files, collision refusal and data preservation (device-free).'
} finally {
  Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
