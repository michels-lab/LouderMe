#requires -Version 7.0
# Strict source+receive-only virtual speaker map; no DSP or real physical audio.
function Test-LouderMeSpeakerMapSource {
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr
  )
  if ($ExitCode -ne 0 -or -not [string]::IsNullOrWhiteSpace($StdErr)) {
    throw 'Independent speaker-map source failed or wrote to stderr.'
  }
  $match = [regex]::Matches($StdOut,
    '(?m)^EXTERNAL_APP_SPEAKER_MAP_FINISHED frames=(?<frames>[0-9]+) channels=(?<channels>2|6|8); virtual speaker mapping is verified only by the separate bridge\.\r?$')
  if ($match.Count -ne 1 -or
      [regex]::Matches($StdOut, '(?m)^EXTERNAL_APP_SPEAKER_MAP_FINISHED').Count -ne 1) {
    throw 'Exactly one independent 2/5.1/7.1 speaker-map source marker required.'
  }
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $frames = [long]::Parse($match[0].Groups['frames'].Value, $culture)
  $channels = [int]::Parse($match[0].Groups['channels'].Value, $culture)
  if ($frames -lt 16000 -or $frames -gt 384000 -or $frames % 2 -ne 0) {
    throw 'Independent speaker-map source has invalid two-second PCM duration.'
  }
  return @{ frames = $frames; channels = $channels }
}
function Test-LouderMeSpeakerMapReceiver {
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr,
    [Parameter(Mandatory)][int]$ExpectedChannels,
    [Parameter(Mandatory)][long]$SourceFrames
  )
  if ($ExitCode -ne 0 -or -not [string]::IsNullOrWhiteSpace($StdErr)) {
    throw 'Virtual speaker-map capture exited unsuccessfully.'
  }
  if ([regex]::Matches($StdOut, '(?m)^EXTERNAL_VIRTUAL_READY\r?$').Count -ne 1 -or
      [regex]::Matches($StdOut,
        '(?m)^PASS: independent Windows process -> LouderMe virtual WASAPI positional speaker map\. No physical output or DSP was opened\.\r?$').Count -ne 1 -or
      [regex]::Matches($StdOut, '(?m)^PASS: independent Windows process').Count -ne 1) {
    throw 'Speaker-map capture needs exactly one readiness and narrow PASS marker.'
  }
  $found = [regex]::Matches($StdOut,
    '(?m)^VIRTUAL_SPEAKER_MAP_PROOF frames=(?<frames>[0-9]+) channels=(?<channels>[0-9]+) minOwn=(?<min>[0-9]+\.[0-9]{6}) maxLeakage=(?<leak>[0-9]+\.[0-9]{6})\r?$')
  if ($found.Count -ne 1 -or
      [regex]::Matches($StdOut, '(?m)^VIRTUAL_SPEAKER_MAP_PROOF').Count -ne 1) {
    throw 'Unique numeric real virtual WASAPI speaker-position proof missing.'
  }
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $frames = [long]::Parse($found[0].Groups['frames'].Value, $culture)
  $channels = [int]::Parse($found[0].Groups['channels'].Value, $culture)
  $own = [double]::Parse($found[0].Groups['min'].Value, $culture)
  $leakage = [double]::Parse($found[0].Groups['leak'].Value, $culture)
  if ($ExpectedChannels -notin @(2, 6, 8) -or
      $SourceFrames -lt 16000 -or $SourceFrames -gt 384000 -or
      $channels -ne $ExpectedChannels -or
      $frames -lt $SourceFrames -or
      $frames -gt 10 * $SourceFrames -or
      -not [double]::IsFinite($own) -or -not [double]::IsFinite($leakage) -or
      $own -lt 0.004 -or $own -gt 0.15 -or
      $leakage -lt 0 -or $leakage -gt $own * 0.30) {
    throw 'Missing, swapped, contaminated or unrelated virtual speaker identity.'
  }
  return @{
    source_frames = $SourceFrames
    monitor_frames = $frames
    channels = $channels
    min_own_amplitude = $own
    max_leakage_amplitude = $leakage
  }
}
