// Host-side numerical tests for the SAME header compiled by WDK in SysVAD.
// Minimal kernel API stubs; no audio devices, kernel privileges or installation.
#include <algorithm>
#include <array>
#include <cassert>
#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <thread>
#include <vector>
#include <iostream>
using BYTE = std::uint8_t;
using ULONG = std::uint32_t;
using USHORT = std::uint16_t;
using BOOLEAN = int;
using NTSTATUS = std::int32_t;
using VOID = void;
using KIRQL = int;
using KSPIN_LOCK = std::mutex;
struct GUID { std::uint8_t bytes[16] = {}; };
struct WAVEFORMATEX {
    USHORT wFormatTag = 1;
    USHORT nChannels = 2;
    ULONG nSamplesPerSec = 48000;
    ULONG nAvgBytesPerSec = 192000;
    USHORT nBlockAlign = 4;
    USHORT wBitsPerSample = 16;
    USHORT cbSize = 0;
};
struct WAVEFORMATEXTENSIBLE {
    WAVEFORMATEX Format;
    struct { USHORT wValidBitsPerSample = 0; } Samples;
    ULONG dwChannelMask = 0;
    GUID SubFormat;
};
static constexpr int TRUE = 1, FALSE = 0;
static constexpr USHORT WAVE_FORMAT_PCM = 1, WAVE_FORMAT_IEEE_FLOAT = 3,
    WAVE_FORMAT_EXTENSIBLE = 65534;
static const GUID KSDATAFORMAT_SUBTYPE_PCM = {{1}};
static const GUID KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = {{2}};
static constexpr ULONG POOL_FLAG_NON_PAGED = 1, MINWAVERT_POOLTAG = 0x4C4D5043;
static constexpr NTSTATUS STATUS_SUCCESS = 0, STATUS_INVALID_DEVICE_STATE = -1,
    STATUS_INSUFFICIENT_RESOURCES = -2;
static unsigned g_allocations = 0, g_wipes = 0;
#define _In_
#define _In_opt_
#define _IRQL_requires_max_(x)
#define _In_reads_bytes_(x)
#define _Out_writes_bytes_(x)
#define min(a, b) ((a) < (b) ? (a) : (b))
static bool IsEqualGUIDAligned(const GUID& a, const GUID& b) {
    return std::memcmp(&a, &b, sizeof(GUID)) == 0;
}
static void KeInitializeSpinLock(KSPIN_LOCK*) {}
static void KeAcquireSpinLock(KSPIN_LOCK* lock, KIRQL*) { lock->lock(); }
static void KeReleaseSpinLock(KSPIN_LOCK* lock, KIRQL) { lock->unlock(); }
static void RtlZeroMemory(void* p, std::size_t n) { std::memset(p, 0, n); }
static void RtlCopyMemory(void* dest, const void* src, std::size_t n) {
    std::memcpy(dest, src, n);
}
static void* RtlSecureZeroMemory(void* p, std::size_t n) {
    ++g_wipes;
    return std::memset(p, 0, n);
}
static void* ExAllocatePool2(ULONG, std::size_t n, ULONG) {
    ++g_allocations;
    return std::calloc(1, n);
}
static void ExFreePoolWithTag(void* p, ULONG) { std::free(p); }
#include "../LouderMeLabPcmRelay.h"

