// LouderMe lab-only virtual render -> WaveRT loopback PCM relay.
// This file is deployed only into a pinned, MS-PL SysVAD source checkout.
// It is NOT signed, installed or shipped from GitHub Actions.
#pragma once

class LouderMeLabPcmRelay final
{
    // Multiple of every permitted mono/stereo PCM frame size: 2, 3, 4, 6, 8.
    // A 65536-byte ring cuts 24-bit stereo frames after wraparound.
    static const ULONG Capacity = 65520; // bounded, nonpaged and frame-aligned
    KSPIN_LOCK m_Lock;
    BYTE* m_Buffer;
    ULONG m_Read;
    ULONG m_Write;
    ULONG m_Count;
    USHORT m_Channels;
    USHORT m_Bits;
    USHORT m_BlockAlign;
    USHORT m_SampleKind;
    USHORT m_ValidBits;
    ULONG m_Rate;
    BOOLEAN m_HaveFormat;

    static BOOLEAN Supported(_In_opt_ const WAVEFORMATEXTENSIBLE* format)
    {
        if (format == NULL || format->Format.nChannels < 1 ||
            format->Format.nChannels > 2 ||
            format->Format.nSamplesPerSec < 8000 ||
            format->Format.nSamplesPerSec > 192000 ||
            format->Format.nBlockAlign == 0 ||
            (format->Format.wBitsPerSample != 16 &&
             format->Format.wBitsPerSample != 24 &&
             format->Format.wBitsPerSample != 32) ||
            format->Format.nBlockAlign !=
                format->Format.nChannels * (format->Format.wBitsPerSample / 8))
            return FALSE;
        // WAVEFORMATEXTENSIBLE adds exactly 22 bytes after WAVEFORMATEX.
        // sizeof(struct)-sizeof(WAVEFORMATEX) is NOT a wire-format size:
        // C/C++ padding can make it 20 or 24 depending on ABI.
        const BOOLEAN extended =
            format->Format.wFormatTag == WAVE_FORMAT_EXTENSIBLE &&
            format->Format.cbSize >= 22;
        const BOOLEAN pcm = format->Format.wFormatTag == WAVE_FORMAT_PCM ||
            (extended && IsEqualGUIDAligned(
                format->SubFormat, KSDATAFORMAT_SUBTYPE_PCM));
        const BOOLEAN float32 =
            (format->Format.wFormatTag == WAVE_FORMAT_IEEE_FLOAT ||
             (extended && IsEqualGUIDAligned(
                 format->SubFormat, KSDATAFORMAT_SUBTYPE_IEEE_FLOAT))) &&
            format->Format.wBitsPerSample == 32;
        // Mono defaults to front-center (0x4); stereo is front left+right
        // (0x3). An alternate channel map cannot be silently reinterpreted.
        if (extended && format->dwChannelMask != 0 &&
            format->dwChannelMask !=
                (format->Format.nChannels == 1 ? 0x4UL : 0x3UL))
            return FALSE;
        // A 24-valid-bit sample in a 32-bit container is supported only
        // when BOTH formats explicitly agree on that nonstandard precision.
        if (extended && format->Samples.wValidBitsPerSample != 0 &&
            (format->Samples.wValidBitsPerSample >
                 format->Format.wBitsPerSample ||
             format->Samples.wValidBitsPerSample < 1))
            return FALSE;
        // Unknown GUIDs never qualify. Refuse impossible byte rates.
        return (pcm || float32) &&
            format->Format.nAvgBytesPerSec ==
                format->Format.nSamplesPerSec * format->Format.nBlockAlign;
    }

    // Normalize only the wrapper representation, never resample or
    // reinterpret a PCM sample as IEEE float. Samples remain byte-identical.
    static USHORT SampleKind(_In_ const WAVEFORMATEXTENSIBLE* format)
    {
        if (format->Format.wFormatTag == WAVE_FORMAT_PCM ||
            (format->Format.wFormatTag == WAVE_FORMAT_EXTENSIBLE &&
             IsEqualGUIDAligned(format->SubFormat, KSDATAFORMAT_SUBTYPE_PCM)))
            return 1;
        return 2; // Supported() already admitted IEEE float32 only.
    }

    static USHORT ValidBits(_In_ const WAVEFORMATEXTENSIBLE* format)
    {
        if (format->Format.wFormatTag == WAVE_FORMAT_EXTENSIBLE &&
            format->Samples.wValidBitsPerSample != 0)
            return format->Samples.wValidBitsPerSample;
        return format->Format.wBitsPerSample;
    }

    BOOLEAN SameFormat(_In_ const WAVEFORMATEXTENSIBLE* format) const
    {
        return m_HaveFormat && m_Channels == format->Format.nChannels &&
               m_Bits == format->Format.wBitsPerSample &&
               m_BlockAlign == format->Format.nBlockAlign &&
               m_Rate == format->Format.nSamplesPerSec &&
               m_SampleKind == SampleKind(format) &&
               m_ValidBits == ValidBits(format);
    }

