// LouderMe LAB WaveRT frame clock: preserve fractional frames across DPCs.
// Shared kernel/host-tested implementation. No allocation, float or locks.
#pragma once

class LouderMeLabFrameClock final
{
public:
    static ULONG Advance(
        ULONG averageBytesPerSecond,
        USHORT blockAlign,
        ULONG elapsedMilliseconds,
        ULONG* fractionalFrameMillis)
    {
        if (fractionalFrameMillis == NULL)
            return 0;
        if (blockAlign == 0 ||
            averageBytesPerSecond == 0 ||
            averageBytesPerSecond % blockAlign != 0 ||
            *fractionalFrameMillis >= 1000)
        {
            *fractionalFrameMillis = 0;
            return 0;
        }

        // FractionalFrameMillis means thousandths of ONE audio frame,
        // rather than thousandths of a byte (the upstream SysVAD unit).
        // In particular 44.1 kHz advances 44 frames for nine 1-ms ticks,
        // then 45 frames on the tenth, NEVER 44.1 unaligned frames.
        const ULONGLONG framesPerSecond =
            averageBytesPerSecond / blockAlign;
        const ULONGLONG scaledFrames =
            framesPerSecond * elapsedMilliseconds + *fractionalFrameMillis;
        const ULONGLONG wholeFrames = scaledFrames / 1000ULL;
        // Do not let a suspended VM or stalled timer force multi-second
        // work inside a kernel DPC. Cap a single callback to 250 ms of
        // frames, below the ULONG byte limit at 192 kHz 7.1/32-bit too.
        // Old audio is dropped rather than scheduling an unbounded catch-up.
        const ULONGLONG maxWholeFrames = framesPerSecond / 4ULL;
        if (wholeFrames > maxWholeFrames)
        {
            *fractionalFrameMillis = 0;
            return static_cast<ULONG>(maxWholeFrames * blockAlign);
        }

        *fractionalFrameMillis = static_cast<ULONG>(scaledFrames % 1000ULL);
        return static_cast<ULONG>(wholeFrames * blockAlign);
    }
};
