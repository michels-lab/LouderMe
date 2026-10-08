using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace LouderMeDesktop;

internal sealed record DesktopUpdateManifest(
    int schema,
    string product,
    string platform,
    string channel,
    string versionName,
    string installerUrl,
    string installerSha256,
    string portableUrl,
    string portableSha256,
    string releaseUrl);

internal sealed record DesktopUpdateCheck(
    bool UpdateAvailable,
    Version CurrentVersion,
    Version LatestVersion,
    DesktopUpdateManifest? Manifest,
    string Message);

internal sealed class DesktopUpdateService
{
    private const string FeedUrl =
        "https://raw.githubusercontent.com/michels-lab/michel-s-life-releases/main/louderme-desktop/latest.json";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    public string CurrentVersionText
    {
        get
        {
            var productVersion = Application.ProductVersion;
            var clean = productVersion.Split('+')[0].Trim();
            return Version.TryParse(clean, out var parsed)
                ? $"{parsed.Major}.{parsed.Minor}.{parsed.Build}"
                : clean;
        }
    }

    public async Task<DesktopUpdateCheck> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = ParseVersion(CurrentVersionText);

        using var response = await _http.GetAsync(FeedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var manifest = JsonSerializer.Deserialize<DesktopUpdateManifest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Desktop update feed is empty.");

        if (manifest.schema != 1)
            throw new InvalidDataException("Desktop update feed schema is not supported.");

        if (!manifest.product.Equals("LouderMe Desktop", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Desktop update feed product does not match LouderMe Desktop.");

        if (!manifest.channel.Equals("stable", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Desktop update feed is not on the stable channel.");

        if (!manifest.platform.Equals("windows-x64", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Desktop update feed platform does not match Windows x64.");

        var latest = ParseVersion(manifest.versionName);
        var available = latest > current;

        return new DesktopUpdateCheck(
            available,
            current,
            latest,
            manifest,
            available
                ? $"LouderMe Desktop v{latest} is available."
                : $"LouderMe Desktop v{current} is current.");
    }

    public async Task<string> DownloadInstallerAsync(
        DesktopUpdateManifest manifest,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Michel's Lab",
            "LouderMe",
            "Updates");
        Directory.CreateDirectory(updateDirectory);

        var target = Path.Combine(
            updateDirectory,
            $"LouderMe-Setup-v{manifest.versionName}.exe");

        using var response = await _http.GetAsync(
            manifest.installerUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(target);

        var buffer = new byte[128 * 1024];
        long copied = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read <= 0) break;
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;

            if (total is > 0)
                progress?.Report((int)Math.Clamp(copied * 100 / total.Value, 0, 100));
        }

        await output.FlushAsync(cancellationToken);

        var actual = await ComputeSha256Async(target, cancellationToken);
        if (!actual.Equals(manifest.installerSha256, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(target); } catch { }
            throw new InvalidDataException(
                $"Downloaded installer failed SHA-256 verification. Expected {manifest.installerSha256}; got {actual}.");
        }

        progress?.Report(100);
        return target;
    }

    public static void LaunchInstaller(string path)
    {
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        }) ?? throw new InvalidOperationException("Windows could not launch the LouderMe installer.");
    }

    public static void OpenReleasePage(DesktopUpdateManifest manifest)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = manifest.releaseUrl,
            UseShellExecute = true,
        });
    }

    private static Version ParseVersion(string value)
    {
        var clean = value.Trim().TrimStart('v', 'V');
        if (Version.TryParse(clean, out var version))
            return version;

        throw new InvalidDataException($"Invalid LouderMe version: {value}");
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
