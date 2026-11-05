# Granada — 2026-11-05: PCM24 precision and surround speaker-layout safety

## Actual defect and scope

The lab Windows decoder, external tone source and route preflight accepted WAVEFORMATEXTENSIBLE by container bit width and subtype alone. A 24-bit container carrying 20 significant bits would be read or written as full-width packed PCM24, and a 5.1/7.1 stream with reordered speaker masks could be fed to a sink with a different channel ordering. Either can produce wrong sound even if frame sizes and rates match. The native SysVAD relay already treats valid bits and layout as part of identity; the managed side must not relax that contract.

## Changes

- Fail closed on extensible formats with valid-bit count different from container width. Explicitly continue to reject PCM32 integer and 24-valid-in-32. Plain PCM16, packed PCM24 and IEEE float32 remain supported.
- Reject extensible masks whose set bits differ from channel count; require explicit speaker masks for 3–8 channels and preserve exact masks in virtual-only float32 probe. Reject source/sink layout mismatch for equal-channel streams before conversion. Same-channel legacy mono/stereo and deliberate mono↔stereo conversion remain supported.
- The independent Windows tone generator rejects partial-precision/invalid layouts. Eight-channel scratch and virtual test vectors use valid extensible masks, not legacy formats without speaker positions.
- Device-free self-tests exercise 20-in-24, 24-in-32, integer PCM32, invalid masks, legitimate alternative 5.1 layouts, mismatched 5.1 routing and valid 8-channel PCM24/float32.

## Limits

Changes require Windows CI at exact PR SHA before integration. They do not certify a signed/installed driver or prove audio from another Windows application reaches physical speakers. Issue #48 remains open. No public release or automatic default Windows endpoint modification.
