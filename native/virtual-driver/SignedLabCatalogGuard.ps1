#requires -Version 7.0
# LAB ONLY. Original unsigned ZIP and SHA256 sidecar come from WDK Actions.
# Read-only checks. No certificate import, boot change, PnP or audio access.
Set-StrictMode -Version Latest

function Get-LouderMeUnsignedLabSource {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$ZipPath,
        [Parameter(Mandatory)][string]$Sha256Sidecar)

  $zip = Get-Item -LiteralPath $ZipPath -ErrorAction Stop
  $side = Get-Item -LiteralPath $Sha256Sidecar -ErrorAction Stop
  if ($zip -isnot [IO.FileInfo] -or $side -isnot [IO.FileInfo] -or
      $zip.Length -gt 32MB -or $side.Length -gt 2048 -or
      ($zip.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or
      ($side.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw 'Unsafe WDK lab ZIP or checksum path.'
  }
  $hash = (Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  if ([IO.File]::ReadAllText($side.FullName).Trim() -cne "$hash  $($zip.Name)") {
    throw 'Unsigned WDK artifact differs from its Actions SHA256 sidecar.'
  }

  Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
  $archive = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
  try {
    $prefix = 'LouderMe-VirtualRender-LAB-UNSIGNED/'
    $names = @('LouderMeVirtualRenderLab.inf','LouderMeVirtualRenderLab.cat',
      'TabletAudioSample.sys','LOUDERME_LAB_MANIFEST.json','LICENSE-MS-PL.txt',
      'LAB_PACKAGE_PROVENANCE.json','README-DO-NOT-INSTALL.txt','SHA256SUMS.txt')
    $entries = @($archive.Entries)
    $received = @($entries | ForEach-Object { $_.FullName } | Sort-Object -CaseSensitive)
    $wanted = @($names | ForEach-Object { $prefix + $_ } | Sort-Object -CaseSensitive)
    if ($entries.Count -ne $names.Count -or
        (($received -join '|') -cne ($wanted -join '|'))) {
      throw 'Unsigned WDK ZIP has extra, missing or duplicate entries.'
    }
    $bytes = @{}
    foreach ($entry in $entries) {
      if ($entry.Length -lt 1 -or $entry.Length -gt 32MB) {
        throw 'Unsigned WDK archive entry is missing or oversized.'
      }
      $stream = $entry.Open()
      $memory = [IO.MemoryStream]::new()
      try { $stream.CopyTo($memory); $value = $memory.ToArray() }
      finally { $memory.Dispose(); $stream.Dispose() }
      if ($value.Length -ne $entry.Length) { throw 'Truncated WDK entry.' }
      $bytes[$entry.FullName.Substring($prefix.Length)] = $value
    }
    $manifest = [Text.Encoding]::UTF8.GetString($bytes['LOUDERME_LAB_MANIFEST.json']) |
      ConvertFrom-Json -ErrorAction Stop
    $proof = [Text.Encoding]::UTF8.GetString($bytes['LAB_PACKAGE_PROVENANCE.json']) |
      ConvertFrom-Json -ErrorAction Stop
    if ($manifest.upstream_commit -cne '2dc3fd3a0cc84a2933f2194e7ec0871584979071' -or
        $proof.upstream_commit -cne $manifest.upstream_commit -or
        $manifest.hardware_id -cne 'Root\LouderMe_VirtualRender_Lab' -or
        $proof.hardware_id -cne $manifest.hardware_id -or
        -not $manifest.lab_only -or $manifest.render_endpoints -ne 1 -or
        $manifest.capture_endpoints -ne 0 -or $manifest.driver_signed -or
        $manifest.driver_installed -or $manifest.real_audio_verified) {
      throw 'Unpinned WDK driver manifest or forged signed/installed/audio claims.'
    }
    foreach ($field in @('catalog_signed','package_installed','real_audio_verified',
                         'global_audio_amplification_verified','microsoft_store_approved')) {
      if ($proof.$field -isnot [bool] -or $proof.$field) {
        throw "Unsigned WDK artifact falsely claims $field."
      }
    }
    $sumNames = @($names | Where-Object { $_ -ne 'SHA256SUMS.txt' } | Sort-Object -CaseSensitive)
    $lines = @([Text.Encoding]::ASCII.GetString($bytes['SHA256SUMS.txt']).Trim() -split '\r?\n')
    if ($sumNames.Count -ne $lines.Count) { throw 'Wrong number of WDK checksums.' }
    for ($i=0; $i -lt $sumNames.Count; $i++) {
      $n = $sumNames[$i]
      $sha = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($bytes[$n])).ToLowerInvariant()
      if ($lines[$i] -cne "$sha  $n") {
        throw "Original WDK ZIP entry $n differs from source checksums."
      }
    }
    foreach ($n in @('LouderMeVirtualRenderLab.inf','LouderMeVirtualRenderLab.cat',
                     'TabletAudioSample.sys','LOUDERME_LAB_MANIFEST.json','LICENSE-MS-PL.txt')) {
      $sha = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($bytes[$n])).ToLowerInvariant()
      if ($proof.source_checksums_sha256.$n -cne $sha) {
        throw "Original WDK provenance differs from $n."
      }
    }
    return [pscustomobject]@{
      ZipSha256 = $hash
      InfSha256 = $proof.source_checksums_sha256.'LouderMeVirtualRenderLab.inf'
      SysSha256 = $proof.source_checksums_sha256.'TabletAudioSample.sys'
      CatSha256 = $proof.source_checksums_sha256.'LouderMeVirtualRenderLab.cat'
      RunId = [string]$proof.github_actions_run_id
    }
  } finally { $archive.Dispose() }
}

