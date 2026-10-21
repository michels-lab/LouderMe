#requires -Version 7.0
<#
Fail-closed no-stimulus negative control for the SAME installed VM route.
The bridge must begin real WASAPI capture, run its complete measurement
window, and reject the absence of externally generated 440 Hz PCM.
Do not treat a missing driver, an aborted process, or no capture as PASS.
No audio recordings, endpoints, or system state changes occur here.
#>
Set-StrictMode -Version Latest

function Test-LouderMeNoSourceControl {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr
  )
  if ($ExitCode -ne 4 -or
      [regex]::Matches($StdOut, '(?m)^EXTERNAL_SOURCE_READY\r?) {
    throw 'No-source control did not reach the real listeners and expected evidence failure.'
  }
  if ($StdOut -match 'PASS: LAB independent source' -or
      $StdErr -notmatch 'FAIL: no matching lab tone reached the physical render loopback') {
    throw 'No-source route was accepted or did not fail for missing audio.'
  }
  $metrics = [regex]::Matches($StdOut,
    '(?m)^Physical LAB proof: frames=(?<frames>\d+) RMS=(?<rms>\d+(?:\.\d+)?) peak=(?<peak>\d+(?:\.\d+)?) matched440=(?<tone>\d+(?:\.\d+)?) coherence=(?<coherence>\d+(?:\.\d+)?) virtualProcessed=(?<virtual>\d+) physicalEmitted=(?<physical>\d+) dropped=(?<drops>\d+)\r?
  if ($metrics.Count -ne 1) {
    throw 'No-source control requires exactly one complete actual physical WASAPI measurement.'
  }
  $m = $metrics[0]
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $tone = [double]::Parse($m.Groups['tone'].Value, $culture)
  $coherence = [double]::Parse($m.Groups['coherence'].Value, $culture)
  $frames = [long]::Parse($m.Groups['frames'].Value, $culture)
  $drops = [long]::Parse($m.Groups['drops'].Value, $culture)
  if (-not [double]::IsFinite($tone) -or $tone -lt 0 -or $tone -gt .003 -or
      -not [double]::IsFinite($coherence) -or $coherence -lt 0 -or $coherence -gt 1.05 -or
      $frames -le 0 -or $drops -ne 0) {
    throw 'Physical listener missing, contaminated by 440 Hz sound, or audio packets lost.'
  }
  return [ordered]@{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = $tone
    physical_monitor_frames = $frames
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
).Count -ne 1) {
    throw 'No-source control did not reach the real listeners and expected evidence failure.'
  }
  if ($StdOut -match 'PASS: LAB independent source' -or
      $StdErr -notmatch 'FAIL: no matching lab tone reached the physical render loopback') {
    throw 'No-source route was accepted or did not fail for missing audio.'
  }
  $metrics = [regex]::Matches($StdOut,
    '(?m)^Physical LAB proof: frames=(?<frames>\d+) RMS=(?<rms>\d+(?:\.\d+)?) peak=(?<peak>\d+(?:\.\d+)?) matched440=(?<tone>\d+(?:\.\d+)?) coherence=(?<coherence>\d+(?:\.\d+)?) virtualProcessed=(?<virtual>\d+) physicalEmitted=(?<physical>\d+) dropped=(?<drops>\d+)$')
  if ($metrics.Count -ne 1) {
    throw 'No-source control requires exactly one complete actual physical WASAPI measurement.'
  }
  $m = $metrics[0]
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $tone = [double]::Parse($m.Groups['tone'].Value, $culture)
  $coherence = [double]::Parse($m.Groups['coherence'].Value, $culture)
  $frames = [long]::Parse($m.Groups['frames'].Value, $culture)
  $drops = [long]::Parse($m.Groups['drops'].Value, $culture)
  if (-not [double]::IsFinite($tone) -or $tone -lt 0 -or $tone -gt .003 -or
      -not [double]::IsFinite($coherence) -or $coherence -lt 0 -or $coherence -gt 1.05 -or
      $frames -le 0 -or $drops -ne 0) {
    throw 'Physical listener missing, contaminated by 440 Hz sound, or audio packets lost.'
  }
  return [ordered]@{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = $tone
    physical_monitor_frames = $frames
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
)
  if ($metrics.Count -ne 1) {
    throw 'No-source control requires exactly one complete actual physical WASAPI measurement.'
  }
  $m = $metrics[0]
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $tone = [double]::Parse($m.Groups['tone'].Value, $culture)
  $coherence = [double]::Parse($m.Groups['coherence'].Value, $culture)
  $frames = [long]::Parse($m.Groups['frames'].Value, $culture)
  $drops = [long]::Parse($m.Groups['drops'].Value, $culture)
  if (-not [double]::IsFinite($tone) -or $tone -lt 0 -or $tone -gt .003 -or
      -not [double]::IsFinite($coherence) -or $coherence -lt 0 -or $coherence -gt 1.05 -or
      $frames -le 0 -or $drops -ne 0) {
    throw 'Physical listener missing, contaminated by 440 Hz sound, or audio packets lost.'
  }
  return [ordered]@{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = $tone
    physical_monitor_frames = $frames
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
).Count -ne 1) {
    throw 'No-source control did not reach the real listeners and expected evidence failure.'
  }
  if ($StdOut -match 'PASS: LAB independent source' -or
      $StdErr -notmatch 'FAIL: no matching lab tone reached the physical render loopback') {
    throw 'No-source route was accepted or did not fail for missing audio.'
  }
  $metrics = [regex]::Matches($StdOut,
    '(?m)^Physical LAB proof: frames=(?<frames>\d+) RMS=(?<rms>\d+(?:\.\d+)?) peak=(?<peak>\d+(?:\.\d+)?) matched440=(?<tone>\d+(?:\.\d+)?) coherence=(?<coherence>\d+(?:\.\d+)?) virtualProcessed=(?<virtual>\d+) physicalEmitted=(?<physical>\d+) dropped=(?<drops>\d+)$')
  if ($metrics.Count -ne 1) {
    throw 'No-source control requires exactly one complete actual physical WASAPI measurement.'
  }
  $m = $metrics[0]
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $tone = [double]::Parse($m.Groups['tone'].Value, $culture)
  $coherence = [double]::Parse($m.Groups['coherence'].Value, $culture)
  $frames = [long]::Parse($m.Groups['frames'].Value, $culture)
  $drops = [long]::Parse($m.Groups['drops'].Value, $culture)
  if (-not [double]::IsFinite($tone) -or $tone -lt 0 -or $tone -gt .003 -or
      -not [double]::IsFinite($coherence) -or $coherence -lt 0 -or $coherence -gt 1.05 -or
      $frames -le 0 -or $drops -ne 0) {
    throw 'Physical listener missing, contaminated by 440 Hz sound, or audio packets lost.'
  }
  return [ordered]@{
    status = 'PASS'
    control = 'negative: virtual bridge capture active, independent source absent'
    stimulus_launched = $false
    bridge_failed_for_missing_tone = $true
    matched440_amplitude = $tone
    physical_monitor_frames = $frames
    general_third_party_audio_proven = $false
    acoustic_spl_proven = $false
  }
}
