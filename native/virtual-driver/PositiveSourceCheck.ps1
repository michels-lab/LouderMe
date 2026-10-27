#requires -Version 7.0
<#
Pure fail-closed parser for a separately launched Windows source process
and the bridge's real physical WASAPI-loopback report. No driver install,
endpoint changes, audio capture, or physical QA is performed by this module.
#>
Set-StrictMode -Version Latest

function Test-LouderMePositiveSourceRoute {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr,
    [Parameter(Mandatory)][int]$SourceExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$SourceStdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$SourceStdErr
  )
  if ($ExitCode -ne 0 -or $SourceExitCode -ne 0 -or
      $StdErr -match '(?im)^(FAIL:|Recoverable LAB route interruption:)' -or
      $SourceStdErr -match '(?im)^EXTERNAL_APP_TONE_FAILED') {
    throw 'Bridge or separate source process failed; no independent route proof.'
  }
  if ([regex]::Matches($StdOut, '(?m)^EXTERNAL_SOURCE_READY\r?$').Count -ne 1) {
    throw 'Independent source must follow exactly one live bridge readiness marker.'
  }
  $finished = [regex]::Matches($SourceStdOut,
    '(?m)^EXTERNAL_APP_TONE_FINISHED 440Hz frames=(?<frames>\d+) channels=(?<channels>\d+); physical loopback is verified only by the separate bridge\.\r?$')
  if ($finished.Count -ne 1) {
    throw 'Separate Windows source process did not conclusively finish exactly one tone.'
  }
  $success = [regex]::Matches($StdOut,
    '(?m)^PASS: LAB independent source Windows process -> selected virtual endpoint -> own C\+\+ DSP -> separate physical WASAPI loopback 440 Hz identity\. NOT general third-party/default-app audio proof or acoustic loudness\.\r?$')
  if ($success.Count -ne 1 -or
      [regex]::Matches($StdOut, '(?m)^PASS: LAB independent source').Count -ne 1) {
    throw 'Bridge emitted missing, duplicate, or inflated independent route PASS.'
  }
  $measurements = [regex]::Matches($StdOut,
    '(?m)^Physical LAB proof: frames=(?<frames>\d+) RMS=(?<rms>\d+(?:\.\d+)?) peak=(?<peak>\d+(?:\.\d+)?) matched440=(?<tone>\d+(?:\.\d+)?) coherence=(?<coherence>\d+(?:\.\d+)?) virtualProcessed=(?<virtual>\d+) physicalQueued=(?<queued>\d+) renderConsumed=(?<consumed>\d+) starvedRenderReads=(?<starved>\d+) dropped=(?<drops>\d+)\r?$')
  if ($measurements.Count -ne 1 -or
      [regex]::Matches($StdOut, '(?m)^Physical LAB proof:').Count -ne 1) {
    throw 'A unique, complete physical-route measurement is mandatory.'
  }
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $metric = $measurements[0]
  $number = @{}
  foreach ($field in @('frames', 'rms', 'peak', 'tone', 'coherence',
                        'virtual', 'queued', 'consumed', 'starved', 'drops')) {
    $number[$field] = [double]::Parse($metric.Groups[$field].Value, $culture)
    if (-not [double]::IsFinite($number[$field])) {
      throw "Invalid physical monitor measurement: $field"
    }
  }
  $sourceFrames = [double]::Parse($finished[0].Groups['frames'].Value, $culture)
  $sourceChannels = [double]::Parse($finished[0].Groups['channels'].Value, $culture)
  if (-not [double]::IsFinite($sourceFrames) -or
      -not [double]::IsFinite($sourceChannels) -or
      $sourceFrames -lt 88200 -or $sourceFrames % 1 -ne 0 -or
      $sourceChannels -lt 1 -or $sourceChannels -gt 8 -or
      $sourceChannels % 1 -ne 0) {
    throw 'Separate 440Hz source did not report plausible real PCM output.'
  }
  if ($number.frames -lt 44100 -or
      $number.virtual -lt 44100 -or
      $number.queued -lt 44100 -or
      $number.consumed -lt 44100 -or
      $number.consumed -gt $number.queued -or
      $number.drops -ne 0 -or
      $number.tone -le 0 -or $number.tone -ge 0.15 -or
      $number.coherence -lt 0.50 -or $number.coherence -gt 1.05 -or
      $number.rms -le 0 -or $number.rms -ge 0.15 -or
      $number.peak -lt $number.rms -or $number.peak -gt 1) {
    throw 'Physical loopback missing, clipped, contaminated, or dropped samples.'
  }
  return [ordered]@{
    physical_matched440_amplitude = $number.tone
    physical_coherence = $number.coherence
    physical_monitor_frames = [long]$number.frames
    virtual_processed_frames = [long]$number.virtual
    physical_queued_frames = [long]$number.queued
    render_consumed_frames = [long]$number.consumed
    render_starved_reads = [long]$number.starved
    dropped_frames = [long]$number.drops
    independent_source_frames = [long]$sourceFrames
    independent_source_channels = [int]$sourceChannels
  }
}
