#requires -Version 7.0
# Pure adversarial fixtures. No driver, Windows sound endpoint, or PCM device
# is installed or exercised by this test.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PositiveSourceCheck.ps1')

$goodOut = "EXTERNAL_SOURCE_READY`r`n" +
  "Physical LAB proof: frames=192000 RMS=0.01200 peak=0.03400 " +
  "matched440=0.02000 coherence=0.810 virtualProcessed=96000 physicalEmitted=96000 dropped=0`r`n" +
  "PASS: LAB independent source Windows process -> selected virtual endpoint -> own C++ DSP -> " +
  "separate physical WASAPI loopback 440 Hz identity. " +
  "NOT general third-party/default-app audio proof or acoustic loudness.`r`n"
$goodSource = "EXTERNAL_APP_TONE_FINISHED 440Hz frames=96000 channels=2; " +
  "physical loopback is verified only by the separate bridge.`r`n"

$passed = Test-LouderMePositiveSourceRoute -ExitCode 0 -StdOut $goodOut -StdErr '' `
  -SourceExitCode 0 -SourceStdOut $goodSource -SourceStdErr ''
if ($passed.physical_matched440_amplitude -ne 0.020 -or
    $passed.physical_coherence -ne 0.810 -or
    $passed.physical_monitor_frames -ne 192000 -or
    $passed.independent_source_frames -ne 96000 -or
    $passed.dropped_frames -ne 0) {
  throw 'Valid independent app + physical measurement was rejected or misparsed.'
}

$script:rejected = 0
function Assert-Rejected {
  param(
    [Parameter(Mandatory)][string]$Reason,
    [int]$BridgeExit = 0,
    [int]$ToneExit = 0,
    [string]$BridgeOutput = $goodOut,
    [string]$BridgeError = '',
    [string]$ToneOutput = $goodSource,
    [string]$ToneError = ''
  )
  $isRejected = $false
  try {
    [void](Test-LouderMePositiveSourceRoute -ExitCode $BridgeExit `
      -StdOut $BridgeOutput -StdErr $BridgeError `
      -SourceExitCode $ToneExit -SourceStdOut $ToneOutput -SourceStdErr $ToneError)
  } catch { $isRejected = $true }
  if (-not $isRejected) { throw "False positive Windows source route proof accepted: $Reason" }
  $script:rejected++
}
Assert-Rejected -Reason 'bridge nonzero exit' -BridgeExit 4
Assert-Rejected -Reason 'source nonzero exit' -ToneExit 1
Assert-Rejected -Reason 'source explicit error' -ToneError 'EXTERNAL_APP_TONE_FAILED endpoint'
Assert-Rejected -Reason 'bridge explicit FAIL despite success' -BridgeError 'FAIL: no matching lab tone reached the physical render loopback'
Assert-Rejected -Reason 'bridge never ready' -BridgeOutput ($goodOut.Replace('EXTERNAL_SOURCE_READY','DEVICE_READY'))
Assert-Rejected -Reason 'stale duplicate bridge ready' -BridgeOutput ("EXTERNAL_SOURCE_READY`n" + $goodOut)
Assert-Rejected -Reason 'no finished independent process' -ToneOutput ''
Assert-Rejected -Reason 'duplicate finished process' -ToneOutput ($goodSource + $goodSource)
Assert-Rejected -Reason 'incorrect independent frequency' -ToneOutput ($goodSource.Replace('440Hz','500Hz'))
Assert-Rejected -Reason 'too little independent PCM' -ToneOutput ($goodSource.Replace('frames=96000','frames=1'))
Assert-Rejected -Reason 'no independent channels' -ToneOutput ($goodSource.Replace('channels=2','channels=0'))
Assert-Rejected -Reason 'no real bridge PASS' -BridgeOutput ($goodOut.Replace('PASS: LAB independent source','INFO: LAB independent source'))
Assert-Rejected -Reason 'duplicate pasted PASS' -BridgeOutput ($goodOut + ($goodOut -split 'PASS: LAB independent source')[1].Insert(0,'PASS: LAB independent source'))
Assert-Rejected -Reason 'inflated success claim' -BridgeOutput ($goodOut.Replace('NOT general third-party/default-app audio proof or acoustic loudness.','General third-party audio proven.'))
Assert-Rejected -Reason 'no physical report' -BridgeOutput ($goodOut.Replace('Physical LAB proof:','Physical mock:'))
Assert-Rejected -Reason 'duplicate contradictory physical record' -BridgeOutput ($goodOut + ($goodOut -split 'Physical LAB proof:')[1].Insert(0,'Physical LAB proof:'))
Assert-Rejected -Reason 'fake incomplete frames only' -BridgeOutput ($goodOut.Replace('virtualProcessed=96000','virtualFake=96000'))
Assert-Rejected -Reason 'zero virtual PCM' -BridgeOutput ($goodOut.Replace('virtualProcessed=96000','virtualProcessed=0'))
Assert-Rejected -Reason 'zero physical output' -BridgeOutput ($goodOut.Replace('physicalEmitted=96000','physicalEmitted=0'))
Assert-Rejected -Reason 'no physical monitor' -BridgeOutput ($goodOut.Replace('frames=192000','frames=0'))
Assert-Rejected -Reason 'dropped PCM' -BridgeOutput ($goodOut.Replace('dropped=0','dropped=9'))
Assert-Rejected -Reason 'silent physical output' -BridgeOutput ($goodOut.Replace('matched440=0.02000','matched440=0.00000'))
Assert-Rejected -Reason 'incoherent frequency' -BridgeOutput ($goodOut.Replace('coherence=0.810','coherence=0.100'))
Assert-Rejected -Reason 'nonfinite physical amplitude' -BridgeOutput ($goodOut.Replace('matched440=0.02000','matched440=NaN'))
Assert-Rejected -Reason 'RMS higher than peak' -BridgeOutput ($goodOut.Replace('peak=0.03400','peak=0.00100'))
Assert-Rejected -Reason 'clipped peak' -BridgeOutput ($goodOut.Replace('peak=0.03400','peak=1.10000'))
Assert-Rejected -Reason 'silence despite PASS' -BridgeOutput ($goodOut.Replace('RMS=0.01200','RMS=0.00000'))
Write-Host "PASS: valid source+physical evidence and $script:rejected adversarial false positives rejected (no hardware)."
