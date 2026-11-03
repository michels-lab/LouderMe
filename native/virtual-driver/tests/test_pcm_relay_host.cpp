// Host-side numerical tests for the SAME header compiled by WDK in SysVAD.
// Minimal kernel API stubs; no audio devices, kernel privileges or installation.
#include <algorithm>
#include <array>
#include <cassert>
#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <vector>
#include <iostream>
using BYTE = std::uint8_t;
using ULONG = std::uint32_t;
using USHORT = std::uint16_t;
using BOOLEAN = int;
using NTSTATUS = std::int32_t;
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
    USHORT reserved = 0;
    ULONG channelMask = 0;
    GUID SubFormat;
};
static constexpr int TRUE = 1, FALSE = 0;
static constexpr USHORT WAVE_FORMAT_PCM = 1, WAVE_FORMAT_EXTENSIBLE = 65534;
static constexpr ULONG POOL_FLAG_NON_PAGED = 1, MINWAVERT_POOLTAG = 0x4C4D5043;
static constexpr NTSTATUS STATUS_SUCCESS = 0, STATUS_INVALID_DEVICE_STATE = -1,
    STATUS_INSUFFICIENT_RESOURCES = -2;
static unsigned g_allocations = 0, g_wipes = 0;
#define _In_
#define _In_opt_
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
                                USHORT bits = 16, USHORT tag = WAVE_FORMAT_PCM) {
    WAVEFORMATEXTENSIBLE f{};
    f.Format.nSamplesPerSec = rate;
    f.Format.nChannels = channels;
    f.Format.wBitsPerSample = bits;
    f.Format.nBlockAlign = channels * bits / 8;
    f.Format.wFormatTag = tag;
    f.Format.nAvgBytesPerSec = rate * f.Format.nBlockAlign;
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
    otherSubtype.SubFormat.bytes[0] = 1;
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
    assert(quiet == data);

    // A full circular-buffer overwrite keeps the newest 65536 bytes.
    relay.Reset();
    std::vector<BYTE> longSource(66536);
    for (std::size_t i = 0; i < longSource.size(); ++i)
        longSource[i] = BYTE(i % 251);
    relay.Publish(longSource.data(), (ULONG)longSource.size(), &stereo);
    std::vector<BYTE> complete(65536);
    relay.Consume(complete.data(), (ULONG)complete.size(), &stereo);
    assert(std::equal(complete.begin(), complete.end(), longSource.end() - 65536));
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
    assert(quiet == fresh);

    // Invalid formats must not trigger allocations or use unaligned packets.
    auto unsafe = Fmt(48000, 8);
    relay.Publish(data.data(), (ULONG)data.size(), &unsafe);
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &unsafe);
    Zeros(quiet);
    assert(g_allocations == 1);
    relay.Reset();
    relay.Consume(quiet.data(), (ULONG)quiet.size(), &stereo);
    Zeros(quiet);
    relay.Shutdown();
    assert(g_wipes == 1);
    std::cout << "PASS: bounded native relay FIFO, silence, wrap, overflow, "
                 "format isolation, reset, and no callback allocation\n";
}
