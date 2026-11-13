using System.Diagnostics;
using System.Globalization;
using NAudio.CoreAudioApi;

namespace LouderMeDesktop;

// The user chooses BOTH endpoints. No installer, kernel operation, APO access,
// silent default-device assignment or automatic reconnect occurs in this class.
internal sealed record NativeRouteEndpoint(string Id, string Name, bool IsVirtual)
{
    public override string ToString() => Name;
}

internal sealed class NativeRouteSession : IAsyncDisposable
{
    private Process? _process;
    private Task? _stdoutDrain;
    private Task? _stderrDrain;
    private readonly SemaphoreSlim _stdinGate = new(1, 1);

    internal event Action<string>? StatusChanged;

    internal bool IsRunning
    {
        get
        {
            try { return _process is { HasExited: false }; }
            catch (InvalidOperationException) { return false; }
        }
    }

    internal static IReadOnlyList<NativeRouteEndpoint> Discover()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(x => new NativeRouteEndpoint(
                x.ID, x.FriendlyName,
                x.FriendlyName.Contains("LouderMe Virtual",
                    StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.IsVirtual).ThenBy(x => x.Name)
            .ToArray();
    }

    internal static void ValidatePair(
        IReadOnlyCollection<NativeRouteEndpoint> endpoints, string sourceId, string outputId)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(outputId) ||
            string.Equals(sourceId, outputId, StringComparison.Ordinal))
            throw new ArgumentException("Choose different virtual and physical outputs.");