function Assert-LouderMeSignedLabCatalog {
  [CmdletBinding()]
  param([Parameter(Mandatory)][string]$PackageDir,
        [Parameter(Mandatory)]$UnsignedSource,
        [Parameter(Mandatory)][string]$SignToolExe,
        [Parameter(Mandatory)][ValidatePattern('^[a-fA-F0-9]{40}$')][string]$SignerThumbprint)

  $dir = Get-Item -LiteralPath $PackageDir -ErrorAction Stop
  if ($dir -isnot [IO.DirectoryInfo] -or
      ($dir.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw 'Signed lab package cannot be a reparse directory.'
  }
  $files = @(Get-ChildItem -LiteralPath $dir.FullName -Force)
  $required = @('LouderMeVirtualRenderLab.inf','LouderMeVirtualRenderLab.cat',
                'TabletAudioSample.sys')
  if ($files.Count -ne 3 -or
      ((@($files.Name | Sort-Object -CaseSensitive) -join '|') -cne
        (@($required | Sort-Object -CaseSensitive) -join '|'))) {
    throw 'Signed lab package must have exactly the approved INF/CAT/SYS names.'
  }
  $inf = Join-Path $dir.FullName $required[0]
  $cat = Join-Path $dir.FullName $required[1]
  $sys = Join-Path $dir.FullName $required[2]
  foreach ($p in @($inf,$cat,$sys,$SignToolExe)) {
    $file = Get-Item -LiteralPath $p -ErrorAction Stop
    if ($file -isnot [IO.FileInfo] -or $file.Length -lt 1 -or
        ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
      throw 'Signed driver package has a missing, empty or redirected file.'
    }
  }
  foreach ($p in @($inf,$sys)) {
    $sha = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()
    $wanted = if ($p -eq $inf) { $UnsignedSource.InfSha256 } else { $UnsignedSource.SysSha256 }
    if ($sha -cne $wanted) { throw 'LAB INF/SYS changed since official WDK validation.' }
  }
  $catSha = (Get-FileHash -LiteralPath $cat -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($catSha -ceq $UnsignedSource.CatSha256) {
    throw 'LAB catalog has not been signed by a trusted certificate.'
  }
  $signature = Get-AuthenticodeSignature -LiteralPath $cat
  if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate -or
      $signature.SignerCertificate.Thumbprint -cne $SignerThumbprint.ToUpperInvariant()) {
    throw 'LAB catalog signature is not trusted or does not match approved certificate.'
  }
  # Microsoft documents /pa as PnP install signing policy: test catalog,
  # then independently check exact INF and SYS membership in that catalog.
  foreach ($args in @(
    @('verify','/pa',$cat),
    @('verify','/pa','/c',$cat,$inf),
    @('verify','/pa','/c',$cat,$sys)
  )) {
    $null = & $SignToolExe @args 2>&1
    if ($LASTEXITCODE -ne 0) {
      throw 'Signed catalog membership/trust failed Microsoft SignTool verification.'
    }
  }
  return [pscustomobject]@{
    OriginalArchiveSha256 = $UnsignedSource.ZipSha256
    CatalogSha256 = $catSha
    VmLocalLabSignatureValid = $true
    DriverInstalled = $false
    PhysicalAudioVerified = $false
  }
}
