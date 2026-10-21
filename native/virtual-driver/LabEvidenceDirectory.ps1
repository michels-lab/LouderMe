#requires -Version 7.0
<#
Pure, device-free evidence ownership helpers for LouderMe's disposable VM lab.
No driver, PnP, audio, admin, or physical endpoint access.
#>
Set-StrictMode -Version Latest

function Write-LouderMeLabTextOnce {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$Path, [AllowEmptyString()][Parameter(Mandatory)][string]$Content)
  $fullPath = [IO.Path]::GetFullPath($Path)
  # CreateNew is atomic: never silently replace stale measurements or a log.
  $stream = [IO.FileStream]::new($fullPath, [IO.FileMode]::CreateNew,
    [IO.FileAccess]::Write, [IO.FileShare]::None)
  try {
    $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
    try { $writer.Write($Content) }
    finally { $writer.Dispose() }
  } finally { $stream.Dispose() }
}

function Write-LouderMeLabJsonOnce {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)]$Record)
  Write-LouderMeLabTextOnce -Path $Path -Content ($Record | ConvertTo-Json -Depth 5)
}

function New-LouderMeLabEvidenceDirectory {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$SessionId)
  $parsedSession = [guid]::Empty
  if (-not [guid]::TryParse($SessionId, [ref]$parsedSession) -or
      $parsedSession -eq [guid]::Empty) {
    throw 'A fresh nonempty LAB session GUID is required before reserving evidence.'
  }
  $fullPath = [IO.Path]::GetFullPath($Path)
  if (Test-Path -LiteralPath $fullPath) {
    throw "Evidence directory or file already exists; choose a NEW, never-reused path: $fullPath"
  }
  $parent = [IO.Path]::GetDirectoryName($fullPath)
  if (-not [IO.Directory]::Exists($parent)) {
    throw "Evidence parent directory must already exist: $parent"
  }
  # The directory check rejects even an EMPTY old folder; the CreateNew
  # marker also prevents simultaneous calls that race to create this path.
  [void][IO.Directory]::CreateDirectory($fullPath)
  Write-LouderMeLabTextOnce -Path (Join-Path $fullPath 'lab-session.lock') -Content ($parsedSession.ToString('D') + [Environment]::NewLine)
  return $fullPath
}

function Protect-LouderMeLabLogLine {
  [CmdletBinding()]
  param([AllowEmptyString()][Parameter(Mandatory)][string]$Line,
        [string[]]$KnownEndpointIds = @(), [switch]$ListDevices)
  $safe = $Line
  if ($ListDevices -and $safe -match '^\\[(?<friendly>.+)\\] (?<endpoint>.+)$') {
    $safe = "[$($Matches.friendly)] [REDACTED_ENDPOINT_ID]"
  }
  foreach ($id in $KnownEndpointIds) {
    if (-not [string]::IsNullOrWhiteSpace($id) -and $id.Length -ge 5) {
      $safe = $safe.Replace($id, '[REDACTED_ENDPOINT_ID]')
    }
  }
  return $safe
}
