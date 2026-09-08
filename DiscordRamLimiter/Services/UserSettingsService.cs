using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DiscordRamLimiter.Services;

[SupportedOSPlatform("windows")]
public static class UserSettingsService
{
    private const string SettingsKeyPath = @"Software\Keep It Digital\RAM Limiter";
    private const string EmergencyShutdownValueName = "EmergencyShutdownEnabled";

    public static bool IsEmergencyShutdownEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath, writable: false);
        return key?.GetValue(EmergencyShutdownValueName) is int value && value == 1;
    }

    public static void SetEmergencyShutdownEnabled(bool isEnabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath, writable: true);
        key.SetValue(
            EmergencyShutdownValueName,
            isEnabled ? 1 : 0,
            RegistryValueKind.DWord);
    }
}
