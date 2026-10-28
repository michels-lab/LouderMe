#requires -Version 7.0
<#
Bounded, device-free child-process lifecycle for owner-approved VM audio lab.
Redirect stdout and stderr CONCURRENTLY before waiting: otherwise a large
stderr stream can deadlock a bridge or independent tone subprocess. Kill only
the PID/tree created by this invocation. Never print raw endpoint IDs.
#>
Set-StrictMode -Version Latest

function Start-LouderMeLabChild {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Executable,
    [Parameter(Mandatory)][string[]]$Arguments,
    [switch]$DeferStdOutDrain
  )
  $start = [Diagnostics.ProcessStartInfo]::new($Executable)
  foreach ($argument in $Arguments) { [void]$start.ArgumentList.Add($argument) }
  $start.UseShellExecute = $false
  $start.CreateNoWindow = $true
  $start.RedirectStandardOutput = $true
  $start.RedirectStandardError = $true
  $child = [Diagnostics.Process]::new()
  $child.StartInfo = $start
  try {
    if (-not $child.Start()) { throw 'LAB child process could not start.' }
    # Read stderr from the FIRST instruction after Start; stdout can only
    # be deferred by the independent-tone readiness state machine.
    $errTask = $child.StandardError.ReadToEndAsync()
    $outTask = if ($DeferStdOutDrain) { $null } else {
      $child.StandardOutput.ReadToEndAsync()
    }
    return [pscustomobject]@{
      Process = $child
      StdOutTask = $outTask
      StdErrTask = $errTask
    }
  } catch {
    if (-not $child.HasExited) {
      try { $child.Kill($true); [void]$child.WaitForExit(3000) } catch {}
    }
    $child.Dispose()
    throw 'Could not start bounded isolated lab subprocess.'
  }
}

function Stop-LouderMeLabChild {
  [CmdletBinding()]
  param([Parameter(Mandatory)]$Child)
  if ($null -eq $Child) { return }
  $process = $Child.Process
  try {
    if (-not $process.HasExited) {
      $process.Kill($true)
      if (-not $process.WaitForExit(3000)) {
        throw 'Owned LAB child process could not be terminated safely.'
      }
    }
  } finally {
    $process.Dispose()
  }
}

function Receive-LouderMeLabChildOutput {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]$Child,
    [Parameter(Mandatory)][ValidateRange(1000, 60000)][int]$TimeoutMs,
    [ValidateRange(128, 262144)][int]$MaximumCharacters = 65536
  )
  $process = $Child.Process
  if ($null -eq $Child.StdOutTask -or $null -eq $Child.StdErrTask) {
    throw 'Both LAB child output streams must already be draining.'
  }
  if (-not $process.WaitForExit($TimeoutMs)) {
    throw 'LAB child exceeded bounded execution time. Owned process will be terminated.'
  }
  if (-not $Child.StdOutTask.Wait(2000) -or -not $Child.StdErrTask.Wait(2000)) {
    throw 'LAB child output pipe was not drained after process termination.'
  }
  $stdout = [string]$Child.StdOutTask.GetAwaiter().GetResult()
  $stderr = [string]$Child.StdErrTask.GetAwaiter().GetResult()
  if ($stdout.Length -gt $MaximumCharacters -or
      $stderr.Length -gt $MaximumCharacters) {
    throw 'LAB process output exceeded local bounded evidence limit.'
  }
  return [pscustomobject]@{
    ExitCode = [int]$process.ExitCode
    StdOut = $stdout
    StdErr = $stderr
  }
}

function Invoke-LouderMeLabFiniteProcess {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Executable,
    [Parameter(Mandatory)][string[]]$Arguments,
    [Parameter(Mandatory)][ValidateRange(1000,60000)][int]$TimeoutMs,
    [ValidateRange(128,262144)][int]$MaximumCharacters = 65536
  )
  $child = $null
  try {
    $child = Start-LouderMeLabChild -Executable $Executable -Arguments $Arguments
    return Receive-LouderMeLabChildOutput -Child $child -TimeoutMs $TimeoutMs -MaximumCharacters $MaximumCharacters
  } finally {
    if ($null -ne $child) { Stop-LouderMeLabChild -Child $child }
  }
}