        var sources = endpoints.Where(x => x.Id == sourceId).Take(2).ToArray();
        var sinks = endpoints.Where(x => x.Id == outputId).Take(2).ToArray();
        if (sources.Length != 1 || !sources[0].IsVirtual ||
            sinks.Length != 1 || sinks[0].IsVirtual)
            throw new InvalidOperationException(
                "Select exactly one installed LouderMe Virtual source and a different physical output.");
    }

    internal static void RunContractSelfTests()
    {
        NativeRouteEndpoint[] endpoints =
        [
            new("virtual-id", "LouderMe Virtual Speakers", true),
            new("physical-id", "Speakers", false),
            new("other-id", "USB Headset", false)
        ];
        ValidatePair(endpoints, "virtual-id", "physical-id");
        foreach (var (src, sink) in new[]
        {
            ("physical-id", "virtual-id"), ("virtual-id", "virtual-id"),
            ("unknown-id", "physical-id"), ("virtual-id", "unknown-id"),
            ("", "physical-id"), ("physical-id", "other-id")
        })
        {
            try
            {
                ValidatePair(endpoints, src, sink);
                throw new InvalidOperationException("Invalid native audio route was accepted.");
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException ex) when
                (ex.Message.StartsWith("Select exactly one", StringComparison.Ordinal)) { }
        }
        try
        {
            ValidatePair([endpoints[0], endpoints[0], endpoints[1]],
                "virtual-id", "physical-id");
            throw new InvalidOperationException("Duplicate virtual endpoint accepted.");
        }
        catch (InvalidOperationException ex) when
            (ex.Message.StartsWith("Select exactly one", StringComparison.Ordinal)) { }
        if (BuildBoostCommand(100) != "boost 100" ||
            BuildBoostCommand(300) != "boost 300" ||
            BuildEqCommand([0, 0, 0, 0, 0, 0, 0]) != "eq 0,0,0,0,0,0,0")
            throw new InvalidOperationException("Native DSP command serialization changed.");
        foreach (var invalid in new[] { 0, 99, 301, 1000 })
        {
            try { BuildBoostCommand(invalid); }
            catch (ArgumentOutOfRangeException) { continue; }
            throw new InvalidOperationException("Out-of-range live native boost accepted.");
        }
        try { BuildEqCommand([0, float.NaN, 0, 0, 0, 0, 0]); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid native EQ command accepted.");
    }

    private static string BuildBoostCommand(int percent)
    {
        if (percent is < 100 or > 300)
            throw new ArgumentOutOfRangeException(nameof(percent));
        return "boost " + percent.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildEqCommand(IReadOnlyList<float> bands)
    {
        if (bands.Count != 7 || bands.Any(x => !float.IsFinite(x) || x is < -10 or > 10))
            throw new ArgumentException("Seven finite EQ bands between -10 and +10 dB required.");
        return "eq " + string.Join(",", bands.Select(x =>
            x.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    internal async Task<NativeRouteStatus> InspectPairAsync(
        IReadOnlyCollection<NativeRouteEndpoint> snapshot,
        string sourceId, string outputId, CancellationToken cancellationToken = default)
    {
        ValidatePair(snapshot, sourceId, outputId);
        if (!File.Exists(NativeRouteReadiness.BridgePath))
            return new("NATIVE_BRIDGE_NOT_PACKAGED", false,
                "The own audio bridge is not present in this MSIX.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var probe = NewChild();
        probe.StartInfo.RedirectStandardInput = false;
        probe.StartInfo.ArgumentList.Add("--diagnose-route");
        probe.StartInfo.ArgumentList.Add("--source-id");
        probe.StartInfo.ArgumentList.Add(sourceId);
        probe.StartInfo.ArgumentList.Add("--output-id");
        probe.StartInfo.ArgumentList.Add(outputId);
        var started = false;
        try
        {
            if (!probe.Start()) throw new InvalidOperationException("Native diagnostic did not start.");
            started = true;
            var outTask = probe.StandardOutput.ReadToEndAsync(timeout.Token);
            var errTask = probe.StandardError.ReadToEndAsync(timeout.Token);
            await probe.WaitForExitAsync(timeout.Token);
            string json = await outTask;
            _ = await errTask;
            var result = NativeRouteReadiness.ParseSelectedDiagnostic(json);
            if (result.StaticLabPreflightReady && probe.ExitCode != 0)
                throw new InvalidDataException("Ready diagnostic exited unsuccessfully.");
            return result;
        }
        finally
        {
            if (started)
            {
                try { if (!probe.HasExited) probe.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }
    }

    internal async Task StartAsync(
        IReadOnlyCollection<NativeRouteEndpoint> snapshot,
        string sourceId, string outputId,
        CancellationToken cancellationToken = default)
    {
        if (IsRunning) throw new InvalidOperationException("Native audio route is already running.");
        // Always begin at unity. Higher boost requires an explicit later action.
        var inspection = await InspectPairAsync(snapshot, sourceId, outputId, cancellationToken);
        if (!inspection.StaticLabPreflightReady)
            throw new InvalidOperationException(inspection.Message);

        var child = NewChild();
        child.StartInfo.RedirectStandardInput = true;
        foreach (var arg in new[] {
            "--source-id", sourceId, "--output-id", outputId, "--boost", "100",
            "--interactive-controls", "--confirm-experimental"
        }) child.StartInfo.ArgumentList.Add(arg);
        var started = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            if (!child.Start()) throw new InvalidOperationException("Own DSP bridge could not launch.");
            child.StandardInput.AutoFlush = true;
            _process = child;
            _stdoutDrain = DrainAsync(child.StandardOutput, line =>
            {
                if (line == "NATIVE_PCM_ROUTE_STARTED")
                {
                    started.TrySetResult(true);
                    StatusChanged?.Invoke(
                        "WASAPI streams started at 100%. Real external-app audio and acoustic gain are not yet verified.");
                }
                else if (line.StartsWith("sourceFrames=", StringComparison.Ordinal))
                {
                    // This is live transport telemetry from the own PCM bridge,
                    // NOT proof that a third-party application was heard.
                    StatusChanged?.Invoke("Own bridge running (PCM transport telemetry received).");
                }
            }, started);
            _stderrDrain = DrainAsync(child.StandardError, _ => { }, null);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(12), cancellationToken);
            if (child.HasExited)
                throw new InvalidOperationException("Native bridge exited before a stable session.");
            _ = ObserveExitAsync(child);
        }
        catch
        {
            await StopAsync();
            if (_process is null) child.Dispose();
            throw;
        }
    }

    private async Task ObserveExitAsync(Process child)
    {
        try
        {
            await child.WaitForExitAsync();
            StatusChanged?.Invoke("Native bridge stopped. No automatic device reassignment or restart.");
        }
        catch (InvalidOperationException) { }
    }

    private static async Task DrainAsync(
        StreamReader reader, Action<string> onLine,
        TaskCompletionSource<bool>? started)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
                onLine(line);
            started?.TrySetException(new IOException(
                "Native bridge closed before WASAPI streams started."));
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            started?.TrySetException(new IOException("Native bridge output closed.", ex));
        }
    }

    internal Task SetBoostAsync(int percent) =>
        WriteCommandAsync(BuildBoostCommand(percent));

    internal Task SetEqualizerAsync(bool enabled, IReadOnlyList<float> bands) =>
        WriteCommandAsync(enabled ? BuildEqCommand(bands) : "eq off");

    private async Task WriteCommandAsync(string command)
    {
        await _stdinGate.WaitAsync();
        try
        {
            var child = _process;
            if (child is null || child.HasExited)
                throw new InvalidOperationException("Start an explicitly selected native route first.");
            await child.StandardInput.WriteLineAsync(command);
            await child.StandardInput.FlushAsync();
        }
        finally { _stdinGate.Release(); }
    }

    internal async Task StopAsync()
    {
        await _stdinGate.WaitAsync();
        try
        {
            var child = _process;
            if (child is null) return;
            _process = null;
            try
            {
                if (!child.HasExited)
                {
                    // Preferred path lets both WASAPI streams cleanly release
                    // and prevents an orphaned virtual-to-physical audio route.
                    await child.StandardInput.WriteLineAsync("stop");
                    await child.StandardInput.FlushAsync();
                    try
                    {
                        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(4));
                    }
                    catch (TimeoutException)
                    {
                        if (!child.HasExited) child.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException
                                       or System.ComponentModel.Win32Exception)
            {
                try { if (!child.HasExited) child.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
            finally
            {
                child.Dispose();
                StatusChanged?.Invoke("Native session stopped; endpoints released or child terminated.");
            }
        }
        finally { _stdinGate.Release(); }
    }

    private static Process NewChild() => new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = NativeRouteReadiness.BridgePath,
            WorkingDirectory = Path.GetDirectoryName(NativeRouteReadiness.BridgePath)
                ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }
    };

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _stdinGate.Dispose();
    }
}
