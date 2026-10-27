#requires -Version 7.0
# Device-free adversarial fixtures for real no-stimulus VM acceptance.
# Synthetic test data MUST NOT be mistaken for installed driver evidence.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NoSourceControlCheck.ps1')
$goodOut = "EXTERNAL_SOURCE_READY`r`n" +
  "Physical LAB proof: frames=96000 RMS=0.00001 peak=0.00002 " +
  "matched440=0.00001 coherence=0.012 virtualProcessed=0 physicalQueued=0 renderConsumed=0 starvedRenderReads=20 dropped=0`r`n"
$goodErr = 'FAIL: no matching lab tone reached the physical render loopback through the bridge, or virtual source PCM was insufficient.'
$proof = Test-LouderMeNoSourceControl -ExitCode 4 -StdOut $goodOut -StdErr $goodErr
if ($proof.status -ne 'PASS' -or $proof.stimulus_launched -ne $false -or
    $proof.matched440_amplitude -gt .003 -or
    $proof.render_consumed_frames -ne 0 -or $proof.render_starved_reads -ne 20) {
  throw 'Correct no-source physical capture negative control was rejected.'
}
function Assert-Rejected([string]$description, [int]$exit, [string]$out, [string]$err) {
  $rejected = $false
  try { [void](Test-LouderMeNoSourceControl -ExitCode $exit -StdOut $out -StdErr $err) }
  catch { $rejected = $true }
  if (-not $rejected) { throw "False no-stimulus Windows proof accepted: $description" }
}
Assert-Rejected 'source not actually rejected' 0 $goodOut $goodErr
Assert-Rejected 'no real capture readiness' 4 ($goodOut.Replace('EXTERNAL_SOURCE_READY','DEVICE_NOT_READY')) $goodErr
Assert-Rejected 'duplicate readiness from previous process' 4 ("EXTERNAL_SOURCE_READY`n" + $goodOut) $goodErr
Assert-Rejected 'driver failure mislabelled absence of PCM' 4 $goodOut 'FAIL: virtual device not installed'
Assert-Rejected 'unexpected independent PASS' 4 ($goodOut+"PASS: LAB independent source Windows process") $goodErr
Assert-Rejected 'preexisting physical 440 tone contamination' 4 ($goodOut.Replace('matched440=0.00001', 'matched440=0.01800')) $goodErr
Assert-Rejected 'no physical listener frames' 4 ($goodOut.Replace('frames=96000', 'frames=0')) $goodErr
Assert-Rejected 'dropped physical or virtual audio samples' 4 ($goodOut.Replace('dropped=0', 'dropped=4')) $goodErr
Assert-Rejected 'nonfinite coherence' 4 ($goodOut.Replace('coherence=0.012', 'coherence=NaN')) $goodErr
Assert-Rejected 'duplicate unrelated physical measurement' 4 ($goodOut + $goodOut) $goodErr
Assert-Rejected 'missing monitor measurement' 4 'EXTERNAL_SOURCE_READY' $goodErr
Assert-Rejected 'legacy queue-only output metric' 4 ($goodOut.Replace('renderConsumed=0','physicalEmitted=0')) $goodErr
Assert-Rejected 'cannot consume audio that was not enqueued' 4 ($goodOut.Replace('renderConsumed=0','renderConsumed=1')) $goodErr
Assert-Rejected 'missing render underflow evidence' 4 ($goodOut.Replace('starvedRenderReads=20','starvedFake=20')) $goodErr
Assert-Rejected 'unrelated virtual application PCM invalidates silence control' 4 ($goodOut.Replace('virtualProcessed=0','virtualProcessed=4800')) $goodErr
Assert-Rejected 'physical queue contaminated by background PCM' 4 ($goodOut.Replace('physicalQueued=0','physicalQueued=4800')) $goodErr
Write-Host 'PASS: no-stimulus independent route validator rejects 16 adversarial false positives; no hardware used.'
