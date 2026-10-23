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

# Generate a DARK-SURFACE VARIANT of the same approved SVG. Preserve the
# canonical wave paths, typography, spacing and accent colors. Only change
# the ink of the navy "Louder" text and muted tagline for legibility.
# Do not place a white rectangle behind the product identity.
$canonical = Get-Content $lockupSource -Raw -Encoding UTF8
if (-not $canonical.Contains('fill="#06182d"') -or -not $canonical.Contains('fill="#486580"')) {
  throw 'Canonical official lockup fill colors unexpectedly changed; review manually.'
}
$darkSvg = $canonical.Replace('fill="#06182d"', 'fill="#edf6ff"').Replace('fill="#486580"', 'fill="#a7bed8"')
$temporary = Join-Path $assets 'LouderMeOfficialLockup-dark-tmp.svg'
try {
  [System.IO.File]::WriteAllText($temporary, $darkSvg, [System.Text.UTF8Encoding]::new($false))
  & $MagickPath -background none -density 220 $temporary -resize '960x360' -trim +repage -strip "$assets/LouderMeOfficialLockup.png"
  if ($LASTEXITCODE -ne 0) { throw 'Official LouderMe dark-surface lockup rasterization failed.' }
} finally {
  Remove-Item $temporary -Force -ErrorAction SilentlyContinue
}

foreach ($file in @(
  "$assets/LouderMe.ico",
  "$assets/LouderMeOfficialLockup.png"
)) {
  if (-not (Test-Path $file)) { throw "Windows branding asset missing: $file" }
  if ((Get-Item $file).Length -lt 1000) { throw "Windows branding asset is unexpectedly small: $file" }
}

Write-Host 'Generated canonical LouderMe Windows ICO and official product lockup PNG.'
