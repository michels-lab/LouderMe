#requires -Version 7.0
<#
Fail-closed silence causality verifier for a real installed Windows VM route.
This module only validates observed process logs; it does not access audio
devices, create PCM, generate fake sound or install virtual endpoints.
#>
Set-StrictMode -Version Latest

function Test-LouderMeNoSourceControl {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr
  )
  $readyCount = [regex]::Matches(
    $StdOut, '(?m)^EXTERNAL_SOURCE_READY\r?$').Count
  if ($ExitCode -ne 4 -or $readyCount -ne 1) {
    throw 'No-source control never completed its real listener window with documented tone rejection.'
  }
  if ($StdOut -match 'PASS: LAB independent source' -or
      $StdErr -notmatch 'FAIL: no matching lab tone reached the physical render loopback') {
    throw 'No-source capture was accepted or did not fail specifically for missing source tone.'
  }
  $pattern = '(?m)^Physical LAB proof: frames=(?<frames>\d+) RMS=(?<rms>\d+(?:\.\d+)?) peak=(?<peak>\d+(?:\.\d+)?) matched440=(?<tone>\d+(?:\.\d+)?) coherence=(?<coherence>\d+(?:\.\d+)?) virtualProcessed=(?<virtual>\d+) physicalQueued=(?<queued>\d+) renderConsumed=(?<consumed>\d+) starvedRenderReads=(?<starved>\d+) dropped=(?<drops>\d+)\r?$'
  $metrics = [regex]::Matches($StdOut, $pattern)
  if ($metrics.Count -ne 1) {
    throw 'No-source control requires exactly one complete physical WASAPI monitor measurement.'
  }
  $m = $metrics[0]
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $tone = [double]::Parse($m.Groups['tone'].Value, $culture)
  $coherence = [double]::Parse($m.Groups['coherence'].Value, $culture)
  $frames = [long]::Parse($m.Groups['frames'].Value, $culture)
  $drops = [long]::Parse($m.Groups['drops'].Value, $culture)
  $virtual = [long]::Parse($m.Groups['virtual'].Value, $culture)
  $queued = [long]::Parse($m.Groups['queued'].Value, $culture)
  $consumed = [long]::Parse($m.Groups['consumed'].Value, $culture)
  $starved = [long]::Parse($m.Groups['starved'].Value, $culture)
  if (-not [double]::IsFinite($tone) -or $tone -lt 0 -or $tone -gt 0.003 -or
      -not [double]::IsFinite($coherence) -or $coherence -lt 0 -or $coherence -gt 1.0 -or
      $frames -le 0 -or $drops -ne 0 -or $consumed -gt $queued -or
      $virtual -ne 0 -or $queued -ne 0 -or $consumed -ne 0 -or
      $starved -lt 0) {
    throw 'No physical monitor data, lost packets, or external 440 Hz contamination detected.'
  }
  return [ordered]@{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = $tone
    physical_monitor_frames = $frames
    virtual_processed_frames = $virtual
    physical_queued_frames = $queued
    render_consumed_frames = $consumed
    render_starved_reads = $starved
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
