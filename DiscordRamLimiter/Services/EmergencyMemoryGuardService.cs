using System.Runtime.InteropServices;

namespace DiscordRamLimiter.Services;

public sealed class EmergencyMemoryGuardService : IDisposable
{
    public const uint ShutdownThresholdPercent = 99;
    public static readonly TimeSpan RequiredHighMemoryDuration = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(5);
    private CancellationTokenSource? _monitorCancellation;
    private Task? _monitorTask;
    private DateTimeOffset? _highMemoryStartedAt;
    private bool _shutdownRequestRaised;
    private bool _disposed;

    public event EventHandler<EmergencyShutdownRequestedEventArgs>? EmergencyShutdownRequested;

    public bool IsEnabled { get; private set; }

    public Task StartAsync(bool isEnabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SetEnabled(isEnabled);

        if (_monitorTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _monitorCancellation = new CancellationTokenSource();
        _monitorTask = MonitorLoopAsync(_monitorCancellation.Token);
        return Task.CompletedTask;
    }

    public void SetEnabled(bool isEnabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsEnabled = isEnabled;

        if (!isEnabled)
        {
            ResetThresholdState();
        }
    }

    public async Task StopAsync()
    {
        if (_monitorCancellation is null)
        {
            return;
        }

        await _monitorCancellation.CancelAsync();

        if (_monitorTask is not null)
        {
            try
            {
                await _monitorTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _monitorCancellation.Dispose();
        _monitorCancellation = null;
        _monitorTask = null;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (IsEnabled && TryGetMemoryLoadPercent(out var memoryLoadPercent))
            {
                EvaluateMemoryLoad(memoryLoadPercent, DateTimeOffset.UtcNow);
            }

            await Task.Delay(MonitorInterval, cancellationToken);
        }
    }

    private void EvaluateMemoryLoad(uint memoryLoadPercent, DateTimeOffset capturedAt)
    {
        if (memoryLoadPercent < ShutdownThresholdPercent)
        {
            ResetThresholdState();
            return;
        }

        _highMemoryStartedAt ??= capturedAt;

        if (_shutdownRequestRaised || capturedAt - _highMemoryStartedAt < RequiredHighMemoryDuration)
        {
            return;
        }

        _shutdownRequestRaised = true;
        EmergencyShutdownRequested?.Invoke(
            this,
            new EmergencyShutdownRequestedEventArgs(memoryLoadPercent, capturedAt));
    }

    private void ResetThresholdState()
    {
        _highMemoryStartedAt = null;
        _shutdownRequestRaised = false;
    }

    private static bool TryGetMemoryLoadPercent(out uint memoryLoadPercent)
    {
        var memoryStatus = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref memoryStatus))
        {
            memoryLoadPercent = 0;
            return false;
        }

        memoryLoadPercent = memoryStatus.MemoryLoad;
        return true;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IsEnabled = false;
        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
    }
}

public sealed record EmergencyShutdownRequestedEventArgs(
    uint MemoryLoadPercent,
    DateTimeOffset CapturedAt);
