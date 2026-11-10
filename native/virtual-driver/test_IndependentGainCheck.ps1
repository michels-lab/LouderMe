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
    physical_queued_frames = 96000
    render_consumed_frames = 90000
    render_starved_reads = 8
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
    virtual_processed_frames = 0
    physical_queued_frames = 0
    render_consumed_frames = 0
    render_starved_reads = 20
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
  $b.physical_coherence = 1.01
  Assert-Rejected 'impossible coherence above mathematical maximum one' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.render_starved_reads = -1
  Assert-Rejected 'negative render starvation counter' $a $b

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
  $a.render_consumed_frames = 0
  Assert-Rejected 'gain forged without physical callback reads' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.physical_queued_frames = 0
  Assert-Rejected 'physical PCM was never enqueued' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.render_consumed_frames = 96001
  Assert-Rejected 'render callback consumed more than enqueued PCM' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.Remove('render_starved_reads')
  Assert-Rejected 'render underflow provenance missing' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.render_consumed_frames = '90000'
  Assert-Rejected 'renderer frames forged as string' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.physical_emitted_frames = 90000
  $b.Remove('render_consumed_frames')
  Assert-Rejected 'obsolete physicalEmitted queue-only report' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.dropped_frames = 7
  Assert-Rejected 'gain contaminated by dropped samples' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.independent_source_frames = 1
  Assert-Rejected 'short or missing separately emitted tone' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $a.independent_source_channels = '2'
  Assert-Rejected 'fabricated string independent source channels' $a $b

  # Different 2ch/5.1/7.1 source layouts must not turn a downmix
  # amplitude change into fake evidence of an effective Boost slider.
  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.independent_source_channels = 6
  Assert-Rejected '100/150 compared different stereo and 5.1 inputs' $a $b

  $a = Get-Baseline 100; $b = Get-Baseline 150
  $b.independent_source_frames = 88200
  Assert-Rejected '100/150 compared different input sample rates' $a $b

  foreach ($bad in @(3, 4, 5, 7)) {
    $a = Get-Baseline 100; $b = Get-Baseline 150
    $b.independent_source_channels = $bad
    Assert-Rejected "unknown surround input $bad" $a $b
  }
  foreach ($good in @(6, 8)) {
    $a = Get-Baseline 100; $b = Get-Baseline 150
    $a.independent_source_channels = $good
    $b.independent_source_channels = $good
    Write-Pair $a $b
    $ratio = Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId
    if ([Math]::Abs($ratio - 1.5) -gt 0.000001) {
      throw "Valid 5.1/7.1 equal-source Boost measurement rejected: $good"
    }
  }

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
  $before.virtual_processed_frames = 44100
  Assert-ControlRejected 'negative control captured unrelated source PCM' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.render_consumed_frames = 500
  Assert-ControlRejected 'negative control silently rendered background audio' $before $after

  $before = Get-Control before; $after = Get-Control after
  $after.Remove('render_starved_reads')
  Assert-ControlRejected 'missing renderer starvation accounting on negative control' $before $after

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

  # The VM uses -RequireVirtualProof: the positive virtual-only stage MUST
  # match the final physical A/B records. Older pure gain-fixture tests remain
  # valid without this opt-in phase, but the real VM can never omit it.
  function Get-VirtualProof {
    return @{
      status = 'PASS'
      stimulus = 'separate Windows process'
      session_id = $SessionId
      virtual_endpoint_sha256 = ('a' * 64)
      bridge_binary_sha256 = ('c' * 64)
      source_binary_sha256 = ('d' * 64)
      virtual_monitor_frames = 192000
      independent_source_frames = 96000
      independent_source_channels = 2
      virtual_matched440_amplitude = 0.011
      virtual_coherence = 0.75
      speaker_map_verified = $true
      speaker_map_channels = 2
      speaker_map_min_own_amplitude = 0.011
      speaker_map_max_leakage_amplitude = 0.0003
      physical_output_verified = $false
      global_windows_audio_verified = $false
      acoustic_spl_verified = $false
    }
  }
  function Write-Virtual($record) {
    $record | ConvertTo-Json -Compress |
      Set-Content -LiteralPath (Join-Path $dir 'independent-virtual-source-proof.json')
  }
  function Get-VirtualSilence([string]$phase) {
    return @{
      status = 'PASS'
      control = 'negative: virtual WASAPI listener active, independent source absent'
      control_phase = $phase
      stimulus_launched = $false
      bridge_rejected_for_no_tone = $true
      session_id = $SessionId
      virtual_endpoint_sha256 = ('a' * 64)
      bridge_binary_sha256 = ('c' * 64)
      source_binary_sha256 = ('d' * 64)
      virtual_monitor_frames = 0
      virtual_matched440_amplitude = 0.0
      virtual_coherence = 0.0
      physical_output_verified = $false
      global_windows_audio_verified = $false
      acoustic_spl_verified = $false
    }
  }
  function Write-VirtualSilence($record) {
    $record | ConvertTo-Json -Compress |
      Set-Content -LiteralPath (Join-Path $dir "virtual-no-source-$($record.control_phase).json")
  }
  function Write-ValidVirtualSilence {
    Write-VirtualSilence (Get-VirtualSilence 'before')
    Write-VirtualSilence (Get-VirtualSilence 'after')
  }
  function Assert-VirtualSilenceRejected([string]$label, $before, $after) {
    Write-Pair (Get-Baseline 100) (Get-Baseline 150)
    Write-Virtual (Get-VirtualProof)
    Write-VirtualSilence $before
    Write-VirtualSilence $after
    $rejected = $false
    try {
      [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof)
    } catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid virtual-only silence accepted: $label" }
  }
  function Assert-VirtualRejected([string]$label, $record) {
    Write-Pair (Get-Baseline 100) (Get-Baseline 150)
    Write-Virtual $record
    Write-ValidVirtualSilence
    $rejected = $false
    try {
      [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof)
    } catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid virtual-only PCM proof accepted: $label" }
  }

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $virtualProof = Join-Path $dir 'independent-virtual-source-proof.json'
  if (Test-Path -LiteralPath $virtualProof) { Remove-Item -LiteralPath $virtualProof -Force }
  $missingVirtualRejected = $false
  try {
    [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof)
  } catch { $missingVirtualRejected = $true }
  if (-not $missingVirtualRejected) { throw 'Missing independent virtual source proof accepted.' }

  Write-Virtual (Get-VirtualProof)
  Write-ValidVirtualSilence
  $validVirtualRatio = Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof
  if ([Math]::Abs($validVirtualRatio - 1.5) -gt 0.000001) {
    throw 'Correct session-linked virtual→physical 100/150 gain proof rejected.'
  }
  $v = Get-VirtualProof; $v.session_id = [guid]::NewGuid().ToString('D')
  Assert-VirtualRejected 'unrelated session' $v
  $v = Get-VirtualProof; $v.virtual_endpoint_sha256 = ('f' * 64)
  Assert-VirtualRejected 'different virtual render endpoint' $v
  $v = Get-VirtualProof; $v.bridge_binary_sha256 = ('f' * 64)
  Assert-VirtualRejected 'different bridge binary' $v
  $v = Get-VirtualProof; $v.source_binary_sha256 = ('f' * 64)
  Assert-VirtualRejected 'different independent source executable' $v
  $v = Get-VirtualProof; $v.stimulus = 'bridge generated test tone'
  Assert-VirtualRejected 'source not independently generated' $v
  $v = Get-VirtualProof; $v.physical_output_verified = $true
  Assert-VirtualRejected 'virtual-only proof falsely certifies physical output' $v
  $v = Get-VirtualProof; $v.Remove('independent_source_frames')
  Assert-VirtualRejected 'missing virtual independent-source frame count' $v
  $v = Get-VirtualProof; $v.independent_source_frames = 96001
  Assert-VirtualRejected 'virtual tone source duration not an integral two seconds' $v
  $v = Get-VirtualProof; $v.independent_source_channels = 3
  Assert-VirtualRejected 'virtual source unsupported speaker count' $v
  $v = Get-VirtualProof; $v.independent_source_channels = '6'
  Assert-VirtualRejected 'source speaker count supplied as text' $v

  # The real VM must match the source channels across its virtual-only
  # capture and BOTH native DSP / physical gain passes.
  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  $v = Get-VirtualProof; $v.independent_source_channels = 6
  Write-Virtual $v
  Write-ValidVirtualSilence
  $crossPhaseRejected = $false
  try {
    [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof)
  } catch { $crossPhaseRejected = $true }
  if (-not $crossPhaseRejected) {
    throw 'Virtual 5.1 source incorrectly accepted as stereo physical A/B.'
  }

  $v = Get-VirtualProof; $v.speaker_map_verified = $false
  Assert-VirtualRejected '2ch virtual proof omitted separate speaker-position measurement' $v
  $v = Get-VirtualProof; $v.Remove('speaker_map_verified')
  Assert-VirtualRejected 'speaker-position verification field absent' $v
  $v = Get-VirtualProof; $v.speaker_map_channels = 6
  Assert-VirtualRejected 'physical mono/stereo source falsely tied to 5.1 speaker-map evidence' $v
  $v = Get-VirtualProof; $v.speaker_map_min_own_amplitude = 0.001
  Assert-VirtualRejected 'one channel lacks expected speaker-frequency signature' $v
  $v = Get-VirtualProof; $v.speaker_map_max_leakage_amplitude = 0.009
  Assert-VirtualRejected 'surround lanes were swapped or crossmixed' $v
  $v = Get-VirtualProof; $v.speaker_map_max_leakage_amplitude = '0.001'
  Assert-VirtualRejected 'speaker-map amplitudes were forged as text' $v
  $v = Get-VirtualProof; $v.speaker_map_channels = 2.5
  Assert-VirtualRejected 'fractional speaker-map channel count' $v

  foreach ($layout in @(6, 8)) {
    $a = Get-Baseline 100; $b = Get-Baseline 150
    $a.independent_source_channels = $layout
    $b.independent_source_channels = $layout
    Write-Pair $a $b
    $v = Get-VirtualProof
    $v.independent_source_channels = $layout
    $v.speaker_map_channels = $layout
    Write-Virtual $v
    Write-ValidVirtualSilence
    $matching = Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof
    if ([Math]::Abs($matching - 1.5) -gt 0.000001) {
      throw "Matching 5.1/7.1 speaker-map virtual→physical Boost gate rejected: $layout"
    }
  }

  $v = Get-VirtualProof; $v.virtual_monitor_frames = 0
  Assert-VirtualRejected 'no actual virtual monitor samples' $v
  $v = Get-VirtualProof; $v.virtual_matched440_amplitude = 0.001
  Assert-VirtualRejected 'silence in virtual source proof' $v
  $v = Get-VirtualProof; $v.virtual_coherence = 1.01
  Assert-VirtualRejected 'impossible virtual coherence' $v

  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $b.virtual_matched440_amplitude = 0.012
  Assert-VirtualSilenceRejected 'pre-source 440 Hz is already present' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.virtual_matched440_amplitude = 0.008
  Assert-VirtualSilenceRejected 'post-source 440 Hz failed to stop' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.stimulus_launched = $true
  Assert-VirtualSilenceRejected 'post negative control launched independent source' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $b.virtual_endpoint_sha256 = ('f' * 64)
  Assert-VirtualSilenceRejected 'pre control uses wrong virtual endpoint' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.source_binary_sha256 = ('f' * 64)
  Assert-VirtualSilenceRejected 'post control uses different source binary' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $b.session_id = [guid]::NewGuid().ToString('D')
  Assert-VirtualSilenceRejected 'pre control from another session' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.virtual_coherence = 1.02
  Assert-VirtualSilenceRejected 'post control impossible coherence' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.bridge_rejected_for_no_tone = $false
  Assert-VirtualSilenceRejected 'post control falsely accepted absent source' $b $a
  $b = Get-VirtualSilence before; $a = Get-VirtualSilence after
  $a.virtual_monitor_frames = '0'
  Assert-VirtualSilenceRejected 'non-numeric virtual frame count' $b $a

  Write-Pair (Get-Baseline 100) (Get-Baseline 150)
  Write-Virtual (Get-VirtualProof)
  Write-ValidVirtualSilence
  Remove-Item -LiteralPath (Join-Path $dir 'virtual-no-source-after.json') -Force
  $noPostRejected = $false
  try {
    [void](Test-LouderMeIndependentGain -EvidenceDir $dir -ExpectedSessionId $SessionId -RequireVirtualProof)
  } catch { $noPostRejected = $true }
  if (-not $noPostRejected) { throw 'Missing post-source virtual silence control was accepted.' }

  Write-Host 'PASS: Windows source gain validates session-bound 100/150 records, before/after silence controls and 41+ adversarial fixtures; no audio device used.'
} finally {
  Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
}
