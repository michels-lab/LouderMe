#requires -Version 7.0
<#
Read-only default playback endpoint identity guard for disposable Windows VMs.
Only checks a SHA-256 fingerprint returned by the native .NET WASAPI bridge.
No driver install, registry/default-device changes, endpoint IDs, or audio PCM.
#>
Set-StrictMode -Version Latest

function Test-LouderMeDefaultRenderFingerprint {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][int]$ExitCode,
    [Parameter(Mandatory)][AllowEmptyString()][string]$Output
  )
  if ($ExitCode -ne 0) {
    throw "Windows default render fingerprint command failed (exit=$ExitCode)."
  }
  $normalized = $Output.Trim()
  $match = [regex]::Match($normalized, '\ADEFAULT_RENDER_SHA256=(?<fingerprint>[a-f0-9]{64})\z')
  if (-not $match.Success) {
    throw 'Default render identity must be one complete lowercase SHA256 record; refuse uncertain audio route.'
  }
  return $match.Groups['fingerprint'].Value
}

function Get-LouderMeDefaultRenderFingerprint {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$BridgeDll)
  if (-not (Test-Path -LiteralPath $BridgeDll -PathType Leaf)) {
    throw 'The read-only WASAPI bridge is not present; refusing driver installation.'
  }
  $lines = @(& dotnet $BridgeDll --default-render-fingerprint 2>&1)
  $exitCode = $LASTEXITCODE
  return Test-LouderMeDefaultRenderFingerprint -ExitCode $exitCode -Output ($lines -join "`n")
}

function Assert-LouderMeDefaultRenderUnchanged {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string]$Expected,
    [Parameter(Mandatory)][string]$Actual,
    [Parameter(Mandatory)][string]$Phase
  )
  if ($Expected -cnotmatch '\A[a-f0-9]{64}\z' -or
      $Actual -cnotmatch '\A[a-f0-9]{64}\z' -or
      $Expected -cne $Actual) {
    throw "Windows default render changed or became unobservable during $Phase. Stop LAB audio, remove only the owned driver, and restore the VM snapshot."
  }
}
