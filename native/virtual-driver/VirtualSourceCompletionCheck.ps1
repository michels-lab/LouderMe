#requires -Version 7.0
# Device-free contract for the independent Windows process, NOT sound proof.
function Test-LouderMeVirtualSourceCompletion {
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdOut,
    [Parameter(Mandatory)][AllowEmptyString()][string]$StdErr
  )
  if ($ExitCode -ne 0 -or -not [string]::IsNullOrWhiteSpace($StdErr)) {
    throw 'Independent source exited unsuccessfully or wrote to stderr.'
  }
  $complete = [regex]::Matches($StdOut,
    '(?m)^EXTERNAL_APP_TONE_FINISHED 440Hz frames=(?<frames>[0-9]+) channels=(?<channels>1|2|6|8); physical loopback is verified only by the separate bridge\.\r?$')
  $markers = [regex]::Matches($StdOut, '(?m)^EXTERNAL_APP_TONE_FINISHED')
  if ($complete.Count -ne 1 -or $markers.Count -ne 1) {
    throw 'Exactly one valid 1/2/6/8-channel independent PCM completion required.'
  }
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $frames = [long]::Parse($complete[0].Groups['frames'].Value, $culture)
  $channels = [int]::Parse($complete[0].Groups['channels'].Value, $culture)
  # Source emits exactly two seconds at rate 8..192 kHz.
  if ($frames -lt 16000 -or $frames -gt 384000 -or $frames % 2 -ne 0) {
    throw 'Separate Windows source supplied invalid two-second PCM frame count.'
  }
  return @{ frames = $frames; channels = $channels }
}
