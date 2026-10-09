#requires -Version 7.0
<#
.SYNOPSIS
  Build and validate an UNSIGNED laboratory INF/CAT package from the pinned
  LouderME SysVAD-derived source. Never install or sign anything.
.DESCRIPTION
  Requires a fully installed MATCHED Windows SDK + WDK. Aborts on any failure.
#>
param(
  [Parameter(Mandatory)][string]$SysVadRoot,
  [Parameter(Mandatory)][string]$OutDir
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Require-Tool([string]$Name) {
  $kitRoot = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Windows Kits\10'
  if (-not (Test-Path $kitRoot)) {
    throw "Full WDK/SDK required; Windows Kits bin directory missing: $kitRoot"
  }
  $tool = Get-ChildItem $kitRoot -Recurse -File -Filter "$Name.exe" |
    Where-Object { $_.FullName -match '\\(x64|x86)\\' } |
    Sort-Object -Property @{Expression={ if ($_.FullName -match '\\x64\\') { 1 } else { 0 } };Descending=$true},FullName -Descending |
    Select-Object -First 1
  if ($null -eq $tool) {
    throw "$Name.exe missing. Install a matching full Windows SDK and WDK before packaging."
  }
  Write-Host "WDK tool selected: $($tool.FullName)"
  return $tool.FullName
}

$sysvad = (Resolve-Path $SysVadRoot).Path
$sample = Join-Path $sysvad 'TabletAudioSample'
$manifest = Join-Path $sysvad 'LOUDERME_LAB_MANIFEST.json'
if (-not (Test-Path $manifest)) { throw 'Expected pinned LouderME lab derivation manifest.' }
$meta = Get-Content $manifest -Raw | ConvertFrom-Json
if (-not $meta.lab_only -or $meta.render_endpoints -ne 1 -or
    $meta.capture_endpoints -ne 0 -or $meta.recording_to_file_enabled -or
    $meta.sample_tone_generator_enabled -or $meta.driver_signed -or
    $meta.driver_installed -or $meta.real_audio_verified) {
  throw 'Lab manifest does not match approved driver/source privacy constraints.'
}

$sys = Get-ChildItem $sample -Recurse -File -Filter 'TabletAudioSample.sys' |
  Where-Object { $_.FullName -match 'x64.*Release|Release.*x64' } |
  Select-Object -First 1
if ($null -eq $sys -or $sys.Length -lt 10000) {
  throw 'No compiled x64 Release TabletAudioSample.sys found.'
}
$inx = Join-Path $sample 'ComponentizedAudioSample.inx'
if (-not (Test-Path $inx)) { throw 'Pinned driver INF template not found.' }

$stamp = Require-Tool 'StampInf'
$infverif = Require-Tool 'InfVerif'
$inf2cat = Require-Tool 'Inf2Cat'

$out = [System.IO.Path]::GetFullPath($OutDir)
if (Test-Path $out) {
  if ((Get-ChildItem $out -Force | Measure-Object).Count -gt 0) {
    throw 'OutDir must be empty, to avoid silently retaining an old driver binary.'
  }
} else { New-Item -ItemType Directory -Path $out | Out-Null }

# An actual INF package must reference only files produced by LouderME.
$expectedFiles = @($meta.package_file_references)
if ($expectedFiles.Count -ne 1 -or $expectedFiles[0] -ne 'tabletaudiosample.sys' -or
    $meta.sample_keyword_detector_included) {
  throw 'Driver package references include unshipped demo dependencies.'
}
$destInf = Join-Path $out 'LouderMeVirtualRenderLab.inf'
$destSys = Join-Path $out 'TabletAudioSample.sys'
Copy-Item $inx $destInf
Copy-Item $sys.FullName $destSys

# Genuine WDK tools: stamp architecture, date, version and catalog name.
& $stamp '-f' $destInf '-d' '*' '-v' '1.0.0.0' '-c' 'LouderMeVirtualRenderLab.cat'
if ($LASTEXITCODE -ne 0) { throw "StampInf failed with exit $LASTEXITCODE." }
$infText = Get-Content $destInf -Raw
if ($infText -notmatch [regex]::Escape($meta.hardware_id) -or
    $infText -match 'Root\\sysvad_ComponentizedAudioSample' -or
    $infText -match 'TODO-Set-Provider') {
  throw 'Generated INF hardware ID/provider failed identity assertions.'
}

& $infverif '/h' $destInf
if ($LASTEXITCODE -ne 0) { throw "InfVerif Desktop/WHQL-signability rules failed with exit $LASTEXITCODE." }
& $inf2cat "/driver:$out" '/os:10_GE_X64,10_25H2_X64' '/verbose'
if ($LASTEXITCODE -ne 0) { throw "Inf2Cat signability failed with exit $LASTEXITCODE." }

$cat = Join-Path $out 'LouderMeVirtualRenderLab.cat'
if (-not (Test-Path $cat)) { throw 'Inf2Cat did not produce a catalog.' }
$sum = (Get-FileHash $destSys -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "LAB INF/CAT validation passed; unsigned SYS SHA-256: $sum"
Write-Warning 'Unsigned/WDK-test-signed lab files. Installed virtual audio playback and physical routing remain unverified.'
Write-Warning 'Do not ship or install on a primary workstation.'
