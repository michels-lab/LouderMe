#requires -Version 7.0
<#
Pure, fail-closed numeric proof validator for independent Windows app
WASAPI physical-loopback measurements. This module NEVER installs a driver,
opens a device, changes system volume, or claims acoustic SPL.
#>
Set-StrictMode -Version Latest

function Test-LouderMeIndependentGain {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string]$EvidenceDir,
    [Parameter(Mandatory)][string]$ExpectedSessionId
  )
  $parsedSession = [guid]::Empty
  if (-not [guid]::TryParse($ExpectedSessionId, [ref]$parsedSession) -or
      $parsedSession -eq [guid]::Empty) {
    throw 'A valid nonempty physical LAB session GUID is mandatory.'
  }
  $session = $parsedSession.ToString('D')
  $paths = @(
    'no-source-control-before.json',
    'independent-source-result-100.json',
    'independent-source-result-150.json',
    'no-source-control-after.json'
  )
  $identityFields = @(
    'virtual_endpoint_sha256', 'physical_endpoint_sha256',
    'bridge_binary_sha256', 'source_binary_sha256'
  )
  $records = @{}
  $expectedIdentity = $null
  foreach ($name in $paths) {
    $path = Join-Path $EvidenceDir $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
      throw "Missing session-bound Windows audio LAB evidence: $name"
    }
    $record = Get-Content -LiteralPath $path -Raw -ErrorAction Stop |
      ConvertFrom-Json -ErrorAction Stop
    if ($null -eq $record -or $record.session_id -cne $session) {
      throw "Mixed, missing, or stale LAB session ID in $name"
    }
    $fingerprints = @{}
    foreach ($field in $identityFields) {
      $value = $record.$field
      if ($value -isnot [string] -or $value -cnotmatch '^[a-f0-9]{64}$') {
        throw "Missing or invalid redacted LAB binary/device fingerprint $field in $name"
      }
      $fingerprints[$field] = $value
      if ($null -ne $expectedIdentity -and
          $value -cne $expectedIdentity[$field]) {
        throw "LAB audio device/binary identity changed between phases: $field in $name"
      }
    }
    if ($null -eq $expectedIdentity) {
      $expectedIdentity = $fingerprints
      if ($fingerprints.virtual_endpoint_sha256 -ceq
          $fingerprints.physical_endpoint_sha256) {
        throw 'Physical proof must use distinct virtual/physical endpoints.'
      }
    }
    $records[$name] = $record
  }
  foreach ($phase in @('before', 'after')) {
    $record = $records["no-source-control-$phase.json"]
    if ($record.status -cne 'PASS' -or
        $record.control_phase -cne $phase -or
        $record.control -cne 'negative: virtual bridge capture active, independent source absent' -or
        $record.stimulus_launched -cne $false -or
        $record.bridge_failed_for_missing_tone -cne $true -or
        $record.general_third_party_audio_proven -cne $false -or
        $record.acoustic_spl_proven -cne $false) {
      throw "Missing valid $phase no-stimulus physical listener control."
    }
    foreach ($field in @('matched440_amplitude', 'physical_monitor_frames')) {
      $value = $record.$field
      if ($null -eq $value -or $value -is [bool] -or $value -isnot [ValueType] -or
          -not [double]::IsFinite([double]$value)) {
        throw "Non-numeric $phase no-source control: $field"
      }
    }
    if ([double]$record.matched440_amplitude -lt 0 -or
        [double]$record.matched440_amplitude -gt 0.003 -or
        [double]$record.physical_monitor_frames -lt 1 -or
        [double]$record.physical_monitor_frames % 1 -ne 0) {
      throw "Invalid $phase no-source physical monitor evidence."
    }
    foreach ($field in @('virtual_processed_frames', 'physical_queued_frames',
                         'render_consumed_frames', 'render_starved_reads')) {
      $value = $record.$field
      if ($null -eq $value -or $value -is [bool] -or
          $value -isnot [ValueType] -or
          -not [double]::IsFinite([double]$value) -or
          ([double]$value % 1) -ne 0) {
        throw "Missing no-source callback/queue counter: $field at $phase."
      }
    }
    if ([double]$record.virtual_processed_frames -ne 0 -or
        [double]$record.physical_queued_frames -ne 0 -or
        [double]$record.render_consumed_frames -ne 0 -or
        [double]$record.render_starved_reads -lt 0) {
      throw "Source-free $phase control was contaminated by application PCM."
    }
  }
  $m = @{}
  foreach ($boost in @(100, 150)) {
    $record = $records["independent-source-result-$boost.json"]
    if ($null -eq $record -or $record.status -cne 'PASS' -or
        $record.stimulus -cne 'separate Windows process' -or
        $record.general_third_party_audio_proven -cne $false -or
        $record.acoustic_spl_proven -cne $false) {
      throw "Incorrect or inflated proof scope at $boost percent boost."
    }
    if ($record.boost_percent -is [bool] -or
        $record.boost_percent -isnot [ValueType] -or
        [double]$record.boost_percent -ne [double]$boost) {
      throw "Invalid or swapped recorded boost value at $boost percent."
    }
    foreach ($field in @('physical_matched440_amplitude', 'physical_coherence')) {
      $value = $record.$field
      if ($null -eq $value -or $value -is [bool] -or
          $value -isnot [ValueType]) {
        throw "Non-numeric lab PCM measurement: $field"
      }
      $numeric = [double]$value
      if (-not [double]::IsFinite($numeric)) {
        throw "Nonfinite lab PCM measurement: $field"
      }
    }
    # The trusted positive-source parser also emits frame-level evidence.
    # Require it again here: a hand-edited JSON PASS without real capture
    # counters must never survive the final 100/150 gain acceptance.
    $pcmCounters = @{}
    foreach ($field in @('physical_monitor_frames', 'virtual_processed_frames',
                         'physical_queued_frames', 'render_consumed_frames',
                         'render_starved_reads', 'dropped_frames',
                         'independent_source_frames', 'independent_source_channels')) {
      $value = $record.$field
      if ($null -eq $value -or $value -is [bool] -or
          $value -isnot [ValueType] -or
          -not [double]::IsFinite([double]$value) -or
          ([double]$value % 1) -ne 0) {
        throw "Missing or fractional physical PCM provenance: $field at $boost."
      }
      $pcmCounters[$field] = [double]$value
    }
    if ($pcmCounters.render_starved_reads -lt 0 -or
        $pcmCounters.physical_monitor_frames -lt 44100 -or
        $pcmCounters.virtual_processed_frames -lt 44100 -or
        $pcmCounters.physical_queued_frames -lt 44100 -or
        $pcmCounters.render_consumed_frames -lt 44100 -or
        $pcmCounters.render_consumed_frames -gt $pcmCounters.physical_queued_frames -or
        $pcmCounters.dropped_frames -ne 0 -or
        $pcmCounters.independent_source_frames -lt 88200 -or
        $pcmCounters.independent_source_channels -lt 1 -or
        $pcmCounters.independent_source_channels -gt 8) {
      throw "Incomplete independent-source and physical-output PCM at $boost."
    }
    $amp = [double]$record.physical_matched440_amplitude
    $coherence = [double]$record.physical_coherence
    if ($amp -le 0 -or $amp -ge 0.15 -or
        $coherence -lt 0.50 -or $coherence -gt 1.0) {
      throw "Invalid physical 440 Hz amplitude/coherence at $boost percent boost."
    }
    $m[$boost] = $amp
  }
  $ratio = $m[150] / $m[100]
  if (-not [double]::IsFinite($ratio) -or
      $ratio -lt 1.30 -or $ratio -gt 1.70) {
    throw "Independent Windows app physical gain 100 to 150% ratio failed: $ratio"
  }
  return $ratio
}
