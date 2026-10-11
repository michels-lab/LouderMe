param(
    [Parameter(Mandatory=$true)][string]$ExePath,
    [Parameter(Mandatory=$true)][string]$SourceSha,
    [Parameter(Mandatory=$true)][string]$OutDir
)
$ErrorActionPreference = "Stop"
$exe = (Resolve-Path $ExePath).Path
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$dir = (Resolve-Path $OutDir).Path
$p = Start-Process -FilePath $exe -ArgumentList @('--visual-evidence', ('"' + $dir + '"')) -PassThru -Wait
if ($p.ExitCode -ne 0) {
    $crashLog = Join-Path $env:LOCALAPPDATA "Michel's Lab\LouderMe\startup-crash.log"
    if (Test-Path $crashLog) { Get-Content $crashLog }
    throw "Live Windows visual evidence mode failed with exit $($p.ExitCode)"
}
python tools/build_windows_visual_manifest.py --exe $exe --source-sha $SourceSha --output $dir
if ($LASTEXITCODE -ne 0) { throw "Windows screenshot manifest rejected" }
Write-Host "LouderMe Windows built runtime Home/About evidence validated; human review remains post-release."
