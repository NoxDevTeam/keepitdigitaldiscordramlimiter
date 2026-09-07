namespace DiscordRamLimiter.Models;

public sealed record DiscordMemorySnapshot(
    long CurrentWorkingSetBytes,
    long InitialWorkingSetBytes,
    int ProcessCount,
    int DiscordProcessCount,
    int SpotifyProcessCount,
    int ChromeProcessCount,
    bool IsLimiterActive,
    DateTimeOffset CapturedAt);
