#requires -Version 7.0
<#
Read-only trust-boundary helper for LouderME's disposable-VM physical audio lab.
Hashes the exact approved INF/CAT/SYS + managed bridge + C++ DSP + external
tone app + WDK devcon before ANY PnP operation; rejects content changes
between testing phases and after driver removal. Does not certify signer
identity or acoustics and never reads PCM or signing private keys.
#>
Set-StrictMode -Version Latest

function New-LouderMeLabInputSnapshot {
  [CmdletBinding()]
  param([Parameter(Mandatory)][System.Collections.IDictionary]$Files)

  if ($Files.Count -ne 7) {
    throw 'LAB needs exactly seven preapproved executable/package inputs.'
  }
  $required = @('INF','CAT','SYS','BRIDGE','NATIVE_DSP','INDEPENDENT_TONE','DEVCON')
  $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $snap = [ordered]@{}
  foreach ($label in $required) {
    if (-not $Files.Contains($label)) {
      throw "Mandatory LAB input $label is missing; refuse driver installation."
    }
    $value = [string]$Files[$label]
    if ([string]::IsNullOrWhiteSpace($value)) {
      throw "LAB input $label has no configured path."
    }
    $full = [IO.Path]::GetFullPath($value)
    if (-not $seen.Add($full)) {
      throw 'LAB executable or driver package inputs alias the same file.'
    }
    if (-not [IO.File]::Exists($full)) {
      throw "LAB input $label is absent; refuse privileged operation."
    }
    $entry = [IO.FileInfo]::new($full)
    $entry.Refresh()
    if ($entry.Length -lt 1 -or
        ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
      throw "LAB input $label is empty or a filesystem reparse point."
    }
    # Reject junction/symlink parents too. The path must remain a direct
    # local file, never an indirectly retargetable input during a VM session.
    $parent = $entry.Directory
    while ($null -ne $parent) {
      if (($parent.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "LAB input $label has a mutable junction/symlink parent."
      }
      $parent = $parent.Parent
    }
    $digest = (Get-FileHash -LiteralPath $full -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    if ($digest -cnotmatch '\A[a-f0-9]{64}\z') {
      throw "LAB input $label lacks a valid SHA256 digest."
    }
    $snap[$label] = [pscustomobject]@{
      Path = $full
      Bytes = [long]$entry.Length
      SHA256 = $digest
    }
  }
  if ($Files.Count -ne $snap.Count) {
    throw 'Unrecognized extra lab input; identity set must be explicit.'
  }
  return $snap
}

function Assert-LouderMeLabInputsUnchanged {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][System.Collections.IDictionary]$Snapshot,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Phase
  )
  if ($Snapshot.Count -ne 7) {
    throw 'LAB input snapshot is incomplete.'
  }
  foreach ($label in @('INF','CAT','SYS','BRIDGE','NATIVE_DSP','INDEPENDENT_TONE','DEVCON')) {
    if (-not $Snapshot.Contains($label)) {
      throw "LAB mandatory input $label is not in the baseline."
    }
    $entry = $Snapshot[$label]
    if (-not [IO.File]::Exists($entry.Path)) {
      throw "LAB input $label disappeared during $Phase. Abort and revert VM snapshot."
    }
    $current = [IO.FileInfo]::new($entry.Path)
    $current.Refresh()
    if ($current.Length -ne $entry.Bytes -or
        ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
      throw "LAB input $label was replaced or changed size during $Phase. Stop route."
    }
    $parent = $current.Directory
    while ($null -ne $parent) {
      if (($parent.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "LAB input $label path was redirected during $Phase. Stop route."
      }
      $parent = $parent.Parent
    }
    $sha = (Get-FileHash -LiteralPath $entry.Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    if ($sha -cne $entry.SHA256) {
      throw "LAB input $label bytes changed during $Phase. Stop route and restore VM."
    }
  }
}

function Get-LouderMeLabInputMetadata {
  [CmdletBinding()]
  param([Parameter(Mandatory)][System.Collections.IDictionary]$Snapshot)
  # Publish SHA256 only. Never write absolute executable paths, private audio
  # endpoint IDs or the bytes of driver packages into shared CI artifacts.
  $lines = @()
  foreach ($label in @('INF','CAT','SYS','BRIDGE','NATIVE_DSP','INDEPENDENT_TONE','DEVCON')) {
    $sha = $Snapshot[$label].SHA256
    if ($sha -cnotmatch '\A[a-f0-9]{64}\z') {
      throw 'Invalid LAB input fingerprint for metadata.'
    }
    $lines += "INPUT_$($label)_SHA256=$sha"
  }
  return $lines
}
