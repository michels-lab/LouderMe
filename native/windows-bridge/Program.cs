using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace LouderMeAudioBridge;

internal static class Program
{
    // IEEE 754 floating PCM subtype in WAVEFORMATEXTENSIBLE.
    private static readonly Guid Float32Subtype =
        new("00000003-0000-0010-8000-00aa00389b71");

    private static bool IsFloat32(WaveFormat format) =>
        format.BitsPerSample == 32 &&
        (format.Encoding == WaveFormatEncoding.IeeeFloat ||
         (format is WaveFormatExtensible extended && extended.SubFormat == Float32Subtype));

    private static int Main(string[] args)
    {
        // Explicit opt-in only. Probe runs never retry: repeating a tone
        // invalidates their bounded four-second physical-route evidence.
        bool retry = args.Contains("--auto-reconnect") &&
                     args.Contains("--confirm-experimental") &&
                     !args.Contains("--probe-route") &&
                     !args.Contains("--probe-app-route") &&
                     !args.Contains("--probe-virtual");
        int attempts = retry ? 3 : 1;
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            int exit = RunSingleSession(args);
            if (exit != 75 || attempt == attempts) return exit;
            Console.Error.WriteLine(
                $"Experimental route interrupted. Same endpoint IDs only; " +
                $"retry {attempt}/{attempts - 1} after 2 seconds.");
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
        return 75;
    }

