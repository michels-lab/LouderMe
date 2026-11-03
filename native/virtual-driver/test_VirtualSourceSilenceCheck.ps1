#requires -Version 7.0
# No devices or drivers: malformed/fabricated virtual-only silence evidence
# must be rejected before a physical-DSP A/B can be accepted.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'VirtualSourceSilenceCheck.ps1')
$ready = 'EXTERNAL_VIRTUAL_READY'
$failText = 'FAIL: no matching 440 Hz tone in the virtual WASAPI loopback. Unrelated energy, silence and synthetic driver tones do not qualify.'
$valid = "EXTERNAL_VIRTUAL_READY`nVirtual PCM proof: frames=0 RMS=0.00000 peak=0.00000 matched440=0.00000 coherence=0.000`nVIRTUAL_EXTERNAL_PROOF frames=0 matched440=0.000000 coherence=0.0000"
function Assert-Rejected([string]$name, [int]$code, [string]$stdout, [string]$stderr) {
  $failed = $false
  try {
    [void](Test-LouderMeVirtualNoSource -ExitCode $code -StdOut $stdout -StdErr $stderr -Phase before)
  } catch { $failed = $true }
  if (-not $failed) { throw "Incorrectly accepted virtual loopback negative control: $name" }
}
$blank = Test-LouderMeVirtualNoSource -ExitCode 3 -StdOut $valid -StdErr $failText -Phase before
if ($blank.control_phase -cne 'before' -or
    [double]$blank.virtual_matched440_amplitude -ne 0 -or
    $blank.stimulus_launched -cne $false) {
  throw 'Correct idle virtual listener was rejected.'
}
$quiet = $valid.Replace('frames=0', 'frames=240000').Replace(
  'matched440=0.000000', 'matched440=0.002999').Replace(
  'coherence=0.0000', 'coherence=0.0300')
$after = Test-LouderMeVirtualNoSource -ExitCode 3 -StdOut $quiet -StdErr $failText -Phase after
if ($after.control_phase -cne 'after' -or
    $after.virtual_monitor_frames -ne 240000) {
  throw 'Correct nonzero, quiet virtual loopback was rejected.'
}
Assert-Rejected 'unexpected successful source' 0 $valid $failText
Assert-Rejected 'bridge crash' 1 $valid $failText
Assert-Rejected 'no readiness' 3 ($valid.Replace($ready, 'NOT_READY')) $failText
Assert-Rejected 'duplicate readiness' 3 ($ready + "`n" + $valid) $failText
Assert-Rejected 'no proof' 3 ($valid -replace '(?m)^VIRTUAL_EXTERNAL_PROOF.*$', '') $failText
Assert-Rejected 'duplicated proof' 3 ($valid + "`n" + 'VIRTUAL_EXTERNAL_PROOF frames=0 matched440=0.000000 coherence=0.0000') $failText
Assert-Rejected 'unexpected tone accepted' 3 ($valid + "`nPASS: independent Windows process -> LouderMe virtual WASAPI loopback 440 Hz identity. No physical output or DSP was opened.") $failText
Assert-Rejected 'independent app ran in no-source control' 3 ($valid + "`nEXTERNAL_APP_TONE_FINISHED") $failText
Assert-Rejected 'missing failure marker' 3 $valid ''
Assert-Rejected 'wrong failure origin' 3 $valid 'Could not initialize loopback capture'
Assert-Rejected 'additional exception on stderr' 3 $valid ($failText + "`nException from audio client")
Assert-Rejected 'lingering 440 Hz above silence threshold' 3 ($valid.Replace('matched440=0.000000', 'matched440=0.004000')) $failText
Assert-Rejected '440 Hz active source' 3 ($valid.Replace('matched440=0.000000', 'matched440=0.024000')) $failText
Assert-Rejected 'coherence > 1' 3 ($valid.Replace('coherence=0.0000', 'coherence=1.1000')) $failText
Assert-Rejected 'negative sign in amplitude' 3 ($valid.Replace('matched440=0.000000', 'matched440=-0.001000')) $failText
Assert-Rejected 'nonfinite amplitude' 3 ($valid.Replace('matched440=0.000000', 'matched440=NaN')) $failText
Assert-Rejected 'implausible sample count' 3 ($valid.Replace('frames=0', 'frames=10000001')) $failText
Write-Host 'PASS: virtual no-source negative controls reject false tone, missing capture, duplicate evidence, invalid identity and crashes (device-free).'