    VOID Remember(_In_ const WAVEFORMATEXTENSIBLE* format)
    {
        m_Channels = format->Format.nChannels;
        m_Bits = format->Format.wBitsPerSample;
        m_BlockAlign = format->Format.nBlockAlign;
        m_Rate = format->Format.nSamplesPerSec;
        m_SampleKind = SampleKind(format);
        m_ValidBits = ValidBits(format);
        m_HaveFormat = TRUE;
    }

public:
    LouderMeLabPcmRelay() :
        m_Buffer(NULL), m_Read(0), m_Write(0), m_Count(0),
        m_Channels(0), m_Bits(0), m_BlockAlign(0),
        m_SampleKind(0), m_ValidBits(0), m_Rate(0), m_HaveFormat(FALSE)
    {
        KeInitializeSpinLock(&m_Lock);
    }

    _IRQL_requires_max_(PASSIVE_LEVEL)
    NTSTATUS Initialize()
    {
        if (m_Buffer != NULL)
            return STATUS_INVALID_DEVICE_STATE;
        m_Buffer = (BYTE*)ExAllocatePool2(
            POOL_FLAG_NON_PAGED, Capacity, MINWAVERT_POOLTAG);
        if (m_Buffer == NULL)
            return STATUS_INSUFFICIENT_RESOURCES;
        RtlZeroMemory(m_Buffer, Capacity);
        return STATUS_SUCCESS;
    }

    _IRQL_requires_max_(PASSIVE_LEVEL)
    VOID Shutdown()
    {
        if (m_Buffer != NULL)
        {
            RtlSecureZeroMemory(m_Buffer, Capacity);
            ExFreePoolWithTag(m_Buffer, MINWAVERT_POOLTAG);
            m_Buffer = NULL;
        }
    }

    VOID Reset()
    {
        KIRQL oldIrql;
        KeAcquireSpinLock(&m_Lock, &oldIrql);
        m_Read = m_Write = m_Count = 0;
        m_HaveFormat = FALSE;
        KeReleaseSpinLock(&m_Lock, oldIrql);
    }

    // Publish bytes actually consumed from the system-render DMA, rather than
    // bytes merely queued by a user process. No allocations or file IO at DPC.
    VOID Publish(
        _In_reads_bytes_(bytes) const BYTE* data,
        _In_ ULONG bytes,
        _In_ const WAVEFORMATEXTENSIBLE* format)
    {
        if (m_Buffer == NULL || data == NULL || bytes == 0)
            return;
        if (!Supported(format) ||
            bytes % format->Format.nBlockAlign != 0)
        {
            // Unaligned DMA packet would irreversibly shift sample boundaries.
            Reset();
            return;
        }
        if (bytes > Capacity)
        {
            data += bytes - Capacity;
            bytes = Capacity;
        }
        KIRQL oldIrql;
        KeAcquireSpinLock(&m_Lock, &oldIrql);
        if (!SameFormat(format))
        {
            m_Read = m_Write = m_Count = 0;
            Remember(format);
        }
        // Never deliver stale overwritten content. Older unconsumed PCM is
        // dropped if the capture stream has fallen behind.
        ULONG discard = bytes > Capacity - m_Count ?
            bytes - (Capacity - m_Count) : 0;
        if (discard != 0)
        {
            m_Read = (m_Read + discard) % Capacity;
            m_Count -= discard;
        }
        ULONG first = min(bytes, Capacity - m_Write);
        RtlCopyMemory(m_Buffer + m_Write, data, first);
        if (bytes > first)
            RtlCopyMemory(m_Buffer, data + first, bytes - first);
        m_Write = (m_Write + bytes) % Capacity;
        m_Count += bytes;
        KeReleaseSpinLock(&m_Lock, oldIrql);
    }

    // Capture stream receives silence when source is absent or incompatible.
    // Never revive synthetic SysVAD 440-Hz tones, even on ring underrun.
    VOID Consume(
        _Out_writes_bytes_(bytes) BYTE* output,
        _In_ ULONG bytes,
        _In_ const WAVEFORMATEXTENSIBLE* format)
    {
        if (output == NULL || bytes == 0)
            return;
        RtlZeroMemory(output, bytes);
        if (m_Buffer == NULL)
            return;
        if (!Supported(format) ||
            bytes % format->Format.nBlockAlign != 0)
        {
            Reset();
            return;
        }
        KIRQL oldIrql;
        KeAcquireSpinLock(&m_Lock, &oldIrql);
        if (!SameFormat(format))
        {
            // Do not release old frames if render/capture negotiated
            // incompatible formats or if capture changes mid-session.
            m_Read = m_Write = m_Count = 0;
            m_HaveFormat = FALSE;
        }
        else
        {
            ULONG take = min(bytes, m_Count);
            ULONG first = min(take, Capacity - m_Read);
            if (first > 0)
                RtlCopyMemory(output, m_Buffer + m_Read, first);
            if (take > first)
                RtlCopyMemory(output + first, m_Buffer, take - first);
            m_Read = (m_Read + take) % Capacity;
            m_Count -= take;
        }
        KeReleaseSpinLock(&m_Lock, oldIrql);
    }
};