    private static int RunSingleSession(string[] args)
    {
        try
        {
            if (args.Contains("--check-native"))
                return CheckNative();
            if (args.Contains("--check-pipeline"))
                return CheckPipeline();
            if (args.Contains("--check-loopback"))
            {
                LoopbackToneVerifier.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-route-tone"))
            {
                PhysicalRouteTone.RunSelfTests();
                PhysicalRouteStimulus.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-timing"))
            {
                AdaptiveBufferClock.RunSelfTests();
                AudioProcessingTelemetry.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-recovery"))
            {
                AudioQueueRecovery.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-controls"))
            {
                DspControlMailbox.RunSelfTests();
                DspLiveConsoleControls.RunSelfTests();
                return 0;
            }
            if (args.Contains("--check-formats"))
            {
                PcmFormatAdapter.RunSelfTests();
                return 0;
            }

            using var enumerator = new MMDeviceEnumerator();
            if (args.Contains("--default-render-fingerprint"))
            {
                // Read-only metadata-only preflight for an isolated VM. Never
                // print the raw WASAPI endpoint ID or select/change defaults.
                // Installation can implicitly alter the Windows default even
                // when our tools never call a SetDefaultDevice API.
                if (args.Length != 1)
                    throw new ArgumentException("Default-route preflight accepts no other arguments.");
                // Windows tracks three independent render-default roles.
                // PnP may alter only Console or Communications while leaving
                // Multimedia alone. Fingerprint the ordered tuple, never
                // disclose any raw endpoint ID or friendly name.
                var identity = new StringBuilder();
                foreach (var role in new[] { Role.Console, Role.Multimedia, Role.Communications })
                {
                    var endpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, role);
                    if (endpoint is null || endpoint.State != DeviceState.Active ||
                        string.IsNullOrWhiteSpace(endpoint.ID))
                        throw new InvalidOperationException(
                            "Missing active Windows default output for at least one render role.");
                    identity.Append(role).Append(':').Append(endpoint.ID.Length)
                        .Append(':').Append(endpoint.ID).Append('\n');
                }
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToString()));
                Console.WriteLine($"DEFAULT_RENDER_SHA256={Convert.ToHexString(hash).ToLowerInvariant()}");
                return 0;
            }
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            if (args.Contains("--list-devices"))
            {
                foreach (var device in devices)
                    Console.WriteLine($"[{device.FriendlyName}] {device.ID}");
                return 0;
            }

            if (args.Contains("--probe-virtual"))
                return ProbeVirtualEndpoint(devices, args);

            // Never install a driver or change Windows' default endpoint.
            var sourceId = GetArg(args, "--source-id");
            var outputId = GetArg(args, "--output-id");
            var percentText = GetArg(args, "--boost");
            bool proveExternalAppRoute = args.Contains("--probe-app-route");
            bool ownTone = args.Contains("--probe-route");
            bool provePhysicalRoute = ownTone || proveExternalAppRoute;
            if (ownTone && proveExternalAppRoute)
                throw new ArgumentException("Select exactly one source stimulus for lab proof.");
            if (!args.Contains("--confirm-experimental") ||
                string.IsNullOrWhiteSpace(sourceId) ||
                string.IsNullOrWhiteSpace(outputId) ||
                !int.TryParse(percentText, out var boost) || boost is < 100 or > 250)
            {
                Console.Error.WriteLine(
                    "Experimental only. Use --list-devices, then " +
                    "--source-id <LOUDERME_VIRTUAL_ENDPOINT_ID> --output-id <PHYSICAL_ENDPOINT_ID> " +
                    "--boost <100-250> [--eq-db b1,b2,b3,b4,b5,b6,b7] --confirm-experimental");
                return 2;
            }

            if (sourceId == outputId)
                throw new InvalidOperationException("Capture and physical output endpoints must differ.");
            // This probe intentionally emits audible sound through the chosen
            // physical sink. Keep lab signal quiet even if product boost
            // normally allows 250%. No default-device modifications.
            if (provePhysicalRoute && boost is < 100 or > 150)
                throw new ArgumentException(
                    "Physical-route lab proof requires --boost 100..150 to limit test output.");
            if (provePhysicalRoute && args.Contains("--interactive-controls"))
                throw new ArgumentException(
                    "Physical-route tone evidence forbids live control changes during measurement.");
            if (args.Contains("--interactive-controls") && args.Contains("--auto-reconnect"))
                throw new ArgumentException(
                    "Live stdin controls cannot safely survive experimental reconnect; " +
                    "start a new confirmed session after a route interruption.");

            // Never fall back to the new Windows default device after a
            // Bluetooth/USB/HDMI hot-swap. Retry ONLY the exact supplied IDs.
            var source = devices.FirstOrDefault(x => x.ID == sourceId)
                ?? throw new AudioRouteRecoverableException(
                    "Selected virtual source ID is unavailable; no alternate device selected.");
            var physical = devices.FirstOrDefault(x => x.ID == outputId)
                ?? throw new AudioRouteRecoverableException(
                    "Selected physical output ID is unavailable; no alternate device selected.");

            // Fail closed: capturing arbitrary physical output would cause doubled sound/feedback.
            if (!source.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase) ||
                 physical.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The source must be an explicitly selected LouderMe Virtual output, " +
                    "and the target must be a separate physical playback device.");

            using var capture = new WasapiLoopbackCapture(source);
            var inputFormat = capture.WaveFormat;
            var outputFormat = physical.AudioClient.MixFormat;
            // Decode to native float32 and adapt the processed stream to the
            // actual physical mix format; explicit mono/stereo mapping is
            // supported. Multichannel conversion still fails closed.
            var adapter = new PcmFormatAdapter(inputFormat, outputFormat);
            var clockController = new AdaptiveBufferClock();
            var processingTelemetry = new AudioProcessingTelemetry();
            if (provePhysicalRoute && inputFormat.Channels is < 1 or > 2)
                throw new NotSupportedException(
                    "Physical-route lab proof requires a mono or stereo virtual mix.");

            using var engine = new NativeDsp(
                inputFormat.SampleRate, inputFormat.Channels, boost);
            float[] eqGains = new float[7];
            var eqText = GetArg(args, "--eq-db");
            if (eqText is not null)
            {
                var parts = eqText.Split(',');
                if (parts.Length != 7)
                    throw new ArgumentException("Expected seven comma-separated EQ dB values.");
                for (var i = 0; i < 7; i++)
                {
                    if (!float.TryParse(parts[i],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out eqGains[i]) || !float.IsFinite(eqGains[i]) ||
                        eqGains[i] is < -10 or > 10)
                        throw new ArgumentException("EQ gains must be finite values from -10 to +10 dB.");
                }
            }
            engine.ConfigureEqualizer(eqText is not null, eqGains);
            var liveControlQueue = new DspControlMailbox();
            using var playback = new WasapiOut(physical, AudioClientShareMode.Shared, true, 75);
            var buffer = new BufferedWaveProvider(outputFormat, TimeSpan.FromMilliseconds(250))
            {
                DiscardOnBufferOverflow = false,
                ReadFully = true
            };
            var playbackReader = new MonitoredRenderBuffer(buffer);
            var queueRecovery = new AudioQueueRecovery();
            playback.Init(playbackReader);

            // Optional LAB-only end-to-end observable. A second WASAPI
            // loopback reads the *physical* playback endpoint, never an
            // arbitrary microphone or remote stream.
            using var monitor = provePhysicalRoute
                ? new WasapiLoopbackCapture(physical) : null;
            using var toneStream = ownTone
                ? new RawSourceWaveStream(
                    new MemoryStream(PhysicalRouteTone.Create(inputFormat), writable: false),
                    inputFormat) : null;
            using var sourceToneOutput = ownTone
                ? new WasapiOut(source, AudioClientShareMode.Shared, true, 75) : null;
            var monitorGate = new object();
            LoopbackToneVerifier? monitorVerifier = null;
            PcmFormatAdapter? monitorDecoder = null;
            Exception? monitorError = null;
            if (provePhysicalRoute)
            {
                if (monitor is null || (ownTone &&
                    (toneStream is null || sourceToneOutput is null)))
                    throw new InvalidOperationException("Lab physical-route initialization failed.");
                var recordedFormat = monitor.WaveFormat;
                if (!PcmFormatAdapter.IsSupported(recordedFormat) ||
                    recordedFormat.Channels is < 1 or > 2)
                    throw new NotSupportedException(
                        "Physical LAB monitor requires PCM16/float32 mono or stereo.");
                var floatMonitor = WaveFormat.CreateIeeeFloatWaveFormat(
                    recordedFormat.SampleRate, recordedFormat.Channels);
                monitorDecoder = new PcmFormatAdapter(recordedFormat, floatMonitor);
                monitorVerifier = new LoopbackToneVerifier(floatMonitor);
                monitor.RecordingStopped += (_, e) =>
                {
                    if (e.Exception is not null)
                        Interlocked.CompareExchange(ref monitorError, e.Exception, null);
                };
                monitor.DataAvailable += (_, e) =>
                {
                    lock (monitorGate)
                    {
                        try
                        {
                            if (e.BytesRecorded > 0)
                            {
                                byte[] samples = monitorDecoder.Decode(e.Buffer, e.BytesRecorded);
                                monitorVerifier.Add(samples);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref monitorError, ex, null);
                        }
                    }
                };
                // External-app mode intentionally has NO owned toneStream or
                // tone WasapiOut. The old unconditional Init(null) aborted
                // every independent-source proof before EXTERNAL_SOURCE_READY.
                PhysicalRouteStimulus.Initialize(
                    ownTone, proveExternalAppRoute,
                    initializeOwnTone: () =>
                    {
                        if (sourceToneOutput is null || toneStream is null)
                            throw new InvalidOperationException(
                                "Internal tone mode is missing its sound source.");
                        sourceToneOutput.Init(toneStream);
                    });
            }

            long processedFrames = 0;
            long outputFrames = 0;
            long droppedFrames = 0;
            long flushedOutputFrames = 0;
            long queueResets = 0;
            long overflowPackets = 0;
            var recordingStopped = new ManualResetEventSlim(false);
            Exception? recordingError = null;
            Exception? callbackError = null;
            capture.RecordingStopped += (_, e) =>
            {
                recordingError = e.Exception;
                recordingStopped.Set();
            };
            capture.DataAvailable += (_, e) =>
            {
                if (recordingStopped.IsSet) return;
                long processingStart = Stopwatch.GetTimestamp();
                try
                {
                    if (e.BytesRecorded == 0) return;
                    // Never mutate NAudio's recycled event buffer. Native DSP
                    // receives an independent float32 copy even for PCM16 input.
                    byte[] decoded = adapter.Decode(e.Buffer, e.BytesRecorded);
                    // The ONLY reader and native-state writer is the PCM
                    // callback. The control worker publishes immutable data.
                    liveControlQueue.ApplyPending(engine);
                    engine.Process(decoded, decoded.Length, inputFormat.Channels);
                    byte[] converted = adapter.Encode(decoded, decoded.Length);
                    if (converted.Length == 0) return;
                    var sourceFrames = e.BytesRecorded / inputFormat.BlockAlign;
                    if (converted.Length > buffer.BufferLength)
                        throw new InvalidDataException(
                            "One PCM packet is larger than the entire output queue.");
                    if (buffer.BufferLength - buffer.BufferedBytes < converted.Length)
                    {
                        Interlocked.Increment(ref overflowPackets);
                        switch (queueRecovery.OnOverflow(Stopwatch.GetTimestamp()))
                        {
                            case AudioQueueRecovery.Action.DropNewest:
                                Interlocked.Add(ref droppedFrames, sourceFrames);
                                return;
                            case AudioQueueRecovery.Action.FlushStaleAndAccept:
                                // Discard stale PCM only after repeated loss,
                                // never let a full queue delay sound forever.
                                // Flushes are bounded; further storms stop
                                // the session and optionally reconnect.
                                Interlocked.Add(ref flushedOutputFrames,
                                    buffer.BufferedBytes / outputFormat.BlockAlign);
                                buffer.ClearBuffer();
                                Interlocked.Increment(ref queueResets);
                                break;
                            case AudioQueueRecovery.Action.AbortRoute:
                                throw new AudioRouteRecoverableException(
                                    "Output queue overflow persisted despite bounded resets.");
                        }
                    }
                    buffer.AddSamples(converted, 0, converted.Length);
                    queueRecovery.OnHealthyWrite();
                    Interlocked.Add(ref processedFrames, sourceFrames);
                    Interlocked.Add(ref outputFrames, converted.Length / outputFormat.BlockAlign);
                }
                catch (Exception error)
                {
                    // Managed exceptions must not escape a WASAPI callback;
                    // fail closed on the console thread and release endpoints.
                    Interlocked.CompareExchange(ref callbackError, error, null);
                    recordingStopped.Set();
                }
                finally
                {
                    // This is actual callback work time, not an acoustic
                    // end-to-end latency measurement or total process CPU.
                    processingTelemetry.Record(
                        Stopwatch.GetTimestamp() - processingStart,
                        e.BytesRecorded / inputFormat.BlockAlign,
                        inputFormat.SampleRate);
                }
            };

            using var shutdown = new ManualResetEventSlim(false);
            ConsoleCancelEventHandler cancel = (_, e) =>
            {
                e.Cancel = true;
                shutdown.Set();
            };
            Console.CancelKeyPress += cancel;
            try
            {
                Console.WriteLine("Experimental routing. System default device is NOT changed.");
                Console.WriteLine($"Source: {source.FriendlyName}");
                Console.WriteLine($"Output: {physical.FriendlyName}");
                Console.WriteLine($"DSP: {boost}% gain, own C++ engine, EQ " +
                    (eqText is null ? "disabled" : $"enabled ({eqText} dB)"));
                Console.WriteLine($"Formats: virtual {inputFormat}; physical {outputFormat}");
                Console.WriteLine("PCM16/float32 conversion + linear rate adaptation; matching channels or mono/stereo map.");
                Console.WriteLine("Press Ctrl+C to stop; this is not a shipped system driver.");
                if (args.Contains("--interactive-controls"))
                {
                    var consoleControls = new DspLiveConsoleControls(
                        liveControlQueue, boost, eqText is not null, eqGains);
                    // Background reader may block on Console.ReadLine; it never
                    // changes the native engine or device routing directly.
                    var controlReader = new Thread(() =>
                    {
                        try
                        {
                            string? command;
                            while ((command = Console.ReadLine()) is not null)
                            {
                                consoleControls.TryHandle(command, out var status);
                                Console.WriteLine(status);
                            }
                        }
                        catch (IOException) { /* unavailable stdin: lab controls disabled */ }
                    }) { IsBackground = true, Name = "LouderMe-Lab-Controls" };
                    controlReader.Start();
                    Console.WriteLine("LAB live controls: boost 100..250 | eq off | eq seven comma-separated dB values");
                    Console.WriteLine("Changes are applied on the next capture callback; no UI/console thread touches DSP.");
                }
                if (provePhysicalRoute)
                    Console.WriteLine(ownTone
                        ? "LAB: internally generated 2-second 440 Hz tone; physical loopback, 4-second window."
                        : "LAB: awaiting 2-second 440 Hz from SEPARATE Windows application. " +
                          "Independent source app must start after EXTERNAL_SOURCE_READY; " +
                          "physical monitor has a 5-second total measurement window.");

                playback.Play();
                capture.StartRecording();
                monitor?.StartRecording();
                sourceToneOutput?.Play();
                // Flush readiness only AFTER both WASAPI captures are active.
                // The disposable VM runner starts the separate process only
                // after seeing this line; bridge NEVER synthesizes its tone.
                if (proveExternalAppRoute)
                    Console.WriteLine("EXTERNAL_SOURCE_READY");
                var routeStarted = Stopwatch.GetTimestamp();
                long lastStats = routeStarted;
                long lastObservedQueueResets = 0;
                while (!shutdown.Wait(250))
                {
                    if (monitorError is not null)
                        throw new IOException("Physical PCM loopback observer failed.", monitorError);
                    if (provePhysicalRoute &&
                        Stopwatch.GetElapsedTime(routeStarted) >=
                            TimeSpan.FromSeconds(proveExternalAppRoute ? 5 : 4))
                        break;
                    if (recordingStopped.IsSet)
                    {
                        if (callbackError is AudioRouteRecoverableException recoverable)
                            throw recoverable;
                        if (callbackError is not null)
                            throw new IOException("PCM bridge callback failed.", callbackError);
                        throw new AudioRouteRecoverableException(
                            "Virtual capture interrupted: " +
                            (recordingError?.Message ?? "capture stopped without error."));
                    }
                    if (playback.PlaybackState != PlaybackState.Playing)
                        throw new AudioRouteRecoverableException(
                            "Selected physical audio playback stopped.");
                    try
                    {
                        if (source.State != DeviceState.Active ||
                            physical.State != DeviceState.Active)
                            throw new AudioRouteRecoverableException(
                                "Selected source or physical output was deactivated/hot-unplugged.");
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        throw new AudioRouteRecoverableException(
                            "Windows invalidated one of the selected audio endpoints.");
                    }
                    // A render device's real clock can run tens/hundreds of
                    // ppm faster/slower than the capture endpoint. Steer the
                    // output frame count gradually based on queue occupancy.
                    // Allow startup to settle before applying feedback.
                    double queuedMs = buffer.BufferedDuration.TotalMilliseconds;
                    double correctionPpm = 0;
                    long currentResets = Interlocked.Read(ref queueResets);
                    if (currentResets != lastObservedQueueResets)
                    {
                        clockController.Reset();
                        adapter.SetRateAdjustmentPpm(0);
                        lastObservedQueueResets = currentResets;
                    }
                    if (Interlocked.Read(ref processedFrames) >=
                        2L * inputFormat.SampleRate)
                    {
                        correctionPpm = clockController.Update(queuedMs);
                        adapter.SetRateAdjustmentPpm(correctionPpm);
                    }
                    if (Stopwatch.GetElapsedTime(lastStats) >= TimeSpan.FromSeconds(1))
                    {
                        var timing = processingTelemetry.Read();
                        Console.WriteLine(
                            $"sourceFrames={Interlocked.Read(ref processedFrames)} " +
                            $"queuedPhysicalFrames={Interlocked.Read(ref outputFrames)} " +
                            $"renderConsumedFrames={playbackReader.PayloadFramesRead} " +
                            $"droppedSourceFrames={Interlocked.Read(ref droppedFrames)} " +
                            $"queuedAudio={queuedMs:F1}ms (NOT acoustic latency) " +
                            $"clockCorrection={correctionPpm:+0.0;-0.0;0.0}ppm " +
                            $"callbackWorkToAudio={timing.WorkToAudioPercent:F1}% " +
                            $"worstCallback={timing.WorstWorkMs:F2}ms " +
                            $"overBudgetCallbacks={timing.OverBudgetChunks} " +
                            $"starvedRenderReads={playbackReader.ReadsWithInsufficientPcm} " +
                            $"overflows={Interlocked.Read(ref overflowPackets)} " +
                            $"queueResets={currentResets} " +
                            $"flushedOutputFrames={Interlocked.Read(ref flushedOutputFrames)} " +
                            $"liveCommandsPublished={liveControlQueue.Published} " +
                            $"liveCommandsApplied={liveControlQueue.Applied} " +
                            $"liveCommandsSuperseded={liveControlQueue.Superseded}");
                        if (queuedMs > 160)
                            Console.Error.WriteLine(
                                "WARNING: experimental render queue above 160 ms; " +
                                "clock compensation may be insufficient.");
                        lastStats = Stopwatch.GetTimestamp();
                    }
                }
            }
            finally
            {
                Console.CancelKeyPress -= cancel;
                sourceToneOutput?.Stop();
                monitor?.StopRecording();
                capture.StopRecording();
                recordingStopped.Wait(TimeSpan.FromSeconds(5));
                playback.Stop();
            }
            if (provePhysicalRoute)
            {
                if (monitorError is not null)
                    throw new IOException("Physical LAB observer failed.", monitorError);
                LoopbackToneVerifier.ToneEvidence physicalProof;
                lock (monitorGate)
                    physicalProof = monitorVerifier!.Evaluate();
                Console.WriteLine(
                    $"Physical LAB proof: frames={physicalProof.Frames} " +
                    $"RMS={physicalProof.Rms:F5} peak={physicalProof.Peak:F5} " +
                    $"matched440={physicalProof.Matched440Amplitude:F5} " +
                    $"coherence={physicalProof.Coherence:F3} " +
                    $"virtualProcessed={Interlocked.Read(ref processedFrames)} " +
                    $"physicalQueued={Interlocked.Read(ref outputFrames)} " +
                    $"renderConsumed={playbackReader.PayloadFramesRead} " +
                    $"starvedRenderReads={playbackReader.ReadsWithInsufficientPcm} " +
                    $"dropped={Interlocked.Read(ref droppedFrames)}");
                if (!physicalProof.Passed ||
                    Interlocked.Read(ref processedFrames) < inputFormat.SampleRate ||
                    playbackReader.PayloadFramesRead < outputFormat.SampleRate ||
                    playbackReader.PayloadFramesRead > Interlocked.Read(ref outputFrames) ||
                    Interlocked.Read(ref droppedFrames) != 0 ||
                    Interlocked.Read(ref queueResets) != 0)
                {
                    Console.Error.WriteLine(
                        "FAIL: no matching lab tone reached the physical render loopback " +
                        "through the bridge, real WASAPI render consumed under a second of " +
                        "queued PCM, or the route reported inconsistent source frames. " +
                        "No native global sound proof.");
                    return 4;
                }
                Console.WriteLine(proveExternalAppRoute
                    ? "PASS: LAB independent source Windows process -> selected virtual endpoint -> " +
                      "own C++ DSP -> separate physical WASAPI loopback 440 Hz identity. " +
                      "NOT general third-party/default-app audio proof or acoustic loudness."
                    : "PASS: isolated LAB 440 Hz virtual→native DSP→physical loopback route. " +
                      "This does NOT certify sound pressure, end-user audibility, " +
                      "other apps' routing, gain calibration or driver installation.");
            }
            Console.WriteLine("Audio bridge stopped and endpoints released.");
            return 0;
        }
        catch (AudioRouteRecoverableException ex)
        {
            Console.Error.WriteLine("Recoverable LAB route interruption: " + ex.Message);
            return 75; // outer loop may retry, strictly bounded/opt-in
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static int ProbeVirtualEndpoint(
        IEnumerable<MMDevice> devices, string[] args)
    {
        // Sound proof only: locally generate a low-amplitude test tone on a
        // specified virtual endpoint and measure its loopback. Does not alter
        // the default playback device or play to a physical output.
        if (!args.Contains("--confirm-experimental"))
            throw new ArgumentException("Probe requires explicit --confirm-experimental.");
        var sourceId = GetArg(args, "--source-id")
            ?? throw new ArgumentException("Probe requires --source-id.");
        var endpoint = devices.FirstOrDefault(d => d.ID == sourceId)
            ?? throw new InvalidOperationException("Selected endpoint not available.");
        if (!endpoint.FriendlyName.Contains("LouderMe Virtual", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Probe refuses non-LouderME source endpoints.");

        using var capture = new WasapiLoopbackCapture(endpoint);
        var format = capture.WaveFormat;
        if (!IsFloat32(format) || format.Channels is < 1 or > 8 ||
            format.SampleRate is < 8000 or > 192000)
            throw new NotSupportedException("Probe needs a valid float32 virtual mix format.");
        var frames = format.SampleRate * 2;
        var audio = new byte[checked(frames * format.BlockAlign)];
        for (var frame = 0; frame < frames; frame++)
        {
            var value = 0.025f * MathF.Sin(
                2 * MathF.PI * 440f * frame / format.SampleRate);
            for (var ch = 0; ch < format.Channels; ch++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    audio.AsSpan((frame * format.Channels + ch) * sizeof(float)), value);
        }

        // A nonzero stream alone can be unrelated system audio or DC noise.
        // Prove that captured PCM has the 440 Hz identity of OUR known tone,
        // with continuous phase across callback packets and unknown start lag.
        var verifier = new LoopbackToneVerifier(format);
        var sync = new object();
        Exception? callbackError = null;
        Exception? captureError = null;
        capture.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null) captureError = e.Exception;
        };
        capture.DataAvailable += (_, e) =>
        {
            lock (sync)
            {
                try
                {
                    verifier.Add(e.Buffer.AsSpan(0, e.BytesRecorded));
                }
                catch (Exception ex)
                {
                    // NAudio's event thread must not throw and bring down
                    // the lab host. Report and fail closed on our main thread.
                    callbackError ??= ex;
                }
            }
        };
        using var playback = new WasapiOut(endpoint, AudioClientShareMode.Shared, true, 75);
        using var tone = new RawSourceWaveStream(new MemoryStream(audio, writable: false), format);
        playback.Init(tone);
        try
        {
            capture.StartRecording();
            playback.Play();
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
        finally
        {
            playback.Stop();
            capture.StopRecording();
            Thread.Sleep(150); // permit last loopback callback to complete
        }
        if (captureError is not null)
            throw new IOException("Virtual endpoint capture stopped with an error.", captureError);
        if (callbackError is not null)
            throw new IOException("Invalid virtual endpoint PCM packet.", callbackError);
        LoopbackToneVerifier.ToneEvidence evidence;
        lock (sync) evidence = verifier.Evaluate();
        Console.WriteLine(
            $"Virtual PCM proof: frames={evidence.Frames} RMS={evidence.Rms:F5} " +
            $"peak={evidence.Peak:F5} matched440={evidence.Matched440Amplitude:F5} " +
            $"coherence={evidence.Coherence:F3}");
        if (!evidence.Passed)
        {
            Console.Error.WriteLine(
                "FAIL: no matching 440 Hz tone in the virtual endpoint loopback. " +
                "Other non-silent audio/DC does not qualify as proof.");
            return 3;
        }
        Console.WriteLine(
            "PASS: loopback PCM contained the specific generated 440 Hz tone " +
            "(frequency-matched, phase-insensitive). " +
            "This proves only the isolated LAB virtual endpoint signal. " +
            "Physical playback and other applications remain unverified.");
        return 0;
    }

    private static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i < 0 || i >= args.Length - 1 ? null : args[i + 1];
    }

    // In-memory, hardware-free proof that real PCM16 packets can flow through
    // our native C++ DSP into the physical output's independent float32 rate.
    // This does NOT prove that an installed virtual driver supplies OS PCM.
    private static int CheckPipeline()
    {
        const int sourceRate = 44100;
        const int outputRate = 48000;
        const int channels = 2;
        const int framesPerPacket = 441;
        const int packetCount = 10;
        var adapter = new PcmFormatAdapter(
            new WaveFormat(sourceRate, 16, channels),
            WaveFormat.CreateIeeeFloatWaveFormat(outputRate, channels));
        using var engine = new NativeDsp(sourceRate, channels, 150);
        long framesOut = 0;
        double power = 0;
        long measuredSamples = 0;
        for (int packet = 0; packet < packetCount; packet++)
        {
            var input = new byte[framesPerPacket * channels * sizeof(short)];
            for (int i = 0; i < framesPerPacket; i++)
            {
                var frame = packet * framesPerPacket + i;
                var sample = (short)MathF.Round(0.1f * 32767f *
                    MathF.Sin(2f * MathF.PI * 440f * frame / sourceRate));
                for (int channel = 0; channel < channels; channel++)
                    BinaryPrimitives.WriteInt16LittleEndian(
                        input.AsSpan((i * channels + channel) * sizeof(short)),
                        sample);
            }

            byte[] dspInput = adapter.Decode(input, input.Length);
            engine.Process(dspInput, dspInput.Length, channels);
            byte[] physicalPcm = adapter.Encode(dspInput, dspInput.Length);
            if (physicalPcm.Length % (channels * sizeof(float)) != 0)
                throw new InvalidOperationException("Native pipeline lost output frame alignment.");
            framesOut += physicalPcm.Length / (channels * sizeof(float));
            if (packet < 2) continue; // Skip gain-smoother startup for RMS check.
            for (int offset = 0; offset < physicalPcm.Length; offset += sizeof(float))
            {
                float value = BinaryPrimitives.ReadSingleLittleEndian(
                    physicalPcm.AsSpan(offset));
                if (!float.IsFinite(value) || Math.Abs(value) > 1f)
                    throw new InvalidOperationException("Invalid native pipeline output sample.");
                power += value * value;
                measuredSamples++;
            }
        }
        double rms = Math.Sqrt(power / Math.Max(1, measuredSamples));
        if (Math.Abs(framesOut - outputRate / 10) > 2 || rms is < 0.095 or > 0.12)
            throw new InvalidOperationException(
                $"Native PCM16→DSP→48k float pipeline failed: frames={framesOut}, RMS={rms:F5}");
        Console.WriteLine(
            $"PASS: own native 150% DSP on PCM16 44.1k stereo → float32 48k; " +
            $"frames={framesOut}, RMS={rms:F5}; no Windows devices accessed.");
        return 0;
    }

    private static int CheckNative()
    {
        const int rate = 48000;
        const int channels = 2;
        // Native DLL smoke test without requiring a Windows audio device.
        using var engine = new NativeDsp(rate, channels, 150);
        byte[] pcm = new byte[rate / 10 * channels * sizeof(float)];
        for (var offset = 0; offset < pcm.Length; offset += sizeof(float))
            BinaryPrimitives.WriteSingleLittleEndian(pcm.AsSpan(offset), 0.1f);
        engine.Process(pcm, pcm.Length, channels);
        float sample = BinaryPrimitives.ReadSingleLittleEndian(pcm.AsSpan(pcm.Length - sizeof(float)));
        if (Math.Abs(sample - 0.15f) > 0.001f)
            throw new InvalidOperationException("Managed-to-native DSP gain test failed.");
        using var eqEngine = new NativeDsp(rate, channels, 100);
        eqEngine.ConfigureEqualizer(true, new float[] { 0, 0, 0, 8, 0, 0, 0 });
        byte[] tone = new byte[rate / 10 * channels * sizeof(float)];
        for (var frame = 0; frame < rate / 10; frame++)
        {
            float value = 0.02f * MathF.Sin(2f * MathF.PI * 1000f * frame / rate);
            for (var channel = 0; channel < channels; channel++)
                BinaryPrimitives.WriteSingleLittleEndian(
                    tone.AsSpan((frame * channels + channel) * sizeof(float)), value);
        }
        eqEngine.Process(tone, tone.Length, channels);
        double energy = 0;
        var count = 0;
        for (var frame = rate / 20; frame < rate / 10; frame++)
        {
            var v = BinaryPrimitives.ReadSingleLittleEndian(
                tone.AsSpan(frame * channels * sizeof(float)));
            energy += v * v;
            count++;
        }
        var level = Math.Sqrt(energy / count);
        if (level < 0.028)
            throw new InvalidOperationException("Managed native 1 kHz EQ response test failed.");
        Console.WriteLine($"Native Windows bridge P/Invoke: gain={sample:F3}, 1kHz EQ RMS={level:F4}; passed");
        return 0;
    }
}
