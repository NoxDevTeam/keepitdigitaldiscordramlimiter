using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DiscordRamLimiter.Services;

[SupportedOSPlatform("windows")]
public static class StartupService
{
    public const string MinimizedArgument = "--minimized";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "KeepItDigitalRamLimiter";
    private static readonly string[] LegacyRunValueNames =
    [
        "DiscordRamLimiter",
        "KeepItDigitalDiscordRamLimiter"
    ];

    public static bool IsLaunchAtStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return string.Equals(key?.GetValue(RunValueName) as string, GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetLaunchAtStartup(bool isEnabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (isEnabled)
        {
            DeleteLegacyValues(key);
            key.SetValue(RunValueName, GetStartupCommand(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(RunValueName, throwOnMissingValue: false);
        DeleteLegacyValues(key);
    }

    private static void DeleteLegacyValues(RegistryKey key)
    {
        foreach (var legacyValueName in LegacyRunValueNames)
        {
            key.DeleteValue(legacyValueName, throwOnMissingValue: false);
        }
    }

    private static string GetStartupCommand()
    {
        var processPath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
        {
            processPath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to resolve the application path.");
        }

        return $"\"{processPath}\" {MinimizedArgument}";
    }
}
