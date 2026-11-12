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
        "https://raw.githubusercontent.com/michels-lab/LouderME-Releases/main/louderme-desktop/latest.json";

    // LouderMe must never read another application's release feed.
    private const string OfficialDownloadPath =
        "/michels-lab/LouderME-Releases/releases/download/";
    private const string OfficialReleasePath =
        "/michels-lab/LouderME-Releases/releases/tag/";
    private const long MaxInstallerBytes = 256L * 1024 * 1024;

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
        StorePackageContext.RejectExternalInstaller();
        var current = ParseVersion(CurrentVersionText);

        var manifest = await FetchUpdateManifestAsync(cancellationToken);

        ValidateManifest(manifest);

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

    private async Task<DesktopUpdateManifest> FetchUpdateManifestAsync(
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(FeedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<DesktopUpdateManifest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("LouderMe Desktop update feed is empty.");
    }


    private static bool IsOfficialUrl(string? candidate, string requiredPath)
    {
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.StartsWith(requiredPath, StringComparison.OrdinalIgnoreCase);
    }

    // Fail closed if the feed points at Michel's Life, a lookalike repository
    // or another app. A SHA-256 alone is not a publisher identity check.
    private static void ValidateManifest(DesktopUpdateManifest manifest)
    {
        if (manifest.schema != 1 ||
            !string.Equals(manifest.product, "LouderMe Desktop", StringComparison.Ordinal) ||
            !string.Equals(manifest.channel, "stable", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(manifest.platform, "windows-x64", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("LouderMe update feed product, schema or channel mismatch.");

        if (string.IsNullOrWhiteSpace(manifest.versionName))
            throw new InvalidDataException("LouderMe update feed has no version.");
        _ = ParseVersion(manifest.versionName);

        if (!IsOfficialUrl(manifest.installerUrl, OfficialDownloadPath) ||
            !IsOfficialUrl(manifest.portableUrl, OfficialDownloadPath) ||
            !IsOfficialUrl(manifest.releaseUrl, OfficialReleasePath) ||
            manifest.installerSha256 is null || manifest.installerSha256.Length != 64 ||
            !manifest.installerSha256.All(Uri.IsHexDigit) ||
            manifest.portableSha256 is null || manifest.portableSha256.Length != 64 ||
            !manifest.portableSha256.All(Uri.IsHexDigit))
            throw new InvalidDataException("LouderMe update feed contains an untrusted release URL or checksum.");
    }

    internal static void RunContractSelfTests()
    {
        const string assetRoot = "https://github.com/michels-lab/LouderME-Releases/releases/download/v-test/";
        var good = new DesktopUpdateManifest(
            1, "LouderMe Desktop", "windows-x64", "stable", "0.1.11",
            assetRoot + "LouderMe-Setup.exe", new string('a', 64),
            assetRoot + "LouderMe-Portable.exe", new string('b', 64),
            "https://github.com/michels-lab/LouderME-Releases/releases/tag/v-test");
        ValidateManifest(good);
        var badFeeds = new[]
        {
            good with { product = "Michel's Life" },
            good with { channel = "beta" },
            good with { platform = "android" },
            good with { installerUrl = "https://github.com/michels-lab/michel-s-life-releases/releases/download/v-test/LouderMe.exe" },
            good with { installerUrl = "http://github.com/michels-lab/LouderME-Releases/releases/download/v-test/LouderMe.exe" },
            good with { installerUrl = "https://evil.example/michels-lab/LouderME-Releases/releases/download/v-test/LouderMe.exe" },
            good with { releaseUrl = "https://github.com/michels-lab/michel-s-life-releases/releases/tag/v-test" },
            good with { installerSha256 = "wrong" },
            good with { versionName = "../other-app" }
        };
        foreach (var bad in badFeeds)
        {
            bool rejected = false;
            try { ValidateManifest(bad); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Cross-app or malformed update feed was accepted.");
        }
        RunAtomicInstallerDownloadTestsAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS: official LouderMe feed, publisher URLs, SHA-256 and atomic installer download.");
    }

    // Hermetic file-backed regression checks: no network or real installer.
    private static async Task RunAtomicInstallerDownloadTestsAsync()
    {
        string dir = Path.Combine(Path.GetTempPath(), "LouderMe-Update-Test-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            byte[] valid = System.Text.Encoding.UTF8.GetBytes(
                "LouderMe installer fixture: only a matching digest can publish");
            string sha = Convert.ToHexStringLower(SHA256.HashData(valid));
            string target = Path.Combine(dir, "LouderMe-Setup-v0.1.18.exe");

            static void Preserved(string dir, string path, byte[] data)
            {
                if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(data) ||
                    Directory.EnumerateFiles(dir, "*.part").Any())
                    throw new InvalidOperationException(
                        "Failed transfer replaced a verified installer or left staging bytes.");
            }
            using (var good = new MemoryStream(valid, writable: false))
            {
                string result = await SaveVerifiedInstallerAsync(good, target, sha,
                    valid.LongLength, null, CancellationToken.None);
                if (result != target) throw new InvalidOperationException("Wrong update target.");
                Preserved(dir, target, valid);
            }
            using (var badHash = new MemoryStream(new byte[] { 1, 2, 3 }))
            {
                try
                {
                    await SaveVerifiedInstallerAsync(badHash, target, sha, 3,
                        null, CancellationToken.None);
                    throw new InvalidOperationException("Bad update SHA was accepted.");
                }
                catch (InvalidDataException) { }
                Preserved(dir, target, valid);
            }
            using (var truncated = new MemoryStream(valid, writable: false))
            {
                try
                {
                    await SaveVerifiedInstallerAsync(truncated, target, sha,
                        valid.LongLength + 1, null, CancellationToken.None);
                    throw new InvalidOperationException("Truncated download was accepted.");
                }
                catch (InvalidDataException) { }
                Preserved(dir, target, valid);
            }
            using (var oversize = new MemoryStream(valid, writable: false))
            {
                try
                {
                    await SaveVerifiedInstallerAsync(oversize, target, sha,
                        MaxInstallerBytes + 1, null, CancellationToken.None);
                    throw new InvalidOperationException("Oversize download was accepted.");
                }
                catch (InvalidDataException) { }
                Preserved(dir, target, valid);
            }
            using (var cancelled = new MemoryStream(valid, writable: false))
            using (var cancel = new CancellationTokenSource())
            {
                cancel.Cancel();
                try
                {
                    await SaveVerifiedInstallerAsync(cancelled, target, sha,
                        valid.LongLength, null, cancel.Token);
                    throw new InvalidOperationException("Cancelled update was accepted.");
                }
                catch (OperationCanceledException) { }
                Preserved(dir, target, valid);
            }
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    public async Task<string> DownloadInstallerAsync(
        DesktopUpdateManifest manifest,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateManifest(manifest);
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

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await SaveVerifiedInstallerAsync(
            input, target, manifest.installerSha256,
            response.Content.Headers.ContentLength, progress, cancellationToken);
    }

    // Never truncate a previous verified executable. Stage untrusted bytes
    // on the same volume under a non-executable .part name; only a complete,
    // SHA-matching candidate may atomically replace the final .exe.
    private static async Task<string> SaveVerifiedInstallerAsync(
        Stream input, string target, string expectedSha256,
        long? declaredBytes, IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (declaredBytes.HasValue &&
            (declaredBytes.Value <= 0 || declaredBytes.Value > MaxInstallerBytes))
            throw new InvalidDataException("Installer Content-Length exceeds the safe bound.");

        string directory = Path.GetDirectoryName(target)
            ?? throw new ArgumentException("Installer target has no parent.", nameof(target));
        string stage = Path.Combine(directory, ".LouderMe-" +
            Guid.NewGuid().ToString("N") + ".part");
        try
        {
            long copied = 0;
            await using (var output = new FileStream(
                stage, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 128 * 1024, useAsync: true))
            {
                var buffer = new byte[128 * 1024];
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int read = await input.ReadAsync(buffer, cancellationToken);
                    if (read == 0) break;
                    if (copied + read > MaxInstallerBytes ||
                        (declaredBytes.HasValue && copied + read > declaredBytes.Value))
                        throw new InvalidDataException("Installer exceeded declared or safe byte count.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    copied += read;
                    if (declaredBytes is > 0)
                        progress?.Report((int)Math.Clamp(
                            copied * 100 / declaredBytes.Value, 0, 99));
                }
                await output.FlushAsync(cancellationToken);
            }
            if (copied == 0 || (declaredBytes.HasValue && copied != declaredBytes.Value))
                throw new InvalidDataException("Installer download was empty or truncated.");
            string actual = await ComputeSha256Async(stage, cancellationToken);
            if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Installer SHA-256 mismatch; prior verified installer preserved.");
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stage, target, overwrite: true);
            progress?.Report(100);
            return target;
        }
        finally
        {
            try { if (File.Exists(stage)) File.Delete(stage); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static void LaunchInstaller(string path)
    {
        StorePackageContext.RejectExternalInstaller();
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        }) ?? throw new InvalidOperationException("Windows could not launch the LouderMe installer.");
    }

    public static void OpenReleasePage(DesktopUpdateManifest manifest)
    {
        ValidateManifest(manifest);
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
