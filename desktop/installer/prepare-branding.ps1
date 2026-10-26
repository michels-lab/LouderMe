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

# App-surface variant is derived deterministically from the OFFICIAL lockup.
# Retain the exact wave, typography and brand-blue 'Me'; only recolor the
# dark-navy 'Louder' text and tagline so they are legible on the dark UI.
$darkSvg = Get-Content $lockupSource -Raw
if (-not $darkSvg.Contains('fill="#06182d"') -or -not $darkSvg.Contains('fill="#486580"')) {
  throw 'Canonical LouderMe SVG color contract changed: do not invent alternate logo.'
}
$darkSvg = $darkSvg.Replace('fill="#06182d"', 'fill="#eef6ff"').Replace('fill="#486580"', 'fill="#b3c6dc"')
$darkPath = Join-Path $env:TEMP 'louderme-official-dark-lockup.svg'
try {
  [System.IO.File]::WriteAllText($darkPath, $darkSvg, [System.Text.UTF8Encoding]::new($false))
  & $MagickPath -background none -density 220 $darkPath -resize '960x360' -strip "$assets/LouderMeOfficialDarkLockup.png"
  if ($LASTEXITCODE -ne 0) { throw 'Official dark LouderMe lockup rasterization failed.' }
} finally {
  Remove-Item $darkPath -Force -ErrorAction SilentlyContinue
}

# Preserve the actual approved Michel's Lab logo from the canonical tracked
# PNG. Its transparent pixels produced a magenta WinForms DrawToBitmap artifact
# in v0.1.15, so compose it onto the *exact* About-card dark surface before
# embedding. This is NOT a substitute text label or a newly invented mark.
$studioSource = "$assets/MichelsLabLockup.png"
$studioPrepared = "$assets/MichelsLabDarkLockup.png"
if (-not (Test-Path $studioSource)) { throw 'Canonical Michel''s Lab logo missing.' }
& $MagickPath $studioSource -background '#0C1422' -alpha remove -alpha off -resize '640x240' -strip $studioPrepared
if ($LASTEXITCODE -ne 0) { throw 'Official Michel''s Lab dark logo preparation failed.' }

foreach ($file in @(
  "$assets/MichelsLabDarkLockup.png",
  "$assets/LouderMe.ico",
  "$assets/LouderMeOfficialLockup.png",
  "$assets/LouderMeOfficialDarkLockup.png"
)) {
  if (-not (Test-Path $file)) { throw "Windows branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Windows branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated canonical LouderMe Windows ICO and official product lockup PNG.'
