# Granada 2026-11-08 — SysVAD lab FIFO supports 5.1/7.1 complete PCM frames

## Functional mismatch addressed
Windows managed bridge now supports speaker-aware 5.1/7.1 -> stereo mapping, but the pinned SysVAD loopback relay formerly accepted only one/two channels and its 65520-byte FIFO was not aligned to 8-channel / 32-bit (32-byte) frames. Handling wider PCM at that capacity would split interleaved samples during wrap-around and corrupt channel identity. Format transitions also checked sample width/rate but did not include the multichannel speaker mask.

## Kernel-source changes (LAB only)
- Extend Supported() exclusively to 6ch/8ch WAVEFORMATEXTENSIBLE with recognized 5.1 back 0x3f, 5.1 side 0x60f or 7.1 0x63f speaker masks and full frame alignment; mono/stereo wrapper behavior remains intact. Reject legacy maskless 6/8ch PCM even if its frame count is well formed.
- Change bounded nonpaged FIFO to **65376 bytes**, divisible by LCM=288 for 16/24/32-bit 1/2/6/8 channel frame sizes, including packed 24bit six-channel and float32 eight-channel.
- Store canonical speaker mask alongside sample kind, valid bits, rate and frame alignment; reject capture with different 5.1 layout (no silent swapped surround channels), flush stale PCM epoch on mismatch.
- No new callback allocations, driver installation, signature, audio capture file or default endpoint changes.

## Host / build tests
- The **actual header compiled under host kernel stubs** gets 5.1 back/side and 7.1 × 16/24/32-bit whole-frame wrap, latest-frames byte identity, silence when empty, negative maskless/legacy layouts, cross-mask epoch rejection, and the existing concurrent FIFO/DRM checks.
- WaveRT frame clock host tests cover every frame size through 32 bytes at 8/44.1/48/96/192 kHz.
- SysVAD derivation tests guard the new capacity and speaker-mask field. WDK CI still builds unsigned lab source for analysis only.

## Remaining limitations
Whether the pinned SysVAD render pin advertises 5.1/7.1 mixes in an installed VM is **not** verified and may require a separate endpoint format-table change; this source change only allows the relay to safely transport such negotiated formats. Real signed/installed virtual endpoint, independent app, native DSP, physical speaker and Bluetooth/HDMI latency tests remain under #48. No release or RC.

## Revisión adicional: el SysVAD fijado solo anuncia estéreo

La inspección directa del `audio/sysvad/EndpointsCommon/speakerwavtable.h` en el commit upstream fijado `2dc3fd3...` confirmó `SPEAKER_HOST_MAX_CHANNELS 2` y `SPEAKER_HOST_MAX_BITS_PER_SAMPLE 16`, con todas las entradas de host y motor en 2ch. El nuevo FIFO multicanal nunca podría negociarse así. Se agregó `patch_speaker_wavtable` en `prepare_sysvad.py`: amplía los límites de host a 8ch/32bit y añade formatos **explícitos** 5.1 back, 5.1 side y 7.1 a 44.1/48/96 kHz, PCM16, PCM24, PCM24-in-32 y float32 en tablas host y audio engine. Mantiene intactos los índices originales (y default stereo), y el loopback reutiliza la tabla host; offload y teléfono no se tocan. El parche falla si SysVAD cambia su estructura. Las pruebas sintéticas del parche exigen exactamente 72 entradas nuevas, máscaras y tasas correctas; la derivación CI prueba también el archivo de Microsoft real. **No implica que Windows haya instalado o negociado uno de esos formatos; la validación INF/CAT sigue bloqueada por el SDK del runner y falta VM física**.
