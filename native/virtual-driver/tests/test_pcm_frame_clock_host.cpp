// Exact kernel frame-clock helper compiled and exercised without a driver.
// Supports PCM16/24/32 and float32 at fractional-rate (44.1 kHz) clocks.
#include <cassert>
#include <cstdint>
#include <iostream>
#include <limits>
using ULONG = std::uint32_t;
using USHORT = std::uint16_t;
using ULONGLONG = std::uint64_t;
#include "../LouderMeLabFrameClock.h"

static void ExerciseEveryMillisecond(ULONG rate, USHORT frameSize)
{
    ULONG carry = 0;
    std::uint64_t sum = 0;
    for (int tick = 1; tick <= 5000; ++tick)
    {
        ULONG bytes = LouderMeLabFrameClock::Advance(
            rate * frameSize, frameSize, 1, &carry);
        assert(bytes % frameSize == 0);
        sum += bytes;
        assert(sum == (std::uint64_t(rate) * tick / 1000) * frameSize);
        assert(carry < 1000);
        if (rate == 44100 && tick % 10 == 0)
            assert(bytes == 45 * frameSize);
    }
    assert(sum == std::uint64_t(rate) * 5 * frameSize);
    assert(carry == 0);
}

static void ExerciseJitteredIntervals(ULONG rate, USHORT frameSize)
{
    constexpr ULONG timeSteps[] = {1, 1, 3, 7, 17, 2, 47, 0, 9, 5, 6, 12};
    ULONG carry = 0;
    std::uint64_t sum = 0;
    std::uint64_t elapsed = 0;
    for (int cycle = 0; cycle < 1000; ++cycle)
        for (auto ms : timeSteps)
        {
            elapsed += ms;
            ULONG bytes = LouderMeLabFrameClock::Advance(
                rate * frameSize, frameSize, ms, &carry);
            assert(bytes % frameSize == 0);
            sum += bytes;
            assert(sum == (elapsed * rate / 1000) * frameSize);
        }
}

int main()
{
    // 44.1kHz often produces 1ms DMA byte movements that are NOT an
    // integral sample frame with the original upstream byte-based carry.
    ULONG oldRemainder = 0;
    ULONG upstream1 = (44100 * 6 + oldRemainder) / 1000;
    oldRemainder = (44100 * 6 + oldRemainder) % 1000;
    ULONG upstream2 = (44100 * 6 + oldRemainder) / 1000;
    assert(upstream1 == 264 && upstream2 == 265 && upstream2 % 6 != 0);

    for (ULONG rate : {8000U, 44100U, 48000U, 96000U, 192000U})
        for (USHORT frame : {USHORT(2), USHORT(3), USHORT(4),
                             USHORT(6), USHORT(8)})
        {
            ExerciseEveryMillisecond(rate, frame);
            ExerciseJitteredIntervals(rate, frame);
        }

    ULONG carry = 0;
    ULONG bytes = LouderMeLabFrameClock::Advance(44100 * 6, 6, 1, &carry);
    assert(bytes == 264 && carry == 100);
    bytes = LouderMeLabFrameClock::Advance(44100 * 6, 6, 0, &carry);
    assert(bytes == 0 && carry == 100);
    // A STOP/START resets this per-stream carried fractional frame.
    carry = 0;
    bytes = LouderMeLabFrameClock::Advance(44100 * 6, 6, 1, &carry);
    assert(bytes == 264 && carry == 100);

    // Invalid metadata is fail-closed. The maximum timestamp saturates
    // to a complete frame count instead of wrapping a 32-bit DMA offset.
    carry = 999;
    assert(LouderMeLabFrameClock::Advance(123, 6, 1, &carry) == 0);
    assert(carry == 0);
    assert(LouderMeLabFrameClock::Advance(44100, 0, 1, &carry) == 0);
    assert(LouderMeLabFrameClock::Advance(0, 6, 1, &carry) == 0);
    carry = 1000;
    assert(LouderMeLabFrameClock::Advance(44100 * 6, 6, 1, &carry) == 0);
    assert(carry == 0);

    // A 30-second VM suspension must not ask the WaveRT DPC to process
    // 30 seconds of audio in one callback. Exactly 250ms is the ceiling.
    for (ULONG rate : {44100U, 48000U, 96000U, 192000U})
    {
        carry = 0;
        ULONG cappedPause = LouderMeLabFrameClock::Advance(
            rate * 6, 6, 30000, &carry);
        assert(cappedPause == rate * 6 / 4 && carry == 0);
        ULONG resumed = LouderMeLabFrameClock::Advance(
            rate * 6, 6, 1, &carry);
        assert(resumed % 6 == 0);
        assert(resumed == (rate / 1000) * 6);
    }
    ULONG capped = LouderMeLabFrameClock::Advance(
        192000 * 8, 8, std::numeric_limits<ULONG>::max(), &carry);
    assert(capped == 192000 * 8 / 4 && carry == 0);
    assert(capped <= std::numeric_limits<ULONG>::max());

    std::cout << "PASS: real WaveRT frame clock is drift-free and whole-frame at 44.1/48/96kHz, "
                 "preserves jittered time, resets across stream epochs and limits VM-pause DPC catch-up\n";
}
