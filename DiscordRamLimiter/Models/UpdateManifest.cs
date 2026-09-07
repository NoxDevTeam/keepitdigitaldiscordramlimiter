namespace DiscordRamLimiter.Models;

public sealed record UpdateManifest
{
    public string Version { get; init; } = string.Empty;

    public string Changelog { get; init; } = string.Empty;

    public string DownloadUrl { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;
}

public sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    string Changelog,
    Uri DownloadUri,
    string Sha256)
{
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;
}

public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes)
{
    public double? Percentage => TotalBytes is > 0
        ? BytesReceived * 100d / TotalBytes.Value
        : null;
}
