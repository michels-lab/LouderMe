#requires -Version 7.0
# Device-free synthetic pwsh processes only: no physical audio or PnP calls.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LabProcessGuard.ps1')

$pwshPath = (Get-Command pwsh -ErrorAction Stop).Source
$root = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-LabProcess-$([guid]::NewGuid().ToString('N'))"
[void][IO.Directory]::CreateDirectory($root)
function New-PwshArguments([string]$expression) {
  $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($expression))
  return @('-NoProfile', '-EncodedCommand', $encoded)
}
function Assert-Fails([string]$scenario, [scriptblock]$action) {
  $rejected = $false
  try { & $action | Out-Null } catch { $rejected = $true }
  if (-not $rejected) { throw "Unsafe LAB child accepted: $scenario" }
}
try {
  # Concurrently fill BOTH OS pipe buffers beyond nominal pipe sizes.
  $flood = '[Console]::Out.Write("O" * 70000); [Console]::Error.Write("E" * 70000)'
  $result = Invoke-LouderMeLabFiniteProcess -Executable $pwshPath -Arguments (New-PwshArguments $flood) -TimeoutMs 15000 -MaximumCharacters 131072
  if ($result.ExitCode -ne 0 -or $result.StdOut.Length -ne 70000 -or $result.StdErr.Length -ne 70000) {
    throw 'Simultaneous stdout and stderr streams were not both drained.'
  }
  Assert-Fails 'reject overlong output' {
    Invoke-LouderMeLabFiniteProcess -Executable $pwshPath -Arguments (New-PwshArguments $flood) -TimeoutMs 15000 -MaximumCharacters 65536
  }
  $failed = Invoke-LouderMeLabFiniteProcess -Executable $pwshPath -Arguments (New-PwshArguments '[Console]::Out.Write("bad status"); exit 23') -TimeoutMs 15000
  if ($failed.ExitCode -ne 23 -or $failed.StdOut -ne 'bad status') { throw 'Nonzero exit code was hidden.' }

  # Readiness must be visible despite a full stderr pipe before the marker.
  $child = $null
  try {
    $child = Start-LouderMeLabChild -Executable $pwshPath -DeferStdOutDrain -Arguments (New-PwshArguments '[Console]::Error.Write("N" * 70000); [Console]::Out.WriteLine("EXTERNAL_SOURCE_READY"); [Console]::Out.Write("tail")')
    $ready = $child.Process.StandardOutput.ReadLineAsync()
    if (-not $ready.Wait(15000) -or $ready.Result -ne 'EXTERNAL_SOURCE_READY') { throw 'A blocked stderr suppressed real bridge readiness.' }
    $child.StdOutTask = $child.Process.StandardOutput.ReadToEndAsync()
    $record = Receive-LouderMeLabChildOutput -Child $child -TimeoutMs 15000 -MaximumCharacters 131072
    if ($record.ExitCode -ne 0 -or $record.StdOut -ne 'tail' -or $record.StdErr.Length -ne 70000) { throw 'Readiness/tail output differed.' }
  } finally {
    if ($null -ne $child) { Stop-LouderMeLabChild -Child $child }
  }

  # Timeout must kill only this worker, not wait for workflow cancellation.
  $pidFile = Join-Path $root 'timed-out-child.pid'
  $expression = '[IO.File]::WriteAllText(' + "'$($pidFile.Replace("'","''"))'" + ', $PID.ToString()); Start-Sleep -Seconds 20'
  $started = [Diagnostics.Stopwatch]::StartNew()
  Assert-Fails 'local 1.2s deadline' {
    Invoke-LouderMeLabFiniteProcess -Executable $pwshPath -Arguments (New-PwshArguments $expression) -TimeoutMs 1200
  }
  if ($started.Elapsed.TotalSeconds -gt 10 -or -not (Test-Path -LiteralPath $pidFile)) { throw 'Local bounded timeout not enforced.' }
  $deadPid = [int](Get-Content -LiteralPath $pidFile -Raw)
  if (Get-Process -Id $deadPid -ErrorAction SilentlyContinue) { throw 'Timed-out child remains after owned tree cleanup.' }

  # Ensure the REAL VM runner uses the same tested code.
  $runner = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'run_disposable_lab.ps1') -Raw
  $independent = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'probe_independent_app_route.ps1') -Raw
  foreach ($marker in @("Invoke-LouderMeLabFiniteProcess -Executable 'dotnet'", 'Invoke-LouderMeLabFiniteProcess -Executable $DevconExe', "Invoke-LouderMeLabFiniteProcess -Executable 'pnputil.exe'")) {
    if (-not $runner.Contains($marker)) { throw "Real driver VM runner has unbounded subprocess: $marker" }
  }
  foreach ($marker in @("Start-LouderMeLabChild -Executable 'dotnet' -DeferStdOutDrain", '$bridge.StdOutTask = $bridge.Process.StandardOutput.ReadToEndAsync()', 'Receive-LouderMeLabChildOutput -Child $source', 'Receive-LouderMeLabChildOutput -Child $bridge', 'Stop-LouderMeLabChild -Child $child')) {
    if (-not $independent.Contains($marker)) { throw "Independent app lacks deadline/pipe handling: $marker" }
  }
  if ($runner.Contains('& dotnet $script:BridgeDll') -or $independent.Contains('Make-Process') -or $independent.Contains('StandardError.ReadToEnd()')) {
    throw 'Unbounded pipe process mode returned to physical lab.'
  }
  Write-Host 'PASS: dual 70KiB pipes, unique readiness, capped output, nonzero exit, child timeout and cleanup without audio devices.'
} finally {
  Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
