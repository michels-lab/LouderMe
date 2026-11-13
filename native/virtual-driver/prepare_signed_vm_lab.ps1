#requires -Version 7.0
# Manually test-sign the real WDK catalog ONLY in a snapshot-backed VM.
# Never install drivers, change boot policies, import certificates or update
# any Windows default audio endpoint. No keys or certificates are created.
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$UnsignedLabZip,
  [Parameter(Mandatory)][string]$UnsignedLabSha256,
  [Parameter(Mandatory)][string]$SignToolExe,
  [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$SigningThumbprint,
  [Parameter(Mandatory)][string]$OutputDir,
  [Parameter(Mandatory)][ValidateSet('I_ACCEPT_DISPOSABLE_WINDOWS_VM_DRIVER_TEST')]
  [string]$LabConsent
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'SignedLabCatalogGuard.ps1')

if (-not [OperatingSystem]::IsWindows()) { throw 'Only supported in a disposable Windows VM.' }
$computer = Get-CimInstance -ClassName Win32_ComputerSystem -ErrorAction Stop
$model = "$($computer.Manufacturer) $($computer.Model)"
if ($model -notmatch '(?i)Virtual Machine|VMware|VirtualBox|QEMU|KVM|Xen|Parallels') {
  throw 'Refusing kernel lab signing on a non-VM host.'
}
Write-Warning 'A restorable VM snapshot is REQUIRED. Script cannot verify hypervisor snapshots.'
Write-Warning 'No driver will be installed, trusted roots imported or boot settings changed.'

$source = Get-LouderMeUnsignedLabSource -ZipPath $UnsignedLabZip -Sha256Sidecar $UnsignedLabSha256
$tool = Get-Item -LiteralPath $SignToolExe -ErrorAction Stop
if ($tool -isnot [IO.FileInfo] -or $tool.Length -lt 1 -or
    ($tool.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
  throw 'SignTool must be an existing, non-symlinked Windows SDK file.'
}
$thumbprint = $SigningThumbprint.ToUpperInvariant()
$cert = Get-Item -LiteralPath "Cert:\CurrentUser\My\$thumbprint" -ErrorAction Stop
if (-not $cert.HasPrivateKey -or [DateTime]::UtcNow -ge $cert.NotAfter.ToUniversalTime() -or
    [DateTime]::UtcNow -lt $cert.NotBefore.ToUniversalTime() -or
    -not @($cert.EnhancedKeyUsageList | Where-Object {
      $_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3'
    }).Count) {
  throw 'Valid existing CurrentUser Code Signing certificate with private key required.'
}
if ($cert.Subject -notmatch '(?i)LouderMe.*LAB|LAB.*LouderMe') {
  throw 'Refusing to sign with a non-LouderMe-LAB certificate.'
}
$out = [IO.Path]::GetFullPath($OutputDir)
if (Test-Path -LiteralPath $out) {
  throw 'Lab signing destination already exists; never overwrite older packages.'
}
[void][IO.Directory]::CreateDirectory($out)
$success = $false
try {
  Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
  $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $UnsignedLabZip).Path)
  try {
    foreach ($name in @('LouderMeVirtualRenderLab.inf',
                         'LouderMeVirtualRenderLab.cat','TabletAudioSample.sys')) {
      $entry = $zip.GetEntry("LouderMe-VirtualRender-LAB-UNSIGNED/$name")
      if ($null -eq $entry) { throw "Unsigned WDK ZIP missing required file $name." }
      $stream = $entry.Open()
      $writer = [IO.File]::Open((Join-Path $out $name), [IO.FileMode]::CreateNew)
      try { $stream.CopyTo($writer) }
      finally { $writer.Dispose(); $stream.Dispose() }
    }
  } finally { $zip.Dispose() }
  foreach ($entry in @(
      @{Name='LouderMeVirtualRenderLab.inf'; Digest=$source.InfSha256},
      @{Name='TabletAudioSample.sys'; Digest=$source.SysSha256},
      @{Name='LouderMeVirtualRenderLab.cat'; Digest=$source.CatSha256}
    )) {
    $actual = (Get-FileHash -LiteralPath (Join-Path $out $entry.Name) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -cne $entry.Digest) { throw 'Extracted WDK file diverged from CI-verified source.' }
  }
  $catalog = Join-Path $out 'LouderMeVirtualRenderLab.cat'
  if ((Get-AuthenticodeSignature -LiteralPath $catalog).Status -ne 'NotSigned') {
    throw 'Inf2Cat source already signed. Refuse replacing a signature.'
  }
  $null = & $tool.FullName sign /fd SHA256 /s My /sha1 $thumbprint $catalog 2>&1
  if ($LASTEXITCODE -ne 0) { throw 'Local VM-only catalog test signing failed.' }
  $result = Assert-LouderMeSignedLabCatalog -PackageDir $out -UnsignedSource $source -SignToolExe $tool.FullName -SignerThumbprint $thumbprint
  $success = $true
  Write-Host 'PASS: trusted VM test catalog, exact original INF/SYS and SignTool PnP membership.'
  Write-Host ('Source artifact SHA256: ' + $result.OriginalArchiveSha256)
  Write-Host ('LAB catalog SHA256: ' + $result.CatalogSha256)
  Write-Warning 'NOT production Microsoft kernel signing. No driver installed or physical sound proved.'
} finally {
  if (-not $success -and (Test-Path -LiteralPath $out)) {
    Remove-Item -LiteralPath $out -Recurse -Force -ErrorAction SilentlyContinue
    Write-Warning 'Failed LAB staging discarded; no driver installation performed.'
  }
}
