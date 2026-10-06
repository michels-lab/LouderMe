namespace LouderMeDesktop;

internal sealed record ApoEngineStatus(bool Installed, bool Configured, string Message);

internal sealed class EqualizerApoEngine
{
    public string InstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "EqualizerAPO");

    public string ConfigDirectory => Path.Combine(InstallDirectory, "config");
    public string MainConfigPath => Path.Combine(ConfigDirectory, "config.txt");
    public string LouderMeConfigPath => Path.Combine(ConfigDirectory, "LouderMe.txt");

    public ApoEngineStatus GetStatus()
    {
        var installed = Directory.Exists(InstallDirectory);
        var configured = false;

        if (installed && File.Exists(MainConfigPath))
        {
            configured = File.ReadAllText(MainConfigPath)
                .Contains(
                    EqualizerApoConfigBuilder.IncludeLine,
                    StringComparison.OrdinalIgnoreCase);
        }

        return new ApoEngineStatus(
            installed,
            configured,
            installed
                ? configured
                    ? "System-wide APO engine is linked to LouderMe."
                    : "Audio engine found; LouderMe configuration is not linked yet."
                : "System-wide audio engine is not installed.");
    }

    public void Apply(DesktopAudioSettings settings)
    {
        if (!Directory.Exists(InstallDirectory))
            throw new InvalidOperationException("System-wide audio engine is not installed.");

        Directory.CreateDirectory(ConfigDirectory);

        var existing = File.Exists(MainConfigPath)
            ? File.ReadAllText(MainConfigPath)
            : string.Empty;

        var backup = MainConfigPath + ".louderme.bak";
        if (File.Exists(MainConfigPath) && !File.Exists(backup))
            File.Copy(MainConfigPath, backup);

        File.WriteAllText(
            LouderMeConfigPath,
            EqualizerApoConfigBuilder.Build(settings));

        var updated = EqualizerApoConfigBuilder.EnsureInclude(existing);
        if (!string.Equals(existing, updated, StringComparison.Ordinal))
            File.WriteAllText(MainConfigPath, updated);
    }
}
