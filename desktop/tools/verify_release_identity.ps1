# LouderMe-only publisher/installer/update feed guard. Run in CI before publishing.
$ErrorActionPreference = 'Stop'
$installer = Get-Content 'desktop/installer/LouderMe.iss' -Raw
$updater = Get-Content 'desktop/src/LouderMeDesktop/DesktopUpdateService.cs' -Raw

$expected = @(
  'AppPublisherURL=https://github.com/michels-lab',
  'AppSupportURL=https://github.com/michels-lab/LouderME-Releases/releases',
  'AppUpdatesURL=https://github.com/michels-lab/LouderME-Releases/releases'
)
foreach ($item in $expected) {
  if (-not $installer.Contains($item)) {
    throw "LouderMe Windows installer has a wrong product/publisher URL: $item"
  }
}
if ($installer -match 'michel-s-life-releases|github\.com/realmichelduarte') {
  throw "LouderMe installer contains legacy/foreign repository links."
}
foreach ($required in @(
  'LouderME-Releases/main/louderme-desktop/latest.json',
  'ValidateManifest(manifest)',
  'OfficialDownloadPath',
  'OfficialReleasePath',
  'LouderMe Desktop',
  'RunContractSelfTests'
)) {
  if (-not $updater.Contains($required)) {
    throw "LouderMe updater publisher identity contract missing: $required"
  }
}
if ($updater -match 'LegacyFeedUrl|foreach \(var url in new\[\]') {
  throw "LouderMe updater still has fallback to another app's release feed."
}

dotnet run --project desktop/src/LouderMeDesktop/LouderMeDesktop.csproj -c Release --no-build -- --check-update-contract
if ($LASTEXITCODE -ne 0) {
  throw "LouderMe updater rejected its own contract tests or accepted a foreign feed."
}
Write-Host 'PASS: LouderMe Windows installer and updater are publisher-isolated.'
