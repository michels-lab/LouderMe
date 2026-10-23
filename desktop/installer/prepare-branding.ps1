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

# Preserve the approved Option 4 waveform, lettering and coordinates.
# Dark-mode adaptation changes ONLY text colors; no white rectangle, altered
# paths, new icons or regenerated type composition.
& $MagickPath -background none -density 220 $lockupSource -resize '960x360' -strip "$assets/LouderMeOfficialLockup.png"
if ($LASTEXITCODE -ne 0) { throw 'Official LouderMe lockup rasterization failed.' }
$canonicalSvg = [System.IO.File]::ReadAllText((Resolve-Path $lockupSource).Path)
foreach ($pair in @(@('#06182d','#eff6ff'), @('#486580','#a6bcd4'))) {
  if (-not $canonicalSvg.Contains($pair[0])) { throw "Canonical logo color is missing: $($pair[0])" }
  $canonicalSvg = $canonicalSvg.Replace($pair[0], $pair[1])
}
$temporarySvg = Join-Path ([System.IO.Path]::GetTempPath()) ("louderme-dark-" + [Guid]::NewGuid().ToString('N') + '.svg')
try {
  [System.IO.File]::WriteAllText($temporarySvg, $canonicalSvg, [System.Text.UTF8Encoding]::new($false))
  & $MagickPath -background none -density 220 $temporarySvg -resize '960x360' -strip "$assets/LouderMeDarkLockup.png"
  if ($LASTEXITCODE -ne 0) { throw 'Dark-theme LouderMe lockup generation failed.' }
} finally {
  Remove-Item $temporarySvg -ErrorAction SilentlyContinue
}

foreach ($file in @(
  "$assets/LouderMe.ico",
  "$assets/LouderMeOfficialLockup.png",
  "$assets/LouderMeDarkLockup.png"
)) {
  if (-not (Test-Path $file)) { throw "Windows branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Windows branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated canonical LouderMe ICO, original lockup and transparent dark lockup.'
