#requires -Version 7.0
<#
Pure, fail-closed numeric proof validator for independent Windows app
WASAPI physical-loopback measurements. This module NEVER installs a driver,
opens a device, changes system volume, or claims acoustic SPL.
#>
Set-StrictMode -Version Latest

function Test-LouderMeIndependentGain {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string]$EvidenceDir
  )
  $m = @{}
  foreach ($boost in @(100, 150)) {
    $path = Join-Path $EvidenceDir "independent-source-result-$boost.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
      throw "Missing actual independent-app evidence for $boost percent boost."
    }
    $record = Get-Content -LiteralPath $path -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    if ($null -eq $record -or $record.status -cne 'PASS' -or
        $record.stimulus -cne 'separate Windows process' -or
        $record.general_third_party_audio_proven -cne $false -or
        $record.acoustic_spl_proven -cne $false) {
      throw "Incorrect or inflated proof scope at $boost percent boost."
    }
    if ($record.boost_percent -is [bool] -or
        $record.boost_percent -isnot [ValueType] -or
        [int]$record.boost_percent -ne $boost) {
      throw "Invalid or swapped recorded boost value at $boost percent."
    }
    foreach ($field in @('physical_matched440_amplitude', 'physical_coherence')) {
      $value = $record.$field
      if ($null -eq $value -or $value -is [bool] -or
          $value -isnot [ValueType]) {
        throw "Non-numeric lab PCM measurement: $field"
      }
      $numeric = [double]$value
      if (-not [double]::IsFinite($numeric)) {
        throw "Nonfinite lab PCM measurement: $field"
      }
    }
    $amp = [double]$record.physical_matched440_amplitude
    $coherence = [double]$record.physical_coherence
    if ($amp -le 0 -or $amp -ge 0.15 -or
        $coherence -lt 0.50 -or $coherence -gt 1.05) {
      throw "Invalid physical 440 Hz amplitude/coherence at $boost percent boost."
    }
    $m[$boost] = $amp
  }
  $ratio = $m[150] / $m[100]
  if (-not [double]::IsFinite($ratio) -or
      $ratio -lt 1.30 -or $ratio -gt 1.70) {
    throw "Independent Windows app physical gain 100 to 150% ratio failed: $ratio"
  }
  return $ratio
}
