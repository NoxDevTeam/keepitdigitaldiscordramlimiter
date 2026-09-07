using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiscordRamLimiter.Models;

namespace DiscordRamLimiter.Services;

public sealed partial class UpdateService
{
    public const string ManifestUrl = "https://raw.githubusercontent.com/NoxDevTeam/keepitdigitaldiscordramlimiter/main/update.json";

    private const long MaximumInstallerBytes = 512L * 1024L * 1024L;
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(15);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static string UpdateCacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Keep It Digital",
        "RAM Limiter",
        "UpdateCache");

    public bool IsConfigured =>
        Uri.TryCreate(ManifestUrl, UriKind.Absolute, out var manifestUri) &&
        manifestUri.Scheme == Uri.UriSchemeHttps &&
        !manifestUri.Host.EndsWith(".example", StringComparison.OrdinalIgnoreCase);

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !Uri.TryCreate(ManifestUrl, UriKind.Absolute, out var manifestUri))
        {
            throw new InvalidOperationException("The update manifest URL has not been configured yet.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);

        using var response = await HttpClient.GetAsync(
            manifestUri,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        EnsureHttps(response.RequestMessage?.RequestUri, "update manifest");
        response.EnsureSuccessStatusCode();

        await using var manifestStream = await response.Content.ReadAsStreamAsync(timeout.Token);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(manifestStream, JsonOptions, timeout.Token)
            ?? throw new InvalidDataException("The update manifest is empty.");

        if (!Version.TryParse(manifest.Version, out var latestVersion))
        {
            throw new InvalidDataException("The update manifest contains an invalid version.");
        }

        if (!Uri.TryCreate(manifest.DownloadUrl, UriKind.Absolute, out var downloadUri) ||
            downloadUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new SecurityException("The update download URL must use HTTPS.");
        }

        var normalizedHash = manifest.Sha256.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!Sha256Pattern().IsMatch(normalizedHash))
        {
            throw new InvalidDataException("The update manifest contains an invalid SHA-256 checksum.");
        }

        return new UpdateCheckResult(
            GetCurrentVersion(),
            latestVersion,
            string.IsNullOrWhiteSpace(manifest.Changelog) ? "No release notes were provided." : manifest.Changelog.Trim(),
            downloadUri,
            normalizedHash.ToUpperInvariant());
    }

    public async Task<string> DownloadAndVerifyAsync(
        UpdateCheckResult update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(UpdateCacheDirectory);

        var partialPath = Path.Combine(UpdateCacheDirectory, "pending-update.exe.part");
        var installerPath = Path.Combine(UpdateCacheDirectory, "pending-update.exe");

        TryDeleteFile(partialPath);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(DownloadTimeout);

            using var response = await HttpClient.GetAsync(
                update.DownloadUri,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            EnsureHttps(response.RequestMessage?.RequestUri, "update installer");
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            if (totalBytes > MaximumInstallerBytes)
            {
                throw new InvalidDataException("The update installer is larger than the allowed maximum size.");
            }

            await using (var source = await response.Content.ReadAsStreamAsync(timeout.Token))
            await using (var destination = new FileStream(
                partialPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                var buffer = new byte[81920];
                long bytesReceived = 0;

                while (true)
                {
                    var bytesRead = await source.ReadAsync(buffer, timeout.Token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    bytesReceived += bytesRead;
                    if (bytesReceived > MaximumInstallerBytes)
                    {
                        throw new InvalidDataException("The update installer exceeded the allowed maximum size.");
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), timeout.Token);
                    progress?.Report(new UpdateDownloadProgress(bytesReceived, totalBytes));
                }
            }

            await VerifyInstallerAsync(partialPath, update, timeout.Token);
            File.Move(partialPath, installerPath, overwrite: true);
            return installerPath;
        }
        catch
        {
            TryDeleteFile(partialPath);
            throw;
        }
    }

    public static void StartInstaller(string installerPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add("/VERYSILENT");
        startInfo.ArgumentList.Add("/SUPPRESSMSGBOXES");
        startInfo.ArgumentList.Add("/NORESTART");
        startInfo.ArgumentList.Add("/CLOSEAPPLICATIONS");
        startInfo.ArgumentList.Add("/UPDATE=1");

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("Windows could not start the verified update installer.");
        }
    }

    public static Version GetCurrentVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
    }

    private static async Task VerifyInstallerAsync(
        string installerPath,
        UpdateCheckResult update,
        CancellationToken cancellationToken)
    {
        await using var installerStream = new FileStream(
            installerPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        var actualHash = await SHA256.HashDataAsync(installerStream, cancellationToken);
        var expectedHash = Convert.FromHexString(update.Sha256);

        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            throw new SecurityException("The downloaded update failed SHA-256 verification and was not opened.");
        }

        var fileVersionText = FileVersionInfo.GetVersionInfo(installerPath).FileVersion;
        if (!Version.TryParse(fileVersionText, out var fileVersion) ||
            NormalizeVersion(fileVersion) != NormalizeVersion(update.LatestVersion))
        {
            throw new SecurityException("The downloaded installer's embedded version does not match the update manifest.");
        }
    }

    private static Version NormalizeVersion(Version version)
    {
        return new Version(
            version.Major,
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));
    }

    private static void EnsureHttps(Uri? uri, string resourceName)
    {
        if (uri is null || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new SecurityException($"The {resourceName} must be delivered over HTTPS.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        var version = GetCurrentVersion();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"KeepItDigitalRamLimiter/{version.Major}.{version.Minor}");
        return client;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [GeneratedRegex("^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
