#requires -Version 7.0
# Actual PowerShell regression, no audio device or signed lab driver.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'VirtualSourceCompletionCheck.ps1')
$base = 'EXTERNAL_APP_TONE_FINISHED 440Hz frames=96000 channels=2; physical loopback is verified only by the separate bridge.'
foreach ($channels in @(1,2,6,8)) {
  $v = Test-LouderMeVirtualSourceCompletion -ExitCode 0 -StdErr '' -StdOut ($base.Replace('channels=2', "channels=$channels"))
  if ($v.channels -ne $channels -or $v.frames -ne 96000) {
    throw "Valid independent source channels rejected: $channels"
  }
}
function Assert-Rejected([string]$name, [int]$code, [string]$out, [string]$err) {
  $rejected = $false
  try {
    [void](Test-LouderMeVirtualSourceCompletion -ExitCode $code -StdOut $out -StdErr $err)
  } catch { $rejected = $true }
  if (-not $rejected) { throw "False source completion accepted: $name" }
}
foreach ($channels in @(0,3,4,5,7,9,10)) {
  Assert-Rejected "ambiguous channels $channels" 0 ($base.Replace('channels=2',"channels=$channels")) ''
}
Assert-Rejected 'source crashed' 4 $base ''
Assert-Rejected 'source stderr' 0 $base 'EXTERNAL_APP_TONE_FAILED'
Assert-Rejected 'wrong pitch' 0 ($base.Replace('440Hz','880Hz')) ''
Assert-Rejected 'missing marker' 0 ($base.Replace('EXTERNAL_APP_TONE_FINISHED','APP_TONE_FINISHED')) ''
Assert-Rejected 'double completed' 0 ($base + [Environment]::NewLine + $base) ''
Assert-Rejected 'invalid second completion' 0 ($base + [Environment]::NewLine + $base.Replace('channels=2','channels=3')) ''
Assert-Rejected 'fake physical pass' 0 ($base.Replace('physical loopback is verified only by the separate bridge.','PHYSICAL AUDIO PASSED')) ''
Assert-Rejected 'missing disclaimer' 0 ($base.Replace('; physical loopback is verified only by the separate bridge.','')) ''
Assert-Rejected 'zero frames' 0 ($base.Replace('frames=96000','frames=0')) ''
Assert-Rejected 'odd frames' 0 ($base.Replace('frames=96000','frames=96001')) ''
Assert-Rejected 'insufficient frames' 0 ($base.Replace('frames=96000','frames=15998')) ''
Assert-Rejected 'overlong frames' 0 ($base.Replace('frames=96000','frames=384002')) ''
Write-Host 'PASS: Windows source 1/2/6/8-channel markers, frame bounds and adversarial cases (no devices).'
