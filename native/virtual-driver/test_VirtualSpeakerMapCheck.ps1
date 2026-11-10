#requires -Version 7.0
# Actual PowerShell parser tests, no signed driver or WASAPI device.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'VirtualSpeakerMapCheck.ps1')
$source = 'EXTERNAL_APP_SPEAKER_MAP_FINISHED frames=96000 channels=6; virtual speaker mapping is verified only by the separate bridge.'
$receiver = "EXTERNAL_VIRTUAL_READY" + [Environment]::NewLine +
  "VIRTUAL_SPEAKER_MAP_PROOF frames=225000 channels=6 minOwn=0.010000 maxLeakage=0.000300" + [Environment]::NewLine +
  "PASS: independent Windows process -> LouderMe virtual WASAPI positional speaker map. No physical output or DSP was opened."
foreach ($n in @(2, 6, 8)) {
  $a = Test-LouderMeSpeakerMapSource -ExitCode 0 -StdOut ($source.Replace('channels=6',"channels=$n")) -StdErr ''
  $b = Test-LouderMeSpeakerMapReceiver -ExitCode 0 -StdOut ($receiver.Replace('channels=6',"channels=$n")) -StdErr '' -ExpectedChannels $n -SourceFrames $a.frames
  if ($a.channels -ne $n -or $b.channels -ne $n -or $b.min_own_amplitude -ne 0.010) {
    throw "Valid channel map rejected: $n"
  }
}
function Assert-BadSource([string]$label, [int]$code, [string]$output, [string]$err) {
  $failed = $false
  try { [void](Test-LouderMeSpeakerMapSource -ExitCode $code -StdOut $output -StdErr $err) }
  catch { $failed = $true }
  if (-not $failed) { throw "False source marker accepted: $label" }
}
function Assert-BadReceiver([string]$label, [int]$code, [string]$output,
    [string]$err, [int]$channels = 6, [long]$frames = 96000) {
  $failed = $false
  try { [void](Test-LouderMeSpeakerMapReceiver -ExitCode $code -StdOut $output -StdErr $err -ExpectedChannels $channels -SourceFrames $frames) }
  catch { $failed = $true }
  if (-not $failed) { throw "False speaker-map measurement accepted: $label" }
}
foreach ($n in @(0, 1, 3, 4, 5, 7, 9)) {
  Assert-BadSource "invalid source channels $n" 0 ($source.Replace('channels=6',"channels=$n")) ''
}
Assert-BadSource 'crashed' 4 $source ''
Assert-BadSource 'stderr' 0 $source 'SOURCE_ERROR'
Assert-BadSource 'wrong mode' 0 ($source.Replace('SPEAKER_MAP_FINISHED','TONE_FINISHED')) ''
Assert-BadSource 'duplicate completion' 0 ($source + [Environment]::NewLine + $source) ''
Assert-BadSource 'short PCM' 0 ($source.Replace('frames=96000','frames=1')) ''
Assert-BadSource 'odd PCM' 0 ($source.Replace('frames=96000','frames=96001')) ''
Assert-BadReceiver 'exit failure' 3 $receiver ''
Assert-BadReceiver 'stderr failure' 0 $receiver 'CAPTURE_ERROR'
Assert-BadReceiver 'no readiness' 0 ($receiver.Replace('EXTERNAL_VIRTUAL_READY','UNREADY')) ''
Assert-BadReceiver 'no PASS' 0 ($receiver.Replace('PASS: independent','WARN: independent')) ''
Assert-BadReceiver 'bad channel mapping' 0 ($receiver.Replace('channels=6','channels=8')) ''
Assert-BadReceiver 'no frames' 0 ($receiver.Replace('frames=225000','frames=0')) ''
Assert-BadReceiver 'too few frames' 0 ($receiver.Replace('frames=225000','frames=1000')) ''
Assert-BadReceiver 'missing speaker pilot' 0 ($receiver.Replace('minOwn=0.010000','minOwn=0.001000')) ''
Assert-BadReceiver 'speaker swap or full crossmix' 0 ($receiver.Replace('maxLeakage=0.000300','maxLeakage=0.007000')) ''
Assert-BadReceiver 'impossible amplitude' 0 ($receiver.Replace('minOwn=0.010000','minOwn=1.000000')) ''
Assert-BadReceiver 'nonfinite amplitude' 0 ($receiver.Replace('minOwn=0.010000','minOwn=NaN')) ''
Assert-BadReceiver 'duplicate numeric proof' 0 ($receiver + [Environment]::NewLine + 'VIRTUAL_SPEAKER_MAP_PROOF frames=225000 channels=6 minOwn=0.010000 maxLeakage=0.000300') ''
Write-Host 'PASS: independent speaker-map parser accepts 2/5.1/7.1 and rejects invalid markers, swaps, leaks, and false numeric evidence.'
