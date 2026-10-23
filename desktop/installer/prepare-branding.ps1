param(
  [Parameter(Mandatory = $true)]
  [string]$MagickPath
)

$ErrorActionPreference = 'Stop'
$assets = 'desktop/src/LouderMeDesktop/Assets'
$iconSource = 'app/src/main/assets/branding/louderme/official-app-icon.svg'
$lockupSource = 'app/src/main/assets/branding/louderme/official-lockup.svg'

if (-not (Test-Path $iconSource)) { throw 'Official LouderMe app icon is missing.' }
if (-not (Test-Path $lockupSource)) { throw 'Official LouderMe product lockup SVG is missing.' }
New-Item -ItemType Directory -Force $assets | Out-Null

& $MagickPath -background none $iconSource -define icon:auto-resize=256,128,64,48,32,16 "$assets/LouderMe.ico"
if ($LASTEXITCODE -ne 0) { throw 'LouderMe ICO generation failed.' }

# Rasterize the ACTUAL approved Option 4 logo. Do not approximate its
# geometry, text, spacing or colors with independent WinForms labels.
# Official navy wordmark requires a light background to remain legible.
& $MagickPath -background none -density 220 $lockupSource -resize '960x360' -strip "$assets/LouderMeOfficialLockup.png"
if ($LASTEXITCODE -ne 0) { throw 'Official LouderMe lockup rasterization failed.' }

foreach ($file in @(
  "$assets/LouderMe.ico",
  "$assets/LouderMeOfficialLockup.png"
)) {
  if (-not (Test-Path $file)) { throw "Windows branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Windows branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated canonical LouderMe Windows ICO and official product lockup PNG.'
