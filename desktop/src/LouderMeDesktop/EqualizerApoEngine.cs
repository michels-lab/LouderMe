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

    private const string DiagnosticMarker =
        "# LouderMe temporary audio-path attenuation; restart recovery guard";
    private const string DiagnosticAttenuation = "Preamp: -18.0 dB";

    private static string DiagnosticContent(DesktopAudioSettings settings) =>
        EqualizerApoConfigBuilder.Build(settings) +
        DiagnosticMarker + Environment.NewLine +
        DiagnosticAttenuation + Environment.NewLine;

    private static string LegacyDiagnosticContent(DesktopAudioSettings settings) =>
        EqualizerApoConfigBuilder.Build(settings) +
        DiagnosticAttenuation + Environment.NewLine;

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\r', '\n');

    // Recover ONLY an exact LouderMe-owned 4-second attenuation file after
    // an application crash. Never overwrite user changes to an APO file.
    // Also recover v0.1.13's unmarked suffix when the whole file matches.
    public bool RecoverInterruptedDiagnostic(DesktopAudioSettings settings)
    {
        if (!File.Exists(LouderMeConfigPath)) return false;
        var current = File.ReadAllText(LouderMeConfigPath);
        var knownTagged = NormalizeNewlines(DiagnosticContent(settings));
        var knownLegacy = NormalizeNewlines(LegacyDiagnosticContent(settings));
        var normalized = NormalizeNewlines(current);
        if (normalized == knownTagged || normalized == knownLegacy)
        {
            File.WriteAllText(LouderMeConfigPath, EqualizerApoConfigBuilder.Build(settings));
            return true;
        }
        if (current.Contains(DiagnosticMarker, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The temporary LouderMe audio test file changed outside the app. " +
                "Automatic recovery stopped to preserve the modified APO configuration. " +
                "Inspect LouderMe.txt manually; no sound path can be certified.");
        return false;
    }

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

        // An interrupted tagged probe is *not* a normal stale-settings write.
        // Refuse to discard unknown external edits until it is safely recovered.
        if (File.Exists(LouderMeConfigPath) &&
            File.ReadAllText(LouderMeConfigPath)
                .Contains(DiagnosticMarker, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "A temporary LouderMe audio-path test is still present. " +
                "Recover it first; modified third-party configuration will not be overwritten.");

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

    // Temporary, exclusively attenuating probe: no microphone, recording,
    // endpoint switching or elevation. Caller must recover via the exact
    // fingerprinted content check in finally; a process crash is recovered
    // again on next launch before showing Boost/EQ as applied.
    public void ApplyDiagnosticAttenuation(DesktopAudioSettings settings)
    {
        if (File.Exists(LouderMeConfigPath) &&
            File.ReadAllText(LouderMeConfigPath)
                .Contains(DiagnosticMarker, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Finish recovery of the previous audio-path diagnostic first.");

        Apply(settings);
        File.AppendAllText(LouderMeConfigPath,
            DiagnosticMarker + Environment.NewLine +
            DiagnosticAttenuation + Environment.NewLine);
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

            // Reproduce the previous failure: process dies before finally,
            // leaving a -18 dB Preamp in Equalizer APO. Next startup must
            // restore ONLY an unmodified, exact LouderMe diagnostic suffix.
            engine.ApplyDiagnosticAttenuation(settings);
            var exactProbe = File.ReadAllText(engine.LouderMeConfigPath);
            if (!exactProbe.Contains(DiagnosticMarker, StringComparison.Ordinal) ||
                !exactProbe.Contains(DiagnosticAttenuation, StringComparison.Ordinal) ||
                engine.GetStatus(settings).MatchesSettings)
                throw new InvalidOperationException("Diagnostic attenuation was not applied.");
            if (!engine.RecoverInterruptedDiagnostic(settings) ||
                engine.GetStatus(settings).MatchesSettings is false)
                throw new InvalidOperationException("Interrupted attenuation was not recovered on startup.");
            if (engine.RecoverInterruptedDiagnostic(settings))
                throw new InvalidOperationException("A completed probe was recovered twice.");

            // A late UI change must never retarget recovery to different
            // settings. The app holds an immutable snapshot and disables its
            // Boost/EQ controls while the four-second effect is active.
            var captureSettings = new DesktopAudioSettings
            {
                BoostEnabled = settings.BoostEnabled,
                BoostPercent = settings.BoostPercent,
                EqualizerEnabled = settings.EqualizerEnabled,
                Preset = settings.Preset,
                EqualizerGainsDb = (float[])settings.EqualizerGainsDb.Clone(),
            };
            engine.ApplyDiagnosticAttenuation(captureSettings);
            settings.BoostPercent = 175;
            var oldProbe = File.ReadAllText(engine.LouderMeConfigPath);
            var changedSettingsRejected = false;
            try { engine.RecoverInterruptedDiagnostic(settings); }
            catch (InvalidOperationException) { changedSettingsRejected = true; }
            if (!changedSettingsRejected ||
                File.ReadAllText(engine.LouderMeConfigPath) != oldProbe)
                throw new InvalidOperationException(
                    "Changed live UI settings incorrectly overwrote the running test.");
            if (!engine.RecoverInterruptedDiagnostic(captureSettings))
                throw new InvalidOperationException(
                    "Captured settings did not restore their own pending audio test.");
            settings.BoostPercent = 200;
            engine.Apply(settings);

            // If another program/user edits the file during the test, never
            // silently clobber the edit at exit or on the next start.
            engine.ApplyDiagnosticAttenuation(settings);
            File.AppendAllText(engine.LouderMeConfigPath, "# legitimate external edit" + Environment.NewLine);
            var changed = File.ReadAllText(engine.LouderMeConfigPath);
            var blocked = false;
            try { engine.RecoverInterruptedDiagnostic(settings); }
            catch (InvalidOperationException) { blocked = true; }
            if (!blocked || File.ReadAllText(engine.LouderMeConfigPath) != changed)
                throw new InvalidOperationException("Modified diagnostic file was silently overwritten.");
            blocked = false;
            try { engine.Apply(settings); }
            catch (InvalidOperationException) { blocked = true; }
            if (!blocked || File.ReadAllText(engine.LouderMeConfigPath) != changed)
                throw new InvalidOperationException("Normal apply overwrote modified probe content.");

            // Explicit test-only repair to resume ordinary product contract.
            File.WriteAllText(engine.LouderMeConfigPath, exactProbe);
            if (!engine.RecoverInterruptedDiagnostic(settings))
                throw new InvalidOperationException("Exact marked diagnostic could not be repaired.");

            // v0.1.13 used an unmarked -18 dB suffix; only byte-equivalent
            // legacy generated content is eligible for safe recovery.
            File.WriteAllText(engine.LouderMeConfigPath, LegacyDiagnosticContent(settings));
            if (!engine.RecoverInterruptedDiagnostic(settings) ||
                !engine.GetStatus(settings).MatchesSettings)
                throw new InvalidOperationException("Original v0.1.13 diagnostic was not recovered.");
            File.WriteAllText(engine.LouderMeConfigPath,
                LegacyDiagnosticContent(settings) + "# external legacy edit" + Environment.NewLine);
            var legacyEdited = File.ReadAllText(engine.LouderMeConfigPath);
            if (engine.RecoverInterruptedDiagnostic(settings) ||
                File.ReadAllText(engine.LouderMeConfigPath) != legacyEdited)
                throw new InvalidOperationException("Legacy foreign edit was overwritten.");
            File.WriteAllText(engine.LouderMeConfigPath, EqualizerApoConfigBuilder.Build(settings));

            // No out-of-process endpoint or hardware proof is implied.
            if (engine.RecoverInterruptedDiagnostic(settings))
                throw new InvalidOperationException("Ordinary APO config was misidentified as a probe.");

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
