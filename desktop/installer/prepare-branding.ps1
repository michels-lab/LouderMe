param(
  [Parameter(Mandatory = $true)]
  [string]$MagickPath
)

$ErrorActionPreference = 'Stop'
$assets = 'desktop/src/LouderMeDesktop/Assets'
$iconSource = 'app/src/main/assets/branding/louderme/official-app-icon.svg'
$lockupSource = 'app/src/main/assets/branding/louderme/official-lockup.svg'

if (-not (Test-Path $iconSource)) { throw 'Official LouderMe app icon is missing.' }
if (-not (Test-Path $lockupSource)) { throw 'Official LouderMe lockup is missing.' }
New-Item -ItemType Directory -Force $assets | Out-Null

& $MagickPath -background none $iconSource -define icon:auto-resize=256,128,64,48,32,16 "$assets/LouderMe.ico"
if ($LASTEXITCODE -ne 0) { throw 'LouderMe ICO generation failed.' }

& $MagickPath -size 164x314 canvas:'#060910' '(' $iconSource -background none -resize '132x132>' ')' -gravity north -geometry '+0+34' -composite '(' $lockupSource -background none -resize '146x94>' ')' -gravity south -geometry '+0+36' -composite -alpha off -type TrueColor "$assets/LouderMeInstallerWizard.bmp"
if ($LASTEXITCODE -ne 0) { throw 'LouderMe installer wizard image generation failed.' }

& $MagickPath -size 55x55 canvas:'#060910' '(' $iconSource -background none -resize '47x47>' ')' -gravity center -composite -alpha off -type TrueColor "$assets/LouderMeInstallerSmall.bmp"
if ($LASTEXITCODE -ne 0) { throw 'LouderMe installer small image generation failed.' }

foreach ($file in @(
  "$assets/LouderMe.ico",
  "$assets/LouderMeInstallerWizard.bmp",
  "$assets/LouderMeInstallerSmall.bmp"
)) {
  if (-not (Test-Path $file)) { throw "Installer branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Installer branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated LouderMe Windows and installer branding assets.'
