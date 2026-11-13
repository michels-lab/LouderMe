using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouderMeDesktop;

/// <summary>
/// Read-only diagnostic bridge for the own Windows virtual endpoint.
/// This MUST NOT install a driver, start playback, write APO configuration,
/// select the OS default device or claim a working native audio route.
/// </summary>
internal sealed record NativeRouteStatus(
    string Code, bool StaticLabPreflightReady, string Message);

internal static class NativeRouteReadiness
{
    internal static string BridgePath =>
        Path.Combine(AppContext.BaseDirectory, "native", "LouderMeAudioBridge.exe");

    internal static async Task<NativeRouteStatus> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(BridgePath))
            return new("NATIVE_BRIDGE_NOT_PACKAGED", false,
                "The LouderMe native audio bridge is not installed. Boost/EQ is not active.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = BridgePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(BridgePath)
                    ?? AppContext.BaseDirectory,
            }
        };
        // --diagnose-route without device IDs enumerates metadata only.
        // It explicitly cannot return a ready/active audio route.
        process.StartInfo.ArgumentList.Add("--diagnose-route");
        var started = false;
        try
        {
            if (!process.Start())
                return new("NATIVE_DIAGNOSTIC_FAILED", false,
                    "Windows could not launch the read-only native diagnostic.");
            started = true;
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            string json = await output;
            _ = await error; // Never expose endpoint names, IDs, command paths or exceptions.
            // Exit 3 is expected for an unselected route; an error is NOT ready.
            if (process.ExitCode != 0 && process.ExitCode != 3)
                return new("NATIVE_DIAGNOSTIC_FAILED", false,
                    "The native audio check failed. No audio routing was changed.");
            return ParseDiagnostic(json);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryStopProbe(process, started);
            return new("NATIVE_DIAGNOSTIC_TIMEOUT", false,
                "The native audio check timed out; Boost/EQ remains disabled.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or
                                   System.ComponentModel.Win32Exception or JsonException)
        {
            TryStopProbe(process, started);
            return new("NATIVE_DIAGNOSTIC_FAILED", false,
                "The native audio check could not be completed safely.");
        }
    }

    private static void TryStopProbe(Process process, bool started)
    {
        if (!started) return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { /* Process exited concurrently. */ }
        catch (System.ComponentModel.Win32Exception) { /* No new unsafe action. */ }
    }

    internal static NativeRouteStatus ParseDiagnostic(string json) =>
        ParseDiagnostic(json, explicitlySelected: false);

    internal static NativeRouteStatus ParseSelectedDiagnostic(string json) =>
        ParseDiagnostic(json, explicitlySelected: true);

    private static NativeRouteStatus ParseDiagnostic(string json, bool explicitlySelected)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("code", out var codeProperty) ||
            codeProperty.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("ready_for_controlled_lab_only", out var readyProperty) ||
            (readyProperty.ValueKind != JsonValueKind.True &&
             readyProperty.ValueKind != JsonValueKind.False))
            throw new JsonException("Invalid native route diagnostic schema.");

        var code = codeProperty.GetString() ?? string.Empty;
        if (!Regex.IsMatch(code, @"^[A-Z][A-Z0-9_]{2,79}$"))
            throw new JsonException("Unsafe native route diagnostic code.");

        // Fail closed: the bridge's metadata preflight is not a driver or
        // hardware proof. A future bridge cannot silently promote READY to
        // audio verified merely by adding a JSON flag.
        foreach (var claim in new[] {
            "real_audio_verified", "driver_signing_verified",
            "windows_system_audio_routing_verified", "acoustic_output_verified"
        })
        {
            if (!root.TryGetProperty(claim, out var field) ||
                field.ValueKind != JsonValueKind.False)
                throw new JsonException("Unverified native audio evidence claim.");
        }

        var ready = readyProperty.GetBoolean();
        if (ready && code != "STATIC_PREFLIGHT_READY")
            throw new JsonException("Native readiness/code contradiction.");
        if (!ready && code == "STATIC_PREFLIGHT_READY")
            throw new JsonException("Native readiness/code contradiction.");

        // Ordinary automatic GUI diagnostics always lack endpoints; a selected
        // preflight may acknowledge metadata-only readiness but NEVER playback.
        if (ready && !explicitlySelected)
            throw new JsonException("Read-only GUI probe unexpectedly selected an audio route.");
        if (ready)
            return new NativeRouteStatus(code, true,
                "Virtual and physical endpoint metadata are compatible for a controlled lab session; audio, driver signing and physical gain remain UNVERIFIED.");

        return new NativeRouteStatus(code, false, code switch
        {
            "VIRTUAL_ENDPOINT_NOT_FOUND" =>
                "LouderMe's own Windows virtual endpoint is not installed. No native Boost/EQ is active.",
            "SELECTION_REQUIRED" =>
                "A LouderMe virtual endpoint was detected. Native audio is NOT active; source/output selection and hardware verification are still required.",
            _ =>
                "Native route preflight: " + code + ". No audio processing was started."
        });
    }

    internal static void RunContractSelfTests()
    {
        const string claims =
            ",\"real_audio_verified\":false,\"driver_signing_verified\":false" +
            ",\"windows_system_audio_routing_verified\":false,\"acoustic_output_verified\":false";
        var missing = ParseDiagnostic(
            "{\"code\":\"VIRTUAL_ENDPOINT_NOT_FOUND\",\"ready_for_controlled_lab_only\":false" + claims + "}");
        if (missing.StaticLabPreflightReady || missing.Code != "VIRTUAL_ENDPOINT_NOT_FOUND")
            throw new InvalidOperationException("Absent virtual endpoint was called ready.");

        var selected = ParseSelectedDiagnostic(
            "{\"code\":\"STATIC_PREFLIGHT_READY\",\"ready_for_controlled_lab_only\":true" + claims + "}");
        if (!selected.StaticLabPreflightReady || selected.Message.Contains("audio verified", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Explicit selected metadata contract regressed.");
        if (ParseSelectedDiagnostic(
            "{\"code\":\"VIRTUAL_ENDPOINT_NOT_FOUND\",\"ready_for_controlled_lab_only\":false" + claims + "}")
            .StaticLabPreflightReady)
            throw new InvalidOperationException("Missing selected endpoint was called ready.");
        try
        {
            ParseSelectedDiagnostic(
                "{\"code\":\"STATIC_PREFLIGHT_READY\",\"ready_for_controlled_lab_only\":true" +
                claims.Replace("\"real_audio_verified\":false", "\"real_audio_verified\":true") + "}");
            throw new InvalidOperationException("Forged live audio proof passed selected route parser.");
        }
        catch (JsonException) { /* expected fail closed */ }

        var unselected = ParseDiagnostic(
            "{\"code\":\"SELECTION_REQUIRED\",\"ready_for_controlled_lab_only\":false" + claims + "}");
        if (unselected.StaticLabPreflightReady || !unselected.Message.Contains("NOT active", StringComparison.Ordinal))
            throw new InvalidOperationException("Unselected source was called active.");

        foreach (var forged in new[] {
            "{\"code\":\"STATIC_PREFLIGHT_READY\",\"ready_for_controlled_lab_only\":true" + claims + "}",
            "{\"code\":\"STATIC_PREFLIGHT_READY\",\"ready_for_controlled_lab_only\":false" + claims + "}",
            "{\"code\":\"VIRTUAL_ENDPOINT_NOT_FOUND\",\"ready_for_controlled_lab_only\":false" +
                claims.Replace("\"driver_signing_verified\":false", "\"driver_signing_verified\":true") + "}",
            "{\"code\":\"VIRTUAL_ENDPOINT_NOT_FOUND\",\"ready_for_controlled_lab_only\":false}",
            "{\"code\":\"bad\\nraw-id\",\"ready_for_controlled_lab_only\":false" + claims + "}"
        })
        {
            try
            {
                ParseDiagnostic(forged);
                throw new InvalidOperationException("Unsafe native readiness proof accepted.");
            }
            catch (JsonException) { /* expected fail-closed */ }
        }
    }
}
