param(
  [Parameter(Mandatory = $true)]
  [string]$MagickPath
)

$ErrorActionPreference = 'Stop'
$assets = 'desktop/src/LouderMeDesktop/Assets'
$iconSource = 'app/src/main/assets/branding/louderme/official-app-icon.svg'

if (-not (Test-Path $iconSource)) { throw 'Official LouderMe app icon is missing.' }
New-Item -ItemType Directory -Force $assets | Out-Null

& $MagickPath -background none $iconSource -define icon:auto-resize=256,128,64,48,32,16 "$assets/LouderMe.ico"
if ($LASTEXITCODE -ne 0) { throw 'LouderMe ICO generation failed.' }

foreach ($file in @(
  "$assets/LouderMe.ico"
)) {
  if (-not (Test-Path $file)) { throw "Windows branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Windows branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated LouderMe Windows icon.'
