using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace DiscordRamLimiter.Services;

public static class SystemShutdownService
{
    public const int CountdownSeconds = 60;

    public static Task<bool> ScheduleEmergencyShutdownAsync()
    {
        return RunShutdownCommandAsync(
            "/s",
            "/t",
            CountdownSeconds.ToString(),
            "/c",
            "Keep It Digital RAM Limiter detected sustained 99% system memory usage. Save your work or cancel the shutdown from the app.");
    }

    public static Task<bool> AbortShutdownAsync()
    {
        return RunShutdownCommandAsync("/a");
    }

    private static async Task<bool> RunShutdownCommandAsync(params string[] arguments)
    {
        try
        {
            var shutdownPath = Path.Combine(Environment.SystemDirectory, "shutdown.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = shutdownPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            return false;
        }
    }
}
