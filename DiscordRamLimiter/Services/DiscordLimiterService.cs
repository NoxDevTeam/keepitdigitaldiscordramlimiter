using System.Diagnostics;
using System.Runtime.InteropServices;
using DiscordRamLimiter.Models;

namespace DiscordRamLimiter.Services;

public sealed class DiscordLimiterService : IDisposable
{
    private static readonly TimeSpan MonitorInterval = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan PostTrimRefreshDelay = TimeSpan.FromMilliseconds(180);
    private static readonly (string ProcessName, TrackedApp App)[] TrackedProcessNames =
    [
        ("Discord", TrackedApp.Discord),
        ("DiscordCanary", TrackedApp.Discord),
        ("DiscordPTB", TrackedApp.Discord),
        ("DiscordDevelopment", TrackedApp.Discord),
        ("Spotify", TrackedApp.Spotify),
        ("chrome", TrackedApp.Chrome)
    ];

    private CancellationTokenSource? _monitorCancellation;
    private Task? _monitorTask;
    private long? _initialWorkingSetBytes;
    private bool _disposed;

    public event EventHandler<DiscordMemorySnapshot>? SnapshotUpdated;

    public bool IsLimiterActive { get; private set; }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);

    public Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_monitorTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _initialWorkingSetBytes ??= GetTotalTrackedWorkingSetBytes(out _, out _, out _, out _);
        _monitorCancellation = new CancellationTokenSource();
        _monitorTask = MonitorLoopAsync(_monitorCancellation.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        IsLimiterActive = false;

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

    public void SetLimiterActive(bool isActive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsLimiterActive = isActive;
    }

    public int GetLargestTrackedProcessId()
    {
        var largestProcessId = -1;
        long largestWorkingSetBytes = -1;

        foreach (var trackedProcess in GetTrackedProcesses())
        {
            var process = trackedProcess.Process;
            try
            {
                var workingSetBytes = SafeWorkingSet(process);
                if (workingSetBytes > largestWorkingSetBytes)
                {
                    largestWorkingSetBytes = workingSetBytes;
                    largestProcessId = process.Id;
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return largestProcessId;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var beforeBytes = GetTotalTrackedWorkingSetBytes(
                out var processCount,
                out var discordProcessCount,
                out var spotifyProcessCount,
                out var chromeProcessCount);

            if (IsLimiterActive && processCount > 0)
            {
                LimitTrackedMemory();

                await Task.Delay(PostTrimRefreshDelay, cancellationToken);
                beforeBytes = GetTotalTrackedWorkingSetBytes(
                    out processCount,
                    out discordProcessCount,
                    out spotifyProcessCount,
                    out chromeProcessCount);
            }

            SnapshotUpdated?.Invoke(
                this,
                new DiscordMemorySnapshot(
                    beforeBytes,
                    _initialWorkingSetBytes ?? 0,
                    processCount,
                    discordProcessCount,
                    spotifyProcessCount,
                    chromeProcessCount,
                    IsLimiterActive,
                    DateTimeOffset.Now));

            await Task.Delay(MonitorInterval, cancellationToken);
        }
    }

    private static void LimitTrackedMemory()
    {
        foreach (var trackedProcess in GetTrackedProcesses())
        {
            var process = trackedProcess.Process;
            try
            {
                SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static long GetTotalTrackedWorkingSetBytes(
        out int processCount,
        out int discordProcessCount,
        out int spotifyProcessCount,
        out int chromeProcessCount)
    {
        long totalBytes = 0;
        processCount = 0;
        discordProcessCount = 0;
        spotifyProcessCount = 0;
        chromeProcessCount = 0;

        foreach (var trackedProcess in GetTrackedProcesses())
        {
            var process = trackedProcess.Process;
            try
            {
                totalBytes += process.WorkingSet64;
                processCount++;

                if (trackedProcess.App == TrackedApp.Discord)
                {
                    discordProcessCount++;
                }
                else if (trackedProcess.App == TrackedApp.Spotify)
                {
                    spotifyProcessCount++;
                }
                else
                {
                    chromeProcessCount++;
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return totalBytes;
    }

    private static IEnumerable<TrackedProcess> GetTrackedProcesses()
    {
        foreach (var (processName, app) in TrackedProcessNames)
        {
            Process[] processes;

            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (var process in processes)
            {
                yield return new TrackedProcess(process, app);
            }
        }
    }

    private static long SafeWorkingSet(Process process)
    {
        try
        {
            return process.WorkingSet64;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    private enum TrackedApp
    {
        Discord,
        Spotify,
        Chrome
    }

    private readonly record struct TrackedProcess(Process Process, TrackedApp App);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IsLimiterActive = false;
        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
    }
}
