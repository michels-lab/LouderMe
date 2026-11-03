#requires -Version 7.0
<#
Pure, device-free check for a COMPLETE 5-second virtual WASAPI receive-only
control when no independent Windows source executable was started.
A missing source is expected to return exit code 3, not success.
No PnP, audio API, disk capture, defaults or privileged side effects.
#>
Set-StrictMode -Version Latest

function Test-LouderMeVirtualNoSource {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [AllowEmptyString()][Parameter(Mandatory)][string]$StdOut,
    [AllowEmptyString()][Parameter(Mandatory)][string]$StdErr,
    [Parameter(Mandatory)][ValidateSet('before', 'after')][string]$Phase
  )
  # The bridge must finish normally after StartRecording and reject
  # no-stimulus audio; exit 0 would indicate a false 440-Hz acceptance.
  if ($ExitCode -ne 3) {
    throw "No-source virtual listener did not reject absent 440-Hz PCM at $Phase."
  }
  $outLines = @($StdOut -split '\r?\n')
  $errLines = @($StdErr.Trim() -split '\r?\n')
  if (@($outLines | Where-Object { $_ -ceq 'EXTERNAL_VIRTUAL_READY' }).Count -ne 1 -or
      @($outLines | Where-Object { $_ -match '^PASS:|EXTERNAL_APP_TONE_FINISHED' }).Count -ne 0) {
    throw "Virtual $Phase control lacked capture readiness or falsely accepted a source."
  }
  if ($errLines.Count -ne 1 -or $errLines[0] -cne
      'FAIL: no matching 440 Hz tone in the virtual WASAPI loopback. Unrelated energy, silence and synthetic driver tones do not qualify.') {
    throw "Virtual $Phase control did not reach the intended no-tone detector."
  }
  $pattern = '^VIRTUAL_EXTERNAL_PROOF frames=(?<frames>[0-9]+) matched440=(?<amplitude>[0-9]+\.[0-9]{6}) coherence=(?<coherence>[0-9]+\.[0-9]{4})$'
  $proofLines = @($outLines | Where-Object { $_ -match '^VIRTUAL_EXTERNAL_PROOF' })
  if ($proofLines.Count -ne 1) {
    throw "Virtual $Phase no-source control missing unique numeric measurement."
  }
  $proof = [regex]::Match($proofLines[0], $pattern)
  if (-not $proof.Success) {
    throw "Virtual $Phase no-source evidence has invalid numeric precision."
  }
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $frames = [long]::Parse($proof.Groups['frames'].Value, $culture)
  $amplitude = [double]::Parse($proof.Groups['amplitude'].Value, $culture)
  $coherence = [double]::Parse($proof.Groups['coherence'].Value, $culture)
  # On an idle WASAPI endpoint Windows may emit no loopback packets at all;
  # zero frames is legitimate ONLY after a completed receiver window and
  # exit code 3. With packets, no matched 440-Hz signal may exceed 0.003.
  if ($frames -lt 0 -or $frames -gt 10000000 -or
      -not [double]::IsFinite($amplitude) -or
      -not [double]::IsFinite($coherence) -or
      $amplitude -lt 0 -or $amplitude -gt 0.003 -or
      $coherence -lt 0 -or $coherence -gt 1.0001) {
    throw "Virtual $Phase control contains residual 440 Hz or impossible capture data."
  }
  return @{
    status = 'PASS'
    control = 'negative: virtual WASAPI listener active, independent source absent'
    control_phase = $Phase
    stimulus_launched = $false
    bridge_rejected_for_no_tone = $true
    virtual_monitor_frames = $frames
    virtual_matched440_amplitude = $amplitude
    virtual_coherence = $coherence
    physical_output_verified = $false
    global_windows_audio_verified = $false
    acoustic_spl_verified = $false
  }
}
