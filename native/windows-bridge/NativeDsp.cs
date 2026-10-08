using System.Runtime.InteropServices;

namespace LouderMeAudioBridge;

internal sealed class NativeDsp : IDisposable
{
    private IntPtr _handle;

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint louderme_abi_version();

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr louderme_engine_create(int sampleRate, int channels);

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern void louderme_engine_destroy(IntPtr handle);

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern int louderme_engine_set_boost(IntPtr handle, int enabled, int percent);

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern int louderme_engine_set_eq(IntPtr handle, int enabled, float[] gains);

    [DllImport("louderme_audio_c", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe int louderme_engine_process(
        IntPtr handle, float* pcm, nuint frames);

    public NativeDsp(int rate, int channels, int boostPercent)
    {
        if (louderme_abi_version() != 1)
            throw new NotSupportedException("Native DSP ABI mismatch.");
        _handle = louderme_engine_create(rate, channels);
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Native DSP engine creation failed.");
        try
        {
            if (louderme_engine_set_boost(_handle, 1, boostPercent) != 0 ||
                louderme_engine_set_eq(_handle, 0, new float[7]) != 0)
                throw new InvalidOperationException("Native DSP parameter setup failed.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void ConfigureEqualizer(bool enabled, float[] gainsDb)
    {
        if (_handle == IntPtr.Zero) throw new ObjectDisposedException(nameof(NativeDsp));
        if (gainsDb.Length != 7 || gainsDb.Any(g => !float.IsFinite(g) || g < -10 || g > 10))
            throw new ArgumentException("EQ needs exactly seven finite gains in [-10,+10] dB.");
        if (louderme_engine_set_eq(_handle, enabled ? 1 : 0, gainsDb) != 0)
            throw new InvalidOperationException("Native EQ configuration failed.");
    }

    // Must be called from the capture audio callback; no setters on other threads.
    public unsafe void Process(byte[] pcm, int byteCount, int channelCount)
    {
        if (_handle == IntPtr.Zero)
            throw new ObjectDisposedException(nameof(NativeDsp));
        if (byteCount < 0 || byteCount > pcm.Length ||
            byteCount % (sizeof(float) * channelCount) != 0)
            throw new ArgumentException("Invalid interleaved float PCM buffer.");
        fixed (byte* bytes = pcm)
        {
            if (louderme_engine_process(_handle, (float*)bytes,
                (nuint)(byteCount / (sizeof(float) * channelCount))) != 0)
                throw new InvalidOperationException("Native audio processing failed.");
        }
    }

    public void Dispose()
    {
        var handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
        if (handle != IntPtr.Zero) louderme_engine_destroy(handle);
        GC.SuppressFinalize(this);
    }
}
