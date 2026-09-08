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

        // Browser application processes. WebView runtimes are excluded because other apps depend on them.
        ("chrome", TrackedApp.Browser),
        ("msedge", TrackedApp.Browser),
        ("firefox", TrackedApp.Browser),
        ("brave", TrackedApp.Browser),
        ("opera", TrackedApp.Browser),
        ("vivaldi", TrackedApp.Browser),
        ("chromium", TrackedApp.Browser),
        ("Arc", TrackedApp.Browser),
        ("Dia", TrackedApp.Browser),
        ("Safari", TrackedApp.Browser),
        ("waterfox", TrackedApp.Browser),
        ("librewolf", TrackedApp.Browser),
        ("floorp", TrackedApp.Browser),
        ("zen", TrackedApp.Browser),
        ("thorium", TrackedApp.Browser),
        ("duckduckgo", TrackedApp.Browser),
        ("maxthon", TrackedApp.Browser),
        ("palemoon", TrackedApp.Browser),
        ("basilisk", TrackedApp.Browser),
        ("seamonkey", TrackedApp.Browser),
        ("iexplore", TrackedApp.Browser),
        ("slimjet", TrackedApp.Browser),
        ("centbrowser", TrackedApp.Browser),
        ("sidekick", TrackedApp.Browser),
        ("wavebox", TrackedApp.Browser),
        ("avastbrowser", TrackedApp.Browser),
        ("avgbrowser", TrackedApp.Browser),
        ("ulaa", TrackedApp.Browser),
        ("qutebrowser", TrackedApp.Browser),
        ("falkon", TrackedApp.Browser),
        ("midori", TrackedApp.Browser),

        // User-facing background apps from the optional utilities shown in Task Manager.
        // Critical Windows, security, shell, and display-driver processes are intentionally excluded.
        ("NVIDIA App", TrackedApp.Background),
        ("NVIDIA Overlay", TrackedApp.Background),
        ("NVIDIA Web Helper", TrackedApp.Background),
        ("nvsphelper64", TrackedApp.Background),
        ("RadeonSoftware", TrackedApp.Background),
        ("AMDRSServ", TrackedApp.Background),
        ("AMDRSSrcExt", TrackedApp.Background),
        ("amdow", TrackedApp.Background),
        ("cncmd", TrackedApp.Background),
        ("GCC", TrackedApp.Background),
        ("LEDKeeper2", TrackedApp.Background),
        ("PhoneExperienceHost", TrackedApp.Background)
    ];

    private static readonly string[] ProtectedRazerProcessNameFragments =
    [
        "service",
        "driver",
        "sdk",
        "install",
        "update",
        "elevat",
        "crash"
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

        _initialWorkingSetBytes ??= GetTotalTrackedWorkingSetBytes(out _, out _, out _, out _, out _);
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
                out var browserProcessCount,
                out var backgroundProcessCount);

            if (IsLimiterActive && processCount > 0)
            {
                LimitTrackedMemory();

                await Task.Delay(PostTrimRefreshDelay, cancellationToken);
                beforeBytes = GetTotalTrackedWorkingSetBytes(
                    out processCount,
                    out discordProcessCount,
                    out spotifyProcessCount,
                    out browserProcessCount,
                    out backgroundProcessCount);
            }

            SnapshotUpdated?.Invoke(
                this,
                new DiscordMemorySnapshot(
                    beforeBytes,
                    _initialWorkingSetBytes ?? 0,
                    processCount,
                    discordProcessCount,
                    spotifyProcessCount,
                    browserProcessCount,
                    backgroundProcessCount,
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
        out int browserProcessCount,
        out int backgroundProcessCount)
    {
        long totalBytes = 0;
        processCount = 0;
        discordProcessCount = 0;
        spotifyProcessCount = 0;
        browserProcessCount = 0;
        backgroundProcessCount = 0;

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
                else if (trackedProcess.App == TrackedApp.Browser)
                {
                    browserProcessCount++;
                }
                else
                {
                    backgroundProcessCount++;
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
        var yieldedProcessIds = new HashSet<int>();

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
                if (yieldedProcessIds.Add(process.Id))
                {
                    yield return new TrackedProcess(process, app);
                }
                else
                {
                    process.Dispose();
                }
            }
        }

        // Include current-user Razer application front ends, including future variants not yet
        // known by name. Services, drivers, SDK hosts, installers, and updaters stay protected.
        int currentSessionId;
        using (var currentProcess = Process.GetCurrentProcess())
        {
            currentSessionId = currentProcess.SessionId;
        }

        Process[] allProcesses;
        try
        {
            allProcesses = Process.GetProcesses();
        }
        catch (InvalidOperationException)
        {
            yield break;
        }

        foreach (var process in allProcesses)
        {
            var shouldYield = false;

            try
            {
                shouldYield = process.SessionId == currentSessionId &&
                              IsRazerUserApplication(process.ProcessName) &&
                              yieldedProcessIds.Add(process.Id);
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }

            if (shouldYield)
            {
                yield return new TrackedProcess(process, TrackedApp.Background);
            }
            else
            {
                process.Dispose();
            }
        }
    }

    private static bool IsRazerUserApplication(string processName)
    {
        var isRazerApplication = processName.StartsWith("Razer", StringComparison.OrdinalIgnoreCase) ||
                                 processName.Equals("CortexLauncher", StringComparison.OrdinalIgnoreCase);

        return isRazerApplication &&
               !ProtectedRazerProcessNameFragments.Any(
                   fragment => processName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
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
        Browser,
        Background
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
