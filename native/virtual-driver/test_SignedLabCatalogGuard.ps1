#requires -Version 7.0
# Hardware-free verification of WDK SHA256 provenance and VM trust boundary.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'SignedLabCatalogGuard.ps1')
Add-Type -AssemblyName System.IO.Compression

function Sha([byte[]]$raw) {
  return [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
}
function Assert-Refused([string]$label, [scriptblock]$action) {
  $failed = $false
  try { & $action | Out-Null } catch { $failed = $true }
  if (-not $failed) { throw "LAB guard accepted invalid case: $label" }
}
$dir = Join-Path ([IO.Path]::GetTempPath()) "LouderMe-VM-Catalog-Test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $dir | Out-Null
$zipName = 'LouderMe-VirtualRender-LAB-UNSIGNED.zip'
$zip = Join-Path $dir $zipName
$shaFile = "$zip.sha256"
$package = Join-Path $dir 'signed-lab'
New-Item -ItemType Directory -Path $package | Out-Null

try {
  $payload = [ordered]@{}
  $payload['LouderMeVirtualRenderLab.inf'] = [Text.Encoding]::UTF8.GetBytes('LouderMe INF synthetic sample')
  $payload['LouderMeVirtualRenderLab.cat'] = [Text.Encoding]::UTF8.GetBytes('LouderMe synthetic unsigned CAT')
  $payload['TabletAudioSample.sys'] = [Text.Encoding]::UTF8.GetBytes('MZ synthetic kernel driver test')
  $payload['LICENSE-MS-PL.txt'] = [Text.Encoding]::UTF8.GetBytes('The Microsoft Public License (MS-PL)')
  $manifest = [ordered]@{
    upstream_commit = '2dc3fd3a0cc84a2933f2194e7ec0871584979071'
    hardware_id = 'Root\LouderMe_VirtualRender_Lab'
    lab_only = $true
    render_endpoints = 1
    capture_endpoints = 0
    driver_signed = $false
    driver_installed = $false
    real_audio_verified = $false
  }
  $payload['LOUDERME_LAB_MANIFEST.json'] = [Text.Encoding]::UTF8.GetBytes(
    (ConvertTo-Json $manifest -Depth 5))
  $checksums = [ordered]@{}
  foreach ($key in @('LouderMeVirtualRenderLab.inf','LouderMeVirtualRenderLab.cat',
      'TabletAudioSample.sys','LOUDERME_LAB_MANIFEST.json','LICENSE-MS-PL.txt')) {
    $checksums[$key] = Sha $payload[$key]
  }
  $proof = [ordered]@{
    upstream_commit = $manifest.upstream_commit
    hardware_id = $manifest.hardware_id
    catalog_signed = $false
    package_installed = $false
    real_audio_verified = $false
    global_audio_amplification_verified = $false
    microsoft_store_approved = $false
    source_checksums_sha256 = $checksums
    github_actions_run_id = '43342673254'
  }
  $payload['LAB_PACKAGE_PROVENANCE.json'] = [Text.Encoding]::UTF8.GetBytes(
    (ConvertTo-Json $proof -Depth 7))
  $payload['README-DO-NOT-INSTALL.txt'] = [Text.Encoding]::UTF8.GetBytes(
    'Do not install this laboratory file.')
  $sums = @(
    $payload.Keys | Sort-Object -CaseSensitive | ForEach-Object {
      "$(Sha $payload[$_])  $_"
    }
  )
  $payload['SHA256SUMS.txt'] = [Text.Encoding]::ASCII.GetBytes(
    ($sums -join [Environment]::NewLine) + [Environment]::NewLine)

  function Write-TestZip([switch]$ExtraFile) {
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    $z = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
      foreach ($n in $payload.Keys) {
        $entry = $z.CreateEntry("LouderMe-VirtualRender-LAB-UNSIGNED/$n")
        $stream = $entry.Open()
        try { $stream.Write($payload[$n]) } finally { $stream.Dispose() }
      }
      if ($ExtraFile) {
        $extra = $z.CreateEntry('evil/extra.exe')
        $extra.Open().Dispose()
      }
    } finally { $z.Dispose() }
    $sha = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($shaFile, "$sha  $zipName")
  }
  Write-TestZip
  $trusted = Get-LouderMeUnsignedLabSource -ZipPath $zip -Sha256Sidecar $shaFile
  if ($trusted.InfSha256 -cne (Sha $payload['LouderMeVirtualRenderLab.inf']) -or
      $trusted.SysSha256 -cne (Sha $payload['TabletAudioSample.sys']) -or
      $trusted.RunId -cne '43342673254') {
    throw 'Positive verified original WDK source identity failed.'
  }

  $originalSidecar = [IO.File]::ReadAllText($shaFile)
  [IO.File]::WriteAllText($shaFile, '0' * 64 + "  $zipName")
  Assert-Refused 'forged WDK ZIP sidecar' {
    Get-LouderMeUnsignedLabSource -ZipPath $zip -Sha256Sidecar $shaFile
  }
  [IO.File]::WriteAllText($shaFile, $originalSidecar)

  Write-TestZip -ExtraFile
  Assert-Refused 'extra path injected into WDK archive' {
    Get-LouderMeUnsignedLabSource -ZipPath $zip -Sha256Sidecar $shaFile
  }
  Write-TestZip

  $originalProof = $payload['LAB_PACKAGE_PROVENANCE.json']
  $proof.catalog_signed = $true
  $payload['LAB_PACKAGE_PROVENANCE.json'] = [Text.Encoding]::UTF8.GetBytes(
    (ConvertTo-Json $proof -Depth 7))
  Write-TestZip
  Assert-Refused 'forge Store-ready signed catalog evidence' {
    Get-LouderMeUnsignedLabSource -ZipPath $zip -Sha256Sidecar $shaFile
  }
  $payload['LAB_PACKAGE_PROVENANCE.json'] = $originalProof
  Write-TestZip

  foreach ($n in @('LouderMeVirtualRenderLab.inf','LouderMeVirtualRenderLab.cat',
                   'TabletAudioSample.sys')) {
    [IO.File]::WriteAllBytes((Join-Path $package $n), $payload[$n])
  }
  $tool = Join-Path $dir 'signtool-fixture.exe'
  [IO.File]::WriteAllBytes($tool, [Text.Encoding]::UTF8.GetBytes('not executed'))
  Assert-Refused 'catalog remains unsigned before VM installation' {
    Assert-LouderMeSignedLabCatalog -PackageDir $package -UnsignedSource $trusted -SignToolExe $tool -SignerThumbprint ('A'*40)
  }

  [IO.File]::WriteAllText((Join-Path $package 'TabletAudioSample.sys'), 'tampered')
  Assert-Refused 'kernel SYS changed after WDK verification' {
    Assert-LouderMeSignedLabCatalog -PackageDir $package -UnsignedSource $trusted -SignToolExe $tool -SignerThumbprint ('A'*40)
  }
  [IO.File]::WriteAllBytes((Join-Path $package 'TabletAudioSample.sys'), $payload['TabletAudioSample.sys'])
  [IO.File]::WriteAllText((Join-Path $package 'extra.txt'), 'bypass')
  Assert-Refused 'extra file in signed VM package' {
    Assert-LouderMeSignedLabCatalog -PackageDir $package -UnsignedSource $trusted -SignToolExe $tool -SignerThumbprint ('A'*40)
  }

  $vmRunner = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'run_disposable_lab.ps1'))
  if (-not $vmRunner.Contains('Assert-LouderMeSignedLabCatalog') -or
      -not $vmRunner.Contains('before driver install') -or
      -not $vmRunner.Contains('UnsignedLabZip') -or
      -not $vmRunner.Contains('SignerThumbprint')) {
    throw 'Production disposable VM runner does not require WDK provenance and signature membership.'
  }
  $manual = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../.github/workflows/native-physical-lab-manual.yml'))
  if (-not $manual.Contains('unsigned_lab_zip:') -or
      -not $manual.Contains('signer_thumbprint:')) {
    throw 'Physical VM workflow lacks signed driver provenance requirements.'
  }
  Write-Host 'PASS: WDK source SHA, unsigned catalog claims, ZIP members, tamper and installer provenance verified device-free.'
} finally {
  Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
}
