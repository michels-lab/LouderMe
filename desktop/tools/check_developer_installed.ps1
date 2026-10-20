param(
    [Parameter(Mandatory=$true)]
    [string]$ExePath
)

$ErrorActionPreference = 'Stop'
$installedExe = [IO.Path]::GetFullPath($ExePath)
if (-not (Test-Path -LiteralPath $installedExe -PathType Leaf)) {
    throw "Installed LouderMe executable not found."
}

# Inspect the real Inno-created Start Menu shortcut, not an authored .iss string.
# CurrentUser and AllUsers installations can use different Programs directories.
$directories = @(
    [Environment]::GetFolderPath('Programs'),
    [Environment]::GetFolderPath('CommonPrograms')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_) } | Select-Object -Unique

$com = New-Object -ComObject WScript.Shell
$found = $false
foreach ($dir in $directories) {
    foreach ($shortcut in (Get-ChildItem -Path $dir -Filter 'LouderMe Developer Edition.lnk' -Recurse -File -ErrorAction SilentlyContinue)) {
        $link = $com.CreateShortcut($shortcut.FullName)
        if ($link.Arguments.Trim() -ne '--developer') { continue }
        if ([IO.Path]::GetFullPath($link.TargetPath) -ne $installedExe) { continue }
        $found = $true
        break
    }
    if ($found) { break }
}
if (-not $found) {
    throw 'Real installed Developer Edition shortcut missing, wrong launch mode or points to a different EXE.'
}

# Run the native WinForms audit via the physically installed EXE. It must exit
# cleanly after checking real controls and must NEVER edit public user settings.
$process = Start-Process -FilePath $installedExe -ArgumentList '--developer-smoke-test' -PassThru -Wait
if ($process.ExitCode -ne 0) {
    throw "Installed Developer WinForms audit smoke failed: exit code $($process.ExitCode)."
}
Write-Host 'PASS: installed independent Developer shortcut and live auditor.'
