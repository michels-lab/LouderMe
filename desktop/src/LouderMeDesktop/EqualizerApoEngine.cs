using System.Diagnostics;
using System.Security.Principal;

namespace LouderMeDesktop;

// Linked files do NOT prove that the Windows sound endpoint has an APO attached.
internal sealed record ApoEngineStatus(
    bool Installed, bool Configured, bool MatchesSettings, string Message);

internal sealed class EqualizerApoEngine
{
    private readonly string _installDirectory;

    public EqualizerApoEngine(string? installDirectory = null)
    {
        _installDirectory = installDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "EqualizerAPO");
    }

    public string InstallDirectory => _installDirectory;
    public string ConfigDirectory => Path.Combine(InstallDirectory, "config");
    public string MainConfigPath => Path.Combine(ConfigDirectory, "config.txt");
    public string LouderMeConfigPath => Path.Combine(ConfigDirectory, "LouderMe.txt");

    public ApoEngineStatus GetStatus(DesktopAudioSettings? settings = null)
    {
        if (!Directory.Exists(InstallDirectory))
            return new(false, false, false,
                "NOT ACTIVE: Equalizer APO is not installed. Boost and EQ cannot change playback.");

        try
        {
            var mainConfig = File.Exists(MainConfigPath)
                ? File.ReadAllText(MainConfigPath)
                : string.Empty;
            var linked = EqualizerApoConfigBuilder.HasActiveInclude(mainConfig)
                && File.Exists(LouderMeConfigPath);
            if (!linked)
                return new(true, false, false,
                    "NOT ACTIVE: LouderMe has no active Equalizer APO include. " +
                    "Use Connect Boost/EQ for one-time Windows permission setup.");

            if (settings is not null)
            {
                var expected = EqualizerApoConfigBuilder.Build(settings);
                var actual = File.ReadAllText(LouderMeConfigPath);
                if (!NormalizeNewlines(actual).Equals(
                    NormalizeNewlines(expected), StringComparison.Ordinal))
                    return new(true, true, false,
                        "NOT APPLIED: the configuration on disk does not match the displayed Boost/EQ settings.");
            }

            return new(true, true, true,
                "Configuration written. Audible processing is NOT verified: " +
                "select the actual output in Equalizer APO Configurator and restart Windows if requested.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(true, false, false,
                "NOT ACTIVE: cannot read Equalizer APO configuration: " + ex.Message);
        }
    }

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\r', '\n');

    // Normal edits intentionally do not elevate. The user consents separately
    // to the one-time privileged setup; after that only LouderMe.txt is user-writable.
    public void Apply(DesktopAudioSettings settings)
    {
        if (!Directory.Exists(InstallDirectory))
            throw new InvalidOperationException("Equalizer APO is not installed.");

        Directory.CreateDirectory(ConfigDirectory);
        var existing = File.Exists(MainConfigPath)
            ? File.ReadAllText(MainConfigPath) : string.Empty;

        var updated = EqualizerApoConfigBuilder.EnsureInclude(existing);
        var generated = EqualizerApoConfigBuilder.Build(settings);

        if (!File.Exists(LouderMeConfigPath) ||
            !NormalizeNewlines(File.ReadAllText(LouderMeConfigPath))
                .Equals(NormalizeNewlines(generated), StringComparison.Ordinal))
        {
            File.WriteAllText(LouderMeConfigPath, generated);
        }

        if (!string.Equals(existing, updated, StringComparison.Ordinal))
        {
            var backup = MainConfigPath + ".louderme.bak";
            if (File.Exists(MainConfigPath) && !File.Exists(backup))
                File.Copy(MainConfigPath, backup);
            File.WriteAllText(MainConfigPath, updated);
        }
    }

    // Temporary, exclusively attenuating probe. It cannot prove loopback
    // audio itself; the owner listens for a volume decrease on their device.
    // Caller MUST restore Apply(settings) in a finally block.
    public void ApplyDiagnosticAttenuation(DesktopAudioSettings settings)
    {
        Apply(settings);
        File.AppendAllText(LouderMeConfigPath,
            "Preamp: -18.0 dB" + Environment.NewLine);
    }

    // Run only after a user explicitly confirms a Windows UAC prompt.
    // The command accepts a user SID, NOT arbitrary file paths/commands.
    public void SetupForUser(string userSid)
    {
        if (!userSid.StartsWith("S-1-5-21-", StringComparison.Ordinal) &&
            !userSid.StartsWith("S-1-12-1-", StringComparison.Ordinal))
            throw new ArgumentException("Only individual Windows account SIDs are allowed.");
        _ = new SecurityIdentifier(userSid); // Validate the entire SID syntax.

        if (!Directory.Exists(InstallDirectory))
            throw new InvalidOperationException("Equalizer APO must be installed first.");

        Directory.CreateDirectory(ConfigDirectory);
        var existing = File.Exists(MainConfigPath)
            ? File.ReadAllText(MainConfigPath) : string.Empty;
        var updated = EqualizerApoConfigBuilder.EnsureInclude(existing);
        if (!File.Exists(LouderMeConfigPath))
            File.WriteAllText(LouderMeConfigPath,
                EqualizerApoConfigBuilder.Build(new DesktopAudioSettings()));

        if (!string.Equals(existing, updated, StringComparison.Ordinal))
        {
            var backup = MainConfigPath + ".louderme.bak";
            if (File.Exists(MainConfigPath) && !File.Exists(backup))
                File.Copy(MainConfigPath, backup);
            File.WriteAllText(MainConfigPath, updated);
        }

        // Grant this account Write to our managed file ONLY, never the main
        // config, Equalizer APO executables, or the entire install directory.
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "icacls.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(LouderMeConfigPath);
        start.ArgumentList.Add("/grant");
        start.ArgumentList.Add("*" + userSid + ":(W)");
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not launch Windows ACL setup.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                "Windows could not grant limited write access to LouderMe.txt.");

        if (!EqualizerApoConfigBuilder.HasActiveInclude(
            File.ReadAllText(MainConfigPath)))
            throw new InvalidOperationException("APO main configuration was not linked.");
    }

    internal static void RunContractSelfTests()
    {
        var temp = Path.Combine(Path.GetTempPath(), "LouderMe-ApoContract-" +
            Guid.NewGuid().ToString("N"));
        try
        {
            var engine = new EqualizerApoEngine(temp);
            var settings = new DesktopAudioSettings
            {
                BoostEnabled = true, BoostPercent = 200,
            };
            if (engine.GetStatus(settings).Installed)
                throw new InvalidOperationException("Missing APO was reported installed.");
            Directory.CreateDirectory(engine.ConfigDirectory);
            File.WriteAllText(engine.MainConfigPath,
                "# Existing third-party filters\n# Include: LouderMe.txt\n");
            var initial = engine.GetStatus(settings);
            if (!initial.Installed || initial.Configured ||
                EqualizerApoConfigBuilder.HasActiveInclude(
                    File.ReadAllText(engine.MainConfigPath)))
                throw new InvalidOperationException("A commented include was treated as live.");

            engine.Apply(settings);
            var ready = engine.GetStatus(settings);
            if (!ready.Configured || !ready.MatchesSettings)
                throw new InvalidOperationException("Applied settings are not detected.");
            var applied = File.ReadAllText(engine.MainConfigPath);
            if (!applied.Contains("# Existing third-party filters") ||
                !EqualizerApoConfigBuilder.HasActiveInclude(applied))
                throw new InvalidOperationException("User APO config not preserved.");
            if (!File.Exists(engine.MainConfigPath + ".louderme.bak"))
                throw new InvalidOperationException("Original APO config was not backed up.");

            engine.ApplyDiagnosticAttenuation(settings);
            if (!File.ReadAllText(engine.LouderMeConfigPath)
                    .Contains("Preamp: -18.0 dB", StringComparison.Ordinal) ||
                engine.GetStatus(settings).MatchesSettings)
                throw new InvalidOperationException("Diagnostic attenuation was not applied.");
            engine.Apply(settings);
            if (File.ReadAllText(engine.LouderMeConfigPath)
                    .Contains("Preamp: -18.0 dB", StringComparison.Ordinal))
                throw new InvalidOperationException("Temporary attenuation was not restored.");

            settings.BoostPercent = 175;
            var stale = engine.GetStatus(settings);
            if (!stale.Configured || stale.MatchesSettings)
                throw new InvalidOperationException("Stale processing was reported applied.");
            engine.Apply(settings);
            if (!engine.GetStatus(settings).MatchesSettings)
                throw new InvalidOperationException("Updated settings did not persist.");
            var count = File.ReadAllLines(engine.MainConfigPath).Count(line =>
                line.Trim() == EqualizerApoConfigBuilder.IncludeLine);
            if (count != 1)
                throw new InvalidOperationException("Duplicate APO include was written.");
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }
}
