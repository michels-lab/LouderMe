# Granada 2026-11-09 — Virtual multichannel source completion and per-lane PCM proof

## Root causes
1. Actual virtual-only lab runner `probe_independent_virtual_pcm.ps1` still accepted independent Windows source completion only for `channels=[12]`, while both independent source and kernel/bridge negotiate explicit 5.1/7.1 formats. A valid 6/8-channel lab session therefore always failed before DSP/physical measurement.
2. Virtual 440Hz `LoopbackToneVerifier` averaged all speaker lanes. It could pass with one surround lane silent if the others still supplied enough tone, misrepresenting complete multichannel transport.

## Implemented changes
- The real virtual WASAPI receiver restricts 6ch to WAVEFORMATEXTENSIBLE masks 0x3f (back) / 0x60f (side) and 8ch to 0x63f (7.1); maskless or ambiguous multichannel receives fail closed.
- The real virtual-only bridge invokes `LoopbackToneVerifier(requireEverySpeakerLane: true)`. The verifier preallocates sine and cosine projections per channel outside audio callbacks; each declared channel must contain the known 440Hz signal. Normal physical-output monitoring retains its original mix-compatible policy.
- `VirtualSourceCompletionCheck.ps1` validates exactly one completion line from the separate Windows source executable with channel count 1/2/6/8, 8..192 kHz times two seconds, no exit error and no stderr. The actual VM virtual-only probe uses it and stores source frames/channel count alongside measured virtual WASAPI evidence.
- The physical-route source parser now also rejects unexpected 3/4/5/7 channels. Tests cover allowed 6/8 and reject false positive markers, wrong frequency, missing lanes, bad masks, odd/truncated/implausible frame counts.

## Validation
Exact PR SHA native Windows CI runs the C# bridge and virtual decoder/verifier real executable selftests, plus actual PowerShell parser fixtures and syntax checks. Android smoke CI is independent. These are reproducible software tests **without** installed signed driver, true external-app loopback or acoustic output.

## Remaining limits
OS shared-mode effects may remix some multichannel layouts. This lab proof deliberately refuses claiming full 7.1 transport if any lane loses the independent stimulus. The real physical test still needs an operator-approved disposable Windows VM, signed/test-signed lab device installed, explicit virtual/physical endpoints, no-source controls, real independent app→virtual→native DSP→physical loopback gain evidence, installation/uninstall rollback and routing/latency checks (#48). No RC or release.
