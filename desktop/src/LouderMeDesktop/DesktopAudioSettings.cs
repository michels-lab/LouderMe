using System.Text.Json;

namespace LouderMeDesktop;

internal sealed class DesktopAudioSettings
{
    public bool BoostEnabled { get; set; }
    public int BoostPercent { get; set; } = 100;
    public bool EqualizerEnabled { get; set; } = true;
    public string Preset { get; set; } = "Flat";
    public float[] EqualizerGainsDb { get; set; } = new float[7];
}

internal static class DesktopSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static string DirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Michel's Lab",
            "LouderMe");

    internal static string SettingsPath => Path.Combine(DirectoryPath, "desktop-settings.json");

    public static DesktopAudioSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new DesktopAudioSettings();
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<DesktopAudioSettings>(json, JsonOptions)
                ?? new DesktopAudioSettings();
            settings.BoostPercent = Math.Clamp(settings.BoostPercent, 100, 250);
            if (settings.EqualizerGainsDb is null || settings.EqualizerGainsDb.Length != 7)
                settings.EqualizerGainsDb = new float[7];
            for (var i = 0; i < settings.EqualizerGainsDb.Length; i++)
                settings.EqualizerGainsDb[i] = Math.Clamp(settings.EqualizerGainsDb[i], -10f, 10f);
            return settings;
        }
        catch
        {
            return new DesktopAudioSettings();
        }
    }

    public static void Save(DesktopAudioSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
