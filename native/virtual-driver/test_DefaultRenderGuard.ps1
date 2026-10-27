#requires -Version 7.0
# Device-free contract: synthetic hashes only, no audio endpoints or drivers.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'DefaultRenderGuard.ps1')
$expected = ('a' * 64)
$other = ('b' * 64)
$parsed = Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output ("DEFAULT_RENDER_SHA256=$expected`r`n")
if ($parsed -cne $expected) { throw 'Read-only default output fingerprint changed.' }
Assert-LouderMeDefaultRenderUnchanged -Expected $expected -Actual $expected -Phase 'after-cleanup'
$script:rejectCount = 0
function Reject([string]$name, [scriptblock]$block) {
  try { & $block | Out-Null } catch { $script:rejectCount++; return }
  throw "Default route preflight false positive: $name"
}
Reject 'failed WASAPI call' { Test-LouderMeDefaultRenderFingerprint -ExitCode 1 -Output "DEFAULT_RENDER_SHA256=$expected" }
Reject 'empty render enumeration' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output '' }
Reject 'legacy friendly name' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output '[Speakers] Sensitive endpoint ID' }
Reject 'duplicate records' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_RENDER_SHA256=$expected`nDEFAULT_RENDER_SHA256=$expected" }
Reject 'extra log text' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "Warning`nDEFAULT_RENDER_SHA256=$expected" }
Reject 'mixed-case fingerprint' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_RENDER_SHA256=$($expected.ToUpperInvariant())" }
Reject 'truncated digest' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_RENDER_SHA256=$($expected.Substring(0,63))" }
Reject 'extended digest' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_RENDER_SHA256=$expected$(('a' * 1))" }
Reject 'raw endpoint leak' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_RENDER_SHA256=$expected endpoint={PRIVATE}" }
Reject 'renamed field' { Test-LouderMeDefaultRenderFingerprint -ExitCode 0 -Output "DEFAULT_DEVICE_ID=$expected" }
Reject 'default device changed' { Assert-LouderMeDefaultRenderUnchanged -Expected $expected -Actual $other -Phase 'after-install' }
Reject 'identity temporarily unknown' { Assert-LouderMeDefaultRenderUnchanged -Expected $expected -Actual '' -Phase 'after-install' }
Reject 'invalid expected digest' { Assert-LouderMeDefaultRenderUnchanged -Expected 'unknown' -Actual $expected -Phase 'after-install' }
Reject 'faked uppercase actual' { Assert-LouderMeDefaultRenderUnchanged -Expected $expected -Actual $expected.ToUpperInvariant() -Phase 'after-cleanup' }
if ($script:rejectCount -ne 14) { throw "Expected 14 adversarial route fixtures; got $script:rejectCount." }
Write-Host "PASS: default Windows multimedia render identity guard and 14 false-positive controls (no device)."
