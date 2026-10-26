#requires -Version 7.0
# Device-free negative fixtures for exact numeric physical WASAPI-loopback
# gain-comparison contract. Does NOT claim a signed/installed Windows driver.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'IndependentGainCheck.ps1')

$dir = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-IndependentProof-$([guid]::NewGuid())"
[void](New-Item -ItemType Directory -Path $dir -Force)
$SessionId = [guid]::NewGuid().ToString('D')
function Get-Baseline([int]$boost) {
  return @{
    status = 'PASS'
    stimulus = 'separate Windows process'
    boost_percent = $boost
    session_id = $SessionId
    virtual_endpoint_sha256 = ('a' * 64)
    physical_endpoint_sha256 = ('b' * 64)
    bridge_binary_sha256 = ('c' * 64)
    source_binary_sha256 = ('d' * 64)
    physical_matched440_amplitude = $(if ($boost -eq 100) { 0.020 } else { 0.030 })
    physical_coherence = 0.81
    physical_monitor_frames = 192000
    virtual_processed_frames = 96000
    physical_emitted_frames = 96000
    dropped_frames = 0
    independent_source_frames = 96000
    independent_source_channels = 2
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
function Get-Control([string]$phase) {
  return @{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    control_phase = $phase
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = 0.00001
    physical_monitor_frames = 96000
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
    session_id = $SessionId
    virtual_endpoint_sha256 = ('a' * 64)
    physical_endpoint_sha256 = ('b' * 64)
    bridge_binary_sha256 = ('c' * 64)
    source_binary_sha256 = ('d' * 64)
  }
}
function Write-Pair($lo, $hi) {
  $lo | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'independent-source-result-100.json')
  $hi | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'independent-source-result-150.json')
  foreach ($phase in @('before', 'after')) {
    (Get-Control $phase) | ConvertTo-Json -Compress |
      Set-Content -LiteralPath (Join-Path $dir "no-source-control-$phase.json")
  }
}
function Assert-Rejected([string]$label, $lo, $hi) {
  Write-Pair $lo $hi
  $rejected = $false
  try { [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId) }
  catch { $rejected = $true }
  if (-not $rejected) { throw "Unsafe independent-app Windows gain proof was accepted: $label" }
}
function Assert-ControlRejected([string]$label, $before, $after) {
  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $before | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'no-source-control-before.json')
  $after | ConvertTo-Json -Compress |
    Set-Content -LiteralPath (Join-Path $dir 'no-source-control-after.json')
  $rejected = $false
  try {
    [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId)
  } catch { $rejected = $true }
  if (-not $rejected) { throw "Unsafe negative-control evidence accepted: $label" }
}
try {
  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $good = Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId
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

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.Remove('physical_monitor_frames')
  Assert-Rejected 'numeric gain without real physical monitor frame provenance' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.virtual_processed_frames = 0
  Assert-Rejected 'gain forged while virtual source captured zero PCM' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.physical_emitted_frames = 0
  Assert-Rejected 'gain forged without physical output frames' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.dropped_frames = 7
  Assert-Rejected 'gain contaminated by dropped samples' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.independent_source_frames = 1
  Assert-Rejected 'short or missing separately emitted tone' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.independent_source_channels = '2'
  Assert-Rejected 'fabricated string independent source channels' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_monitor_frames = 192000.5
  Assert-Rejected 'fractional physical frame count' $a $b

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  Remove-Item -LiteralPath (Join-Path $dir 'independent-source-result-150.json') -Force
  $missingRejected = $false
  try { [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId) }
  catch { $missingRejected = $true }
  if (-not $missingRejected) { throw 'Missing 150% evidence file was accepted.' }

  $before = Get-Control before; $after = Get-Control after
  $before.control_phase = 'after'
  Assert-ControlRejected 'incorrect baseline control phase' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.session_id = [guid]::NewGuid().ToString('D')
  Assert-ControlRejected 'control from unrelated VM run' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.physical_endpoint_sha256 = ('f' * 64)
  Assert-ControlRejected 'physical output switched after source test' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.source_binary_sha256 = ('f' * 64)
  Assert-ControlRejected 'independent app binary changed across phases' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.matched440_amplitude = 0.019
  Assert-ControlRejected 'lingering external 440Hz contaminates post control' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.physical_monitor_frames = 0
  Assert-ControlRejected 'no actual pre-source physical frames' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.physical_monitor_frames = '96000'
  Assert-ControlRejected 'fabricated string listener frame count' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.stimulus_launched = $true
  Assert-ControlRejected 'source erroneously launched in negative control' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.bridge_failed_for_missing_tone = $false
  Assert-ControlRejected 'negative control did not reject absent tone' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.virtual_endpoint_sha256 = $before.physical_endpoint_sha256
  Assert-ControlRejected 'identical virtual and physical endpoints' $before $after

  $before = Get-Control before; $after = Get-Control after
  $before.bridge_binary_sha256 = 'not-a-sha'
  Assert-ControlRejected 'unverifiable bridge binary fingerprint' $before $after

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  Remove-Item -LiteralPath (Join-Path $dir 'no-source-control-before.json') -Force
  $missingNegativeRejected = $false
  try { [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId) }
  catch { $missingNegativeRejected = $true }
  if (-not $missingNegativeRejected) { throw 'Missing silence control was accepted.' }

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $mismatch = Get-Baseline 150
  $mismatch.virtual_endpoint_sha256 = ('e' * 64)
  Assert-Rejected 'positive file from another virtual endpoint' (Get-Baseline 100) $mismatch

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $wrongSessionRejected = $false
  try {
    [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId ([guid]::NewGuid().ToString('D')))
  } catch { $wrongSessionRejected = $true }
  if (-not $wrongSessionRejected) { throw 'Wrong caller-supplied lab session ID was accepted.' }

  Write-Host 'PASS: Windows source gain validates session-bound 100/150 records, before/after silence controls and 31+ adversarial fixtures; no audio device used.'
} finally {
  Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
}