static WAVEFORMATEXTENSIBLE Fmt(ULONG rate = 48000, USHORT channels = 2,
                                USHORT bits = 16, USHORT tag = WAVE_FORMAT_PCM,
                                bool floatSubtype = false) {
    WAVEFORMATEXTENSIBLE f{};
    f.Format.nSamplesPerSec = rate;
    f.Format.nChannels = channels;
    f.Format.wBitsPerSample = bits;
    f.Format.nBlockAlign = channels * bits / 8;
    f.Format.wFormatTag = tag;
    f.Format.nAvgBytesPerSec = rate * f.Format.nBlockAlign;
    // WAVEFORMATEXTENSIBLE's cbSize is 22 on the wire, independent of
    // ABI struct padding on Linux host stubs or Windows WDK compilers.
    f.Format.cbSize = tag == WAVE_FORMAT_EXTENSIBLE ? 22 : 0;
    f.SubFormat = floatSubtype ?
        KSDATAFORMAT_SUBTYPE_IEEE_FLOAT : KSDATAFORMAT_SUBTYPE_PCM;
    f.Samples.wValidBitsPerSample = bits;
    f.dwChannelMask = channels == 1 ? 0x4UL : channels == 2 ? 0x3UL :
        channels == 6 ? 0x3FUL : channels == 8 ? 0x63FUL : 0;
    return f;
}
static void Zeros(const std::vector<BYTE>& v) {
    for (auto n : v) assert(n == 0);
}
int main() {
    auto stereo = Fmt();
    auto otherRate = Fmt(44100);
    auto otherChannels = Fmt(48000, 1);
    auto otherSubtype = Fmt(48000, 2, 16, WAVE_FORMAT_EXTENSIBLE);
    otherSubtype.SubFormat.bytes[0] = 3; // unknown GUID must not pass
    LouderMeLabPcmRelay relay;
    assert(relay.Initialize() == STATUS_SUCCESS);
    assert(relay.Initialize() == STATUS_INVALID_DEVICE_STATE);
    assert(g_allocations == 1);

    std::vector<BYTE> quiet(16, 0xff);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet); // No upstream app = never a synthetic tone.

    std::vector<BYTE> data(16);
    for (unsigned i = 0; i < data.size(); ++i) data[i] = BYTE(i + 1);
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    std::vector<BYTE> output(8, 0);
    relay.Consume(output.data(), (ULONG)output.size(), &stereo);
    assert(std::equal(output.begin(), output.end(), data.begin()));
    relay.Consume(output.data(), (ULONG)output.size(), &stereo);
    assert(std::equal(output.begin(), output.end(), data.begin() + 8));
    relay.Consume(output.data(), (ULONG)output.size(), &stereo);
    Zeros(output); // FIFO drained.

    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &otherRate);
    Zeros(quiet);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &otherChannels);
    Zeros(quiet);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &otherSubtype);
    Zeros(quiet); // Mismatch does not consume valid PCM.
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet); // A mismatched capture flushes old PCM, never revives it.
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    assert(quiet == data);

    // A full circular-buffer overwrite keeps the newest 65376 bytes.
    relay.Reset();
    std::vector<BYTE> longSource(66536);
    for (std::size_t i = 0; i < longSource.size(); ++i)
        longSource[i] = BYTE(i % 251);
    relay.Publish(longSource.data(), (ULONG)longSource.size(), &stereo);
    std::vector<BYTE> complete(65376);
    relay.Consume(complete.data(), (ULONG)complete.size(), &stereo);
    assert(std::equal(complete.begin(), complete.end(), longSource.end() - 65376));
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);

    // Wrap around, partial consumption and further publication preserve order.
    relay.Reset();
    std::vector<BYTE> first(65000, 0x11), second(2000, 0x72);
    relay.Publish(first.data(), (ULONG)first.size(), &stereo);
    std::vector<BYTE> mostly(64600);
    relay.Consume(mostly.data(), (ULONG)mostly.size(), &stereo);
    for (BYTE value : mostly) assert(value == 0x11);
    relay.Publish(second.data(), (ULONG)second.size(), &stereo);
    std::vector<BYTE> tail(2400);
    relay.Consume(tail.data(), (ULONG)tail.size(), &stereo);
    for (int i = 0; i < 400; ++i) assert(tail[i] == 0x11);
    for (int i = 400; i < 2400; ++i) assert(tail[i] == 0x72);

    // Explicit format switch drops stale samples instead of reinterpreting bytes.
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    std::vector<BYTE> fresh(16, 0x44);
    relay.Publish(fresh.data(), (ULONG)fresh.size(), &otherRate);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &otherRate);
    Zeros(quiet); // Conflicting capture request flushed the queued 44.1kHz PCM.
    relay.Publish(fresh.data(), (ULONG)fresh.size(), &otherRate);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &otherRate);
    assert(quiet == fresh); // A NEW valid source packet is accepted.

    // 24-bit stereo uses six-byte frames. Ring wrap/overflow must never
    // discard half a sample, including when the input exceeds capacity.
    relay.Reset();
    auto pcm24 = Fmt(48000, 2, 24, WAVE_FORMAT_EXTENSIBLE);
    std::vector<BYTE> source24(66120);
    for (std::size_t i = 0; i < source24.size(); ++i)
        source24[i] = BYTE((i * 7) % 253);
    relay.Publish(source24.data(), (ULONG)source24.size(), &pcm24);
    std::vector<BYTE> result24(65376);
    relay.Consume(result24.data(), (ULONG)result24.size(), &pcm24);
    assert(std::equal(result24.begin(), result24.end(), source24.end() - 65376));

    // Virtual kernel render-to-loopback must transport actual 5.1/7.1
    // frames byte-for-byte without cutting interleaved channels at wrap.
    // Both 5.1 speaker orders and 7.1 are validated; 5.1 masks must not
    // be reinterpreted as each other across a capture epoch.
    for (auto layout : {0x3FUL, 0x60FUL, 0x63FUL})
    {
        USHORT channels = layout == 0x63FUL ? 8 : 6;
        for (USHORT bits : {USHORT(16), USHORT(24), USHORT(32)})
        {
            auto surround = Fmt(48000, channels, bits,
                WAVE_FORMAT_EXTENSIBLE, bits == 32);
            surround.dwChannelMask = layout;
            ULONG frameBytes = surround.Format.nBlockAlign;
            std::vector<BYTE> pcm(65376 + frameBytes * 6);
            for (std::size_t frame = 0; frame < pcm.size() / frameBytes; frame++)
                for (ULONG b = 0; b < frameBytes; b++)
                    pcm[frame * frameBytes + b] =
                        BYTE((frame % 251) + 1);
            relay.Reset();
            relay.Publish(pcm.data(), (ULONG)pcm.size(), &surround);
            std::vector<BYTE> got(65376);
            relay.Consume(got.data(), (ULONG)got.size(), &surround);
            assert(std::equal(got.begin(), got.end(), pcm.end() - 65376));
            for (std::size_t offset = 0; offset < got.size(); offset += frameBytes)
                for (ULONG b = 1; b < frameBytes; ++b)
                    assert(got[offset + b] == got[offset]);
            std::vector<BYTE> silence(frameBytes * 4, 0xCC);
            relay.Consume(silence.data(), (ULONG)silence.size(), &surround);
            Zeros(silence);

            if (channels == 6)
            {
                auto wrongSpeakers = surround;
                wrongSpeakers.dwChannelMask =
                    layout == 0x3FUL ? 0x60FUL : 0x3FUL;
                relay.Publish(pcm.data(), frameBytes * 4, &surround);
                relay.Consume(silence.data(), (ULONG)silence.size(),
                    &wrongSpeakers);
                Zeros(silence);  // no left/right speaker re-interpretation
                relay.Consume(silence.data(), (ULONG)silence.size(), &surround);
                Zeros(silence);  // stale format epoch was invalidated
            }
        }
    }

    // Invalid multichannel legacy wrappers, missing masks and unexpected
    // positions never expose the FIFO, even if frame sizes are valid.
    for (USHORT channels : {USHORT(6), USHORT(8)})
    {
        auto invalid = Fmt(48000, channels, 24, WAVE_FORMAT_PCM);
        relay.Reset();
        std::vector<BYTE> packet(invalid.Format.nBlockAlign * 4, 0x55);
        relay.Publish(packet.data(), (ULONG)packet.size(), &invalid);
        relay.Consume(packet.data(), (ULONG)packet.size(), &invalid);
        Zeros(packet);
        invalid.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
        invalid.Format.cbSize = 22;
        invalid.dwChannelMask = 0;
        relay.Publish(packet.data(), (ULONG)packet.size(), &invalid);
        relay.Consume(packet.data(), (ULONG)packet.size(), &invalid);
        Zeros(packet);
        invalid.dwChannelMask = channels == 6 ? 0x63FUL : 0x3FUL;
        relay.Publish(packet.data(), (ULONG)packet.size(), &invalid);
        relay.Consume(packet.data(), (ULONG)packet.size(), &invalid);
        Zeros(packet);
    }

    // Host and extensible IEEE-float 32-bit samples are supported without
    // touching bytes; unknown GUID and float16 are never considered PCM.
    relay.Reset();
    auto floatHost = Fmt(48000, 2, 32, WAVE_FORMAT_IEEE_FLOAT);
    auto floatExt = Fmt(48000, 2, 32, WAVE_FORMAT_EXTENSIBLE, true);
    auto unsupportedFloat16 = Fmt(48000, 2, 16, WAVE_FORMAT_IEEE_FLOAT);
    auto unknownGuid = Fmt(48000, 2, 32, WAVE_FORMAT_EXTENSIBLE);
    unknownGuid.SubFormat.bytes[0] = 9;
    relay.Publish(data.data(), (ULONG)data.size(), &floatHost);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &floatHost);
    assert(quiet == data);
    relay.Publish(data.data(), (ULONG)data.size(), &floatExt);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &floatExt);
    assert(quiet == data);
    relay.Publish(data.data(), (ULONG)data.size(), &unsupportedFloat16);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &floatHost);
    Zeros(quiet);
    relay.Publish(data.data(), (ULONG)data.size(), &unknownGuid);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &floatExt);
    Zeros(quiet);
    auto badRate = Fmt();
    badRate.Format.nAvgBytesPerSec = 1;
    relay.Publish(data.data(), (ULONG)data.size(), &badRate);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &badRate);
    Zeros(quiet);

    // Equivalent legacy/EXTENSIBLE wrapper tags MUST not cause silent
    // loopback, but PCM and IEEE float never share the same sample meaning.
    relay.Reset();
    auto pcmLegacy = Fmt(48000, 2, 16, WAVE_FORMAT_PCM);
    auto pcmExtended = Fmt(48000, 2, 16, WAVE_FORMAT_EXTENSIBLE);
    relay.Publish(data.data(), (ULONG)data.size(), &pcmLegacy);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcmExtended);
    assert(quiet == data);
    relay.Publish(data.data(), (ULONG)data.size(), &pcmExtended);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcmLegacy);
    assert(quiet == data);

    auto legacyFloat = Fmt(48000, 2, 32, WAVE_FORMAT_IEEE_FLOAT);
    auto extendedFloat = Fmt(48000, 2, 32, WAVE_FORMAT_EXTENSIBLE, true);
    relay.Publish(data.data(), (ULONG)data.size(), &legacyFloat);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &extendedFloat);
    assert(quiet == data);
    relay.Publish(data.data(), (ULONG)data.size(), &extendedFloat);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &legacyFloat);
    assert(quiet == data);

    auto pcm32 = Fmt(48000, 2, 32, WAVE_FORMAT_EXTENSIBLE);
    relay.Publish(data.data(), (ULONG)data.size(), &pcm32);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &extendedFloat);
    Zeros(quiet); // float32 cannot consume otherwise identical PCM32 bytes.
    relay.Publish(data.data(), (ULONG)data.size(), &extendedFloat);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcm32);
    Zeros(quiet); // reverse incompatible sample encoding.

    auto wrongMask = pcmExtended;
    wrongMask.dwChannelMask = 0x30UL; // surround channels, not FL/FR
    relay.Publish(data.data(), (ULONG)data.size(), &wrongMask);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcmLegacy);
    Zeros(quiet);
    auto valid24In32 = Fmt(48000, 2, 32, WAVE_FORMAT_EXTENSIBLE);
    valid24In32.Samples.wValidBitsPerSample = 24;
    relay.Publish(data.data(), (ULONG)data.size(), &valid24In32);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcm32);
    Zeros(quiet); // 24 significant bits != 32 significant bits.
    relay.Publish(data.data(), (ULONG)data.size(), &valid24In32);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &valid24In32);
    assert(quiet == data);
    auto invalidFloatBits = extendedFloat;
    invalidFloatBits.Samples.wValidBitsPerSample = 24;
    relay.Publish(data.data(), (ULONG)data.size(), &invalidFloatBits);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &legacyFloat);
    Zeros(quiet);
    auto impossibleValid = valid24In32;
    impossibleValid.Samples.wValidBitsPerSample = 40;
    relay.Publish(data.data(), (ULONG)data.size(), &impossibleValid);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &pcm32);
    Zeros(quiet);

    // 20 or 21 extension bytes are invalid even if the host struct
    // happens to have alignment padding. Exactly 22 is a valid extension.
    relay.Reset();
    auto extension = Fmt(48000, 2, 16, WAVE_FORMAT_EXTENSIBLE);
    auto shortExtension = extension;
    shortExtension.Format.cbSize = 21;
    relay.Publish(data.data(), (ULONG)data.size(), &shortExtension);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &extension);
    Zeros(quiet);
    shortExtension.Format.cbSize = 20;
    relay.Publish(data.data(), (ULONG)data.size(), &shortExtension);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &extension);
    Zeros(quiet);
    relay.Publish(data.data(), (ULONG)data.size(), &extension);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &extension);
    assert(quiet == data);

    // A DRM policy revocation destroys the previous PCM epoch.
    // The derived miniport calls Reset() when the listener is absent,
    // copy protection is active or loopback constriction is enabled.
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    relay.Reset();
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);
    relay.Publish(fresh.data(), (ULONG)fresh.size(), &stereo);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    assert(quiet == fresh); // Only newly rendered unprotected PCM survives.

    // Malformed packets (not an integer number of sample-frames) must
    // fail closed rather than shift the next left/right channel boundary.
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    relay.Publish(data.data(), 7, &stereo);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);
    relay.Publish(data.data(), (ULONG)data.size(), &stereo);
    relay.Consume(quiet.data(), 7, &stereo);
    Zeros(quiet); // 7 byte read is unaligned, returned wholly silent.
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet); // malformed read flushed old samples.

    // Invalid formats must not trigger allocations or use unaligned packets.
    auto unsafe = Fmt(48000, 8);
    relay.Publish(data.data(), (ULONG)data.size(), &unsafe);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &unsafe);
    Zeros(quiet);
    assert(g_allocations == 1);

    // Simultaneous WaveRT DPC producers, loopback consumers, and policy/stream
    // resets must preserve WHOLE 24-bit stereo frames and never surface stale
    // garbage after a reset. Reuses actual kernel FIFO code and host spin-lock.
    relay.Reset();
    auto concurrent24 = Fmt(48000, 2, 24, WAVE_FORMAT_EXTENSIBLE);
    std::thread producer([&] {
        for (unsigned i = 0; i < 3000; ++i) {
            std::vector<BYTE> packet(6 * 101, BYTE((i % 250) + 1));
            relay.Publish(packet.data(), (ULONG)packet.size(), &concurrent24);
            if (i % 7 == 0) std::this_thread::yield();
        }
    });
    std::thread consumer([&] {
        for (unsigned i = 0; i < 3000; ++i) {
            std::vector<BYTE> packet(6 * 97, 0xdd);
            relay.Consume(packet.data(), (ULONG)packet.size(), &concurrent24);
            for (std::size_t p = 0; p < packet.size(); p += 6) {
                // Each input frame is homogeneous; any torn frame or bad
                // wraparound causes one of these five comparisons to fail.
                for (unsigned k = 1; k < 6; ++k)
                    assert(packet[p + k] == packet[p]);
            }
            if (i % 11 == 0) std::this_thread::yield();
        }
    });
    std::thread revoker([&] {
        for (unsigned i = 0; i < 600; ++i) {
            relay.Reset();
            if (i % 3 == 0) std::this_thread::yield();
        }
    });
    producer.join();
    consumer.join();
    revoker.join();
    relay.Reset();
    std::vector<BYTE> afterReset(6 * 97, 0xff);
    relay.Consume(afterReset.data(), (ULONG)afterReset.size(), &concurrent24);
    Zeros(afterReset);
    assert(g_allocations == 1); // no callback allocations in kernel FIFO
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);
    relay.Shutdown();
    assert(g_wipes == 1);
    std::cout << "PASS: bounded native relay FIFO, 24-bit frame alignment, 22-byte extensible format, DRM epoch reset, normalized PCM/float wrapper types and concurrent frame integrity, float32, stale-format rejection, wrap, overflow, "
                 "format isolation, reset, and no callback allocation\n";
}
