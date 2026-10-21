#requires -Version 7.0
# Device-free negative fixtures for exact numeric physical WASAPI-loopback
# gain-comparison contract. Does NOT claim a signed/installed Windows driver.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'IndependentGainCheck.ps1')

$dir = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-IndependentProof-$([guid]::NewGuid())"
[void](New-Item -ItemType Directory -Path $dir -Force)
function Get-Baseline([int]$boost) {
  return @{
    status = 'PASS'
    stimulus = 'separate Windows process'
    boost_percent = $boost
    physical_matched440_amplitude = $(if ($boost -eq 100) { 0.020 } else { 0.030 })
    physical_coherence = 0.81
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
function Write-Pair($lo, $hi) {
  $lo | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'independent-source-result-100.json')
  $hi | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'independent-source-result-150.json')
}
function Assert-Rejected([string]$label, $lo, $hi) {
  Write-Pair $lo $hi
  $rejected = $false
  try { [void](Test-LouderMeIndependentGain -EvidenceDir $dir) }
  catch { $rejected = $true }
  if (-not $rejected) { throw "Unsafe independent-app Windows gain proof was accepted: $label" }
}
try {
  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $good = Test-LouderMeIndependentGain -EvidenceDir $dir
  if ([Math]::Abs($good - 1.5) -gt 0.000001) {
    throw "Correct 1.5x physical PCM gain evidence rejected: $good"
  }

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.boost_percent = 100
  Assert-Rejected 'swapped boost labels' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.boost_percent = 150.4
  Assert-Rejected 'fractional fake boost label' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.stimulus = 'bridge internal tone'
  Assert-Rejected 'internal tone masquerades as independent app' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.general_third_party_audio_proven = $true
  Assert-Rejected 'inflated general third-party OS-audio claim' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.acoustic_spl_proven = $true
  Assert-Rejected 'inflated physical SPL certification claim' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_coherence = '0.99'
  Assert-Rejected 'coherence provided as string' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_matched440_amplitude = [double]::NaN
  Assert-Rejected 'nonfinite amplitude' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_matched440_amplitude = 0.030
  $a.physical_matched440_amplitude = 0.0
  Assert-Rejected 'silent reference output' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_matched440_amplitude = 0.021
  Assert-Rejected 'ineffective 100 to 150% Boost' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_matched440_amplitude = 0.054
  Assert-Rejected 'excessive amplification' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.Remove('physical_matched440_amplitude')
  Assert-Rejected 'missing physical measurement' $a $b

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  Remove-Item -LiteralPath (Join-Path $dir 'independent-source-result-150.json') -Force
  $missingRejected = $false
  try { [void](Test-LouderMeIndependentGain -EvidenceDir $dir) }
  catch { $missingRejected = $true }
  if (-not $missingRejected) { throw 'Missing 150% evidence file was accepted.' }

  Write-Host 'PASS: numeric independent Windows-source physical gain validator rejects 11 adversarial false positives; no audio device used.'
} finally {
  Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
}
