using System.Text;
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
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string DirectoryPath => DeveloperMode.DataDirectory;
    internal static string SettingsPath => Path.Combine(DirectoryPath, "desktop-settings.json");

    private static DesktopAudioSettings Normalize(DesktopAudioSettings settings)
    {
        var eq = new float[7];
        float[] source = settings.EqualizerGainsDb ?? [];
        for (int i = 0; i < eq.Length; ++i)
        {
            float v = i < source.Length ? source[i] : 0;
            eq[i] = float.IsFinite(v) ? Math.Clamp(v, -10, 10) : 0;
        }
        return new DesktopAudioSettings
        {
            BoostEnabled = settings.BoostEnabled,
            BoostPercent = Math.Clamp(settings.BoostPercent, 100, 250),
            EqualizerEnabled = settings.EqualizerEnabled,
            Preset = string.IsNullOrWhiteSpace(settings.Preset) ? "Flat" : settings.Preset,
            EqualizerGainsDb = eq
        };
    }

    public static DesktopAudioSettings Load() => LoadFromPath(SettingsPath);

    internal static DesktopAudioSettings LoadFromPath(string path)
    {
        try
        {
            if (!File.Exists(path)) return new DesktopAudioSettings();
            var settings = JsonSerializer.Deserialize<DesktopAudioSettings>(
                File.ReadAllText(path), JsonOptions);
            return settings is null ? new DesktopAudioSettings() : Normalize(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                         or JsonException or NotSupportedException)
        {
            // Corrupt legacy JSON remains untouched for owner recovery.
            return new DesktopAudioSettings();
        }
    }

    public static void Save(DesktopAudioSettings settings) => SaveToPath(SettingsPath, settings);

    internal static void SaveToPath(
        string path, DesktopAudioSettings settings, Action? beforePublishForTest = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string target = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(directory);
        string stage = Path.Combine(directory, ".louderme-settings-" +
            Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(Normalize(settings), JsonOptions));
            using (var stream = new FileStream(
                stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            // No user settings file is truncated until the atomic rename.
            beforePublishForTest?.Invoke();
            File.Move(stage, target, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(stage)) File.Delete(stage); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // Device-free tests on unique test paths, never the actual user profile.
    internal static void RunContractSelfTests()
    {
        string root = Path.Combine(Path.GetTempPath(), "LouderMe-Prefs-" +
            Guid.NewGuid().ToString("N"));
        try
        {
            string stable = Path.Combine(root, "stable", "desktop-settings.json");
            string developer = Path.Combine(root, "developer", "desktop-settings.json");
            if (LoadFromPath(stable).BoostPercent != 100)
                throw new InvalidOperationException("Missing preferences defaults failed.");
            var old = new DesktopAudioSettings
            {
                BoostEnabled = true, BoostPercent = 200, Preset = "Rock",
                EqualizerGainsDb = [1, 2, 3, 4, 5, 6, 7]
            };
            SaveToPath(stable, old);
            byte[] preserved = File.ReadAllBytes(stable);
            var loaded = LoadFromPath(stable);
            if (!loaded.BoostEnabled || loaded.BoostPercent != 200 ||
                loaded.Preset != "Rock" || loaded.EqualizerGainsDb[6] != 7)
                throw new InvalidOperationException("Boost/EQ roundtrip failed.");

            var next = new DesktopAudioSettings
            {
                BoostEnabled = true, BoostPercent = 250, Preset = "Custom",
                EqualizerGainsDb = [-9, -7, -4, 0, 4, 7, 9]
            };
            bool interrupted = false;
            try
            {
                SaveToPath(stable, next, () =>
                    throw new IOException("Injected failure after flush, before rename"));
            }
            catch (IOException) { interrupted = true; }
            if (!interrupted ||
                !File.ReadAllBytes(stable).AsSpan().SequenceEqual(preserved) ||
                Directory.EnumerateFiles(Path.GetDirectoryName(stable)!, "*.tmp").Any())
                throw new InvalidOperationException("Interrupted write damaged last-good JSON.");

            SaveToPath(developer, next);
            if (LoadFromPath(developer).BoostPercent != 250 ||
                !File.ReadAllBytes(stable).AsSpan().SequenceEqual(preserved))
                throw new InvalidOperationException("Developer preferences overwrote stable settings.");

            SaveToPath(stable, new DesktopAudioSettings
            {
                BoostPercent = 999, Preset = "",
                EqualizerGainsDb = [float.NaN, float.PositiveInfinity, -20, 0, 3, 9, 20]
            });
            var safe = LoadFromPath(stable);
            if (safe.BoostPercent != 250 || safe.Preset != "Flat" ||
                safe.EqualizerGainsDb[0] != 0 || safe.EqualizerGainsDb[1] != 0 ||
                safe.EqualizerGainsDb[2] != -10 || safe.EqualizerGainsDb[6] != 10)
                throw new InvalidOperationException("Nonfinite/invalid EQ values reached persistence.");

            File.WriteAllText(stable, "{corrupt-owner-file");
            if (LoadFromPath(stable).BoostPercent != 100 ||
                File.ReadAllText(stable) != "{corrupt-owner-file")
                throw new InvalidOperationException("Corrupt legacy file was destroyed or read as valid.");

            Console.WriteLine(
                "PASS: atomic Boost/EQ prefs, crash injection, owner JSON preservation, " +
                "finite clamping and Developer/stable file separation.");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
