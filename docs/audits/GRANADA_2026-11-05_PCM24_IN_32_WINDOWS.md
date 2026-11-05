# Operación Granada — 2026-11-05: real PCM24 in 32-bit Windows containers

## Root cause
Windows WAVEFORMATEXTENSIBLE explicitly permits 24 significant PCM bits left-aligned in 32-bit integer containers, with unused low eight bits zero. The SysVAD LAB relay already tracks that exact valid-bit count. The independent Windows source, managed PCM adapter, virtual-only listener and physical tone source, however, rejected or confused this legal representation with IEEE float32. This could prevent a 24-bit physical or virtual endpoint from being exercised at all.

Microsoft spec: https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ksmedia/ns-ksmedia-waveformatextensible

## Functional implementation
- Accept only 32-bit **extensible PCM subtype** with precisely 24 valid bits; do not accept full-width integer PCM32, arbitrary partial precision, invalid channel masks, or partial-valid float.
- Decode the signed top 24 bits to interleaved float32 and require all eight low padding bits zero; encode native DSP float32 back to sign-extended left-aligned 24-in-32 integer PCM with zero padding and no callback allocation.
- The separate Windows application tone source and virtual-only 440-Hz stimulus now write PCM24-in-32 instead of treating every 32-bit container as float. Physical-route generator and route preflight cover the same representation; 44.1/48/96k mono/stereo/8ch matrices and numeric evidence are executed by the Windows CI.
- Deterministic signed -1, 0, +0.5 and positive-rail vectors; conversion between packed stereo 24-bit and aligned 32-bit frames, invalid padding rejection, and PCM32/float aliases rejected.

## Evidence limits
All results remain source and device-free CI until exact-head native Windows .NET/C++ tests pass. The change does not install/sign SysVAD, verify independent Windows app PCM via a deployed virtual endpoint, prove a true-peak limiter, certify speaker sound or enable a consumer release. Issue #48 remains open. No RC / no release.
